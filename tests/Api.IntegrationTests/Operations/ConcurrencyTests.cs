using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Infrastructure.Persistence;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Operations;

[Collection("Api")]
public sealed class ConcurrencyTests(ApiFactory factory)
{
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

    private sealed class TwoSaveBarrier(Guid first, Guid second, ContainerMovementType type) : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        public int Arrivals => _arrivals;
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<ContainerMovement>().Any(e => e.State == EntityState.Added &&
                e.Entity.EventType == type && (e.Entity.ContainerId == first || e.Entity.ContainerId == second)))
            {
                if (Interlocked.Increment(ref _arrivals) == 2) _ready.TrySetResult();
                await _ready.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return result;
        }
    }
}
