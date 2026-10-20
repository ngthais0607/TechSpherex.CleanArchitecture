using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Infrastructure.Persistence;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Operations;

[Collection("Api")]
public sealed class ConcurrencyTests(ApiFactory factory)
{
    [Fact]
    public async Task ConcurrentDisjointExpansionsKeepDimensionsAndSlotsConsistent()
    {
        var ct = TestContext.Current.CancellationToken;
        var s = await OperationScenario.CreateAsync(factory);
        using var authenticated = await ApiTestClient.LoginAsync(factory);
        var barrier = new ExpansionBarrier(s.Block.Id);
        await using var host = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddDbContext<AppDbContext>(options => options.AddInterceptors(barrier))));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = authenticated.DefaultRequestHeaders.Authorization;
        var route = $"/api/yard/blocks/{s.Block.Id}/expand";
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync(route, new { NewBays = 7, NewRows = 1, NewTiers = 2 }, ct),
            client.PostAsJsonAsync(route, new { NewBays = 5, NewRows = 2, NewTiers = 2 }, ct));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        await using var db = factory.Context("DEPOT-A");
        var block = await db.YardBlocks.Include(b => b.Slots).SingleAsync(b => b.Id == s.Block.Id, ct);
        Assert.All(block.Slots, slot => { Assert.InRange(slot.Bay, 1, block.MaxBays); Assert.InRange(slot.Row, 1, block.MaxRows); });
        Assert.Equal(((block.MaxBays + 1) / 2) * block.MaxRows * block.MaxTiers, block.Slots.Count);
        foreach (var response in responses) response.Dispose();
    }

    [Fact]
    public async Task ConcurrentStackPlacementAndSupportGateOutCannotBothCommit()
    {
        var ct = TestContext.Current.CancellationToken;
        var s = await OperationScenario.CreateAsync(factory);
        using var authenticated = await ApiTestClient.LoginAsync(factory);
        var barrier = new TwoSaveBarrier(s.First.Id, s.Second.Id, null);
        await using var host = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddDbContext<AppDbContext>(options => options.AddInterceptors(barrier))));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = authenticated.DefaultRequestHeaders.Authorization;
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/gate/out", new { ContainerId = s.First.Id, DeliveryOrderId = s.Order.Id, OutVehicleNumber = "OUT" }, ct),
            client.PostAsJsonAsync("/api/yard/placements", new { ContainerId = s.Second.Id, BlockId = s.Block.Id, Bay = 1, Row = 1, Tier = 2 }, ct));
        Assert.Equal(2, barrier.Arrivals);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        await using var db = factory.Context("DEPOT-A");
        var slots = await db.YardSlots.Where(x => x.BlockId == s.Block.Id && x.Position.Bay == 1).ToListAsync(ct);
        Assert.False(slots.Single(x => x.Tier == 2).IsOccupied && !slots.Single(x => x.Tier == 1).IsOccupied);
        Assert.Single(await db.ContainerMovements.Where(m => m.ContainerId == s.First.Id || m.ContainerId == s.Second.Id).ToListAsync(ct));
        foreach (var response in responses) response.Dispose();
    }

    [Fact]
    public async Task ConcurrentDifferentOrderItemsConflictThenRetryCompletesOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var s = await OperationScenario.CreateAsync(factory);
        var forty = OperationScenario.NewContainer(size: ContainerSize.Ft40);
        await using (var db = factory.Context("DEPOT-A"))
        {
            db.Containers.Add(forty);
            var life = ContainerLifecycle.GateIn(forty.Id, "MSK", Classification.A, ContainerCondition.Normal, "IN", Guid.NewGuid(), DateTime.UtcNow, "DEPOT-A");
            life.SetCurrentSlot(null);
            db.ContainerLifecycles.Add(life);
            var order = await db.DeliveryOrders.Include(o => o.Items).SingleAsync(o => o.Id == s.Order.Id, ct);
            order.AddItem(ContainerSize.Ft40, ContainerType.Dry, 1);
            db.DeliveryOrderItems.Add(order.Items.Single(i => i.Size == ContainerSize.Ft40));
            await db.SaveChangesAsync(ct);
        }
        using var authenticated = await ApiTestClient.LoginAsync(factory);
        var barrier = new TwoSaveBarrier(s.First.Id, forty.Id, ContainerMovementType.GateOut);
        await using var host = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddDbContext<AppDbContext>(options => options.AddInterceptors(barrier))));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = authenticated.DefaultRequestHeaders.Authorization;
        object Body(Guid id) => new { ContainerId = id, DeliveryOrderId = s.Order.Id, OutVehicleNumber = "OUT" };
        var ids = new[] { s.First.Id, forty.Id };
        var responses = await Task.WhenAll(ids.Select(id => client.PostAsJsonAsync("/api/gate/out", Body(id), ct)));
        Assert.Equal(2, barrier.Arrivals);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NoContent);
        var loser = Array.FindIndex(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.InRange(loser, 0, 1);
        using var retry = await authenticated.PostAsJsonAsync("/api/gate/out", Body(ids[loser]), ct);
        Assert.Equal(HttpStatusCode.NoContent, retry.StatusCode);
        await using var verify = factory.Context("DEPOT-A");
        var completed = await verify.DeliveryOrders.Include(o => o.Items).SingleAsync(o => o.Id == s.Order.Id, ct);
        Assert.True(completed.IsCompleted);
        Assert.All(completed.Items, item => Assert.Equal(0, item.RemainingQuantity));
        Assert.Equal(2, await verify.ContainerMovements.CountAsync(m => m.DeliveryOrderId == s.Order.Id, ct));
        foreach (var response in responses) response.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentRequestsCommitOneMovementAndRollbackTheLoser(bool gateOut)
    {
        var ct = TestContext.Current.CancellationToken;
        var s = await OperationScenario.CreateAsync(factory, gateOut, gateOut);
        using var authenticated = await ApiTestClient.LoginAsync(factory);
        var barrier = new TwoSaveBarrier(s.First.Id, s.Second.Id, gateOut ? ContainerMovementType.GateOut : ContainerMovementType.Place);
        await using var host = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddDbContext<AppDbContext>(options => options.AddInterceptors(barrier))));
        using var first = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var second = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        first.DefaultRequestHeaders.Authorization = authenticated.DefaultRequestHeaders.Authorization;
        second.DefaultRequestHeaders.Authorization = authenticated.DefaultRequestHeaders.Authorization;
        var route = gateOut ? "/api/gate/out" : "/api/yard/placements";
        object Body(Guid id) => gateOut
            ? new { ContainerId = id, DeliveryOrderId = s.Order.Id, OutVehicleNumber = "TRUCK" }
            : new { ContainerId = id, BlockId = s.Block.Id, Bay = 1, Row = 1, Tier = 1 };
        var responses = await Task.WhenAll(first.PostAsJsonAsync(route, Body(s.First.Id), ct), second.PostAsJsonAsync(route, Body(s.Second.Id), ct));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NoContent);
        var conflict = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType?.MediaType);
        Assert.Equal(2, barrier.Arrivals);
        await using var db = factory.Context("DEPOT-A");
        var events = await db.ContainerMovements.Where(m => m.ContainerId == s.First.Id || m.ContainerId == s.Second.Id).ToListAsync(ct);
        var movement = Assert.Single(events);
        Assert.Equal(gateOut ? ContainerMovementType.GateOut : ContainerMovementType.Place, movement.EventType);
        var adminId = await db.Users.Where(u => u.UserName == "admin").Select(u => u.Id).SingleAsync(ct);
        Assert.Equal(adminId, movement.ActorUserId);
        Assert.Equal(1, await db.YardSlots.CountAsync(x => x.BlockId == s.Block.Id && x.IsOccupied, ct));
        var order = await db.DeliveryOrders.Include(o => o.Items).SingleAsync(o => o.Id == s.Order.Id, ct);
        Assert.Equal(gateOut ? 0 : 1, order.Items.Single().RemainingQuantity);
        Assert.Equal(gateOut ? 1 : 0, await db.ContainerLifecycles.CountAsync(l =>
            (l.ContainerId == s.First.Id || l.ContainerId == s.Second.Id) && l.Status == LifecycleStatus.GatedOut, ct));
        foreach (var response in responses) response.Dispose();
    }

    private sealed class ExpansionBarrier(Guid blockId) : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<YardBlock>().Any(e => e.Entity.Id == blockId && e.State == EntityState.Modified))
            {
                if (Interlocked.Increment(ref _arrivals) == 2) _ready.TrySetResult();
                await _ready.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return result;
        }
    }

    private sealed class TwoSaveBarrier(Guid first, Guid second, ContainerMovementType? type) : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        public int Arrivals => _arrivals;
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<ContainerMovement>().Any(e => e.State == EntityState.Added &&
                (type == null || e.Entity.EventType == type) && (e.Entity.ContainerId == first || e.Entity.ContainerId == second)))
            {
                if (Interlocked.Increment(ref _arrivals) == 2) _ready.TrySetResult();
                await _ready.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return result;
        }
    }
}
