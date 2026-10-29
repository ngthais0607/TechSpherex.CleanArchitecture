using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Application.Abstractions.Caching;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Application.Features.MasterData.Customers.Queries.GetCustomers;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Queries.GetLineOperators;
using SnpDepotManagement.Application.Features.Reports.Dtos;
using SnpDepotManagement.Application.Features.Reports.Queries.GetBlockCapacity;
using SnpDepotManagement.Application.Features.Reports.Queries.GetGateTraffic;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Infrastructure.Persistence;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Reports;

[Collection("Api")]
public sealed class AggregateQueryPerformanceTests(ApiFactory factory)
{
    [Fact]
    public async Task CapacityAggregatesInSqlAndCountsSharedContainerOnce()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid().ToString("N");
        var block = YardBlock.CreateStandard("FULL", "Full", 3, 1, 1, tenantId);
        var empty = YardBlock.CreateStandard("EMPTY", "Empty", 3, 1, 1, tenantId);
        var container = OperationScenario.NewContainer(tenant: tenantId, size: ContainerSize.Ft40);
        foreach (var slot in block.Slots) slot.Occupy(container.Id);
        await using (var seed = factory.Context(tenantId))
        {
            seed.Containers.Add(container);
            seed.YardBlocks.AddRange(block, empty, YardBlock.CreateVirtual("VIRTUAL", "Virtual", tenantId),
                YardBlock.CreateStandard("OTHER", "Other tenant", 3, 1, 1, tenantId + "-OTHER"));
            await seed.SaveChangesAsync(ct);
        }
        var capture = new SelectQueryCapture();
        await using var db = CapturedContext(tenantId, capture);
        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<IReadOnlyList<BlockCapacityDto>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BlockCapacityDto>?>(null));
        var result = await new GetBlockCapacityQueryHandler(db, cache)
            .Handle(new GetBlockCapacityQuery(), ct);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        var full = Assert.Single(result.Value, r => r.BlockId == block.Id);
        Assert.Equal(2, full.TotalCapacityTeu);
        Assert.Equal(2, full.OccupiedTeu);
        Assert.Equal(1, full.ContainersCount);
        Assert.Equal(100d, full.OccupancyRate);
        Assert.Equal(0, Assert.Single(result.Value, r => r.BlockId == empty.Id).ContainersCount);
        Assert.Equal(2, capture.Commands.Count);
        Assert.Contains(capture.Commands, sql => sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TrafficAggregatesInSqlUsingVietnamMidnightAndFillsEmptyDays()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid().ToString("N");
        var now = new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
        var midnightUtc = new DateTime(2026, 10, 28, 17, 0, 0, DateTimeKind.Utc);
        var container = OperationScenario.NewContainer(tenant: tenantId);
        var before = GateTransaction.CreateGateIn(container.Id, "T1", VehicleType.Truck, null, null, null, tenantId);
        var after = GateTransaction.CreateGateOut(container.Id, Guid.NewGuid(), "T2", VehicleType.Truck, null, null, tenantId);
        var cancelled = GateTransaction.CreateGateIn(container.Id, "T3", VehicleType.Truck, null, null, null, tenantId);
        cancelled.Cancel(now.UtcDateTime);
        var other = GateTransaction.CreateGateIn(container.Id, "T4", VehicleType.Truck, null, null, null, tenantId + "-OTHER");
        await using (var seed = factory.Context(tenantId))
        {
            seed.Containers.Add(container);
            seed.GateTransactions.AddRange(before, after, cancelled, other);
            await seed.SaveChangesAsync(ct);
            seed.Entry(before).Property(t => t.CreatedAt).CurrentValue = midnightUtc.AddSeconds(-1);
            foreach (var transaction in new[] { after, cancelled, other })
                seed.Entry(transaction).Property(t => t.CreatedAt).CurrentValue = midnightUtc;
            await seed.SaveChangesAsync(ct);
        }
        var capture = new SelectQueryCapture();
        await using var db = CapturedContext(tenantId, capture);
        var cache = Substitute.For<ICacheService>();
        cache.GetAsync<IReadOnlyList<DailyGateTrafficDto>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<DailyGateTrafficDto>?>(null));
        var result = await new GetGateTrafficQueryHandler(db, cache, new FixedTime(now))
            .Handle(new GetGateTrafficQuery(3), ct);
        Assert.True(result.IsSuccess);
        Assert.Collection(result.Value!,
            day => { Assert.Equal("2026-10-27", day.Date); Assert.Equal(0, day.TotalCount); },
            day => { Assert.Equal("2026-10-28", day.Date); Assert.Equal(1, day.GateInCount); Assert.Equal(0, day.GateOutCount); },
            day => { Assert.Equal("2026-10-29", day.Date); Assert.Equal(0, day.GateInCount); Assert.Equal(1, day.GateOutCount); });
        Assert.Contains("GROUP BY", Assert.Single(capture.Commands), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MasterDataAggregatesOrdersInSqlAndPreservesZeroCountsAndTenantIsolation()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid().ToString("N");
        var prefix = "Q" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var op = LineOperator.Create(prefix + "A", "With orders").Value!;
        var emptyOp = LineOperator.Create(prefix + "B", "No orders").Value!;
        var customer = Customer.Create("1234567890", prefix + " customer", tenantId).Value!;
        var emptyCustomer = Customer.Create("1234567891", prefix + " empty", tenantId).Value!;
        var now = DateTime.UtcNow;
        var active = NewOrder(now.AddDays(7), tenantId);
        var expired = NewOrder(now.AddDays(-7), tenantId);
        var completed = NewOrder(now.AddDays(7), tenantId);
        completed.MarkCompleted();
        var other = NewOrder(now.AddDays(7), tenantId + "-OTHER");
        var container = OperationScenario.NewContainer(tenant: tenantId);
        await using (var seed = factory.Context(tenantId))
        {
            seed.LineOperators.AddRange(op, emptyOp);
            seed.Customers.AddRange(customer, emptyCustomer);
            seed.DeliveryOrders.AddRange(active, expired, completed, other);
            seed.Containers.Add(container);
            seed.ContainerLifecycles.Add(ContainerLifecycle.GateIn(container.Id, op.Code,
                Classification.A, ContainerCondition.Normal, "TRUCK", null, now, tenantId));
            await seed.SaveChangesAsync(ct);
        }
        var capture = new SelectQueryCapture();
        await using var db = CapturedContext(tenantId, capture);
        var customers = await new GetCustomersQueryHandler(db).Handle(new GetCustomersQuery(Search: prefix), ct);
        Assert.True(customers.IsSuccess);
        Assert.Equal(2, customers.Value!.TotalCount);
        var populated = Assert.Single(customers.Value.Items, c => c.Id == customer.Id);
        Assert.Equal(3, populated.OrderCount);
        Assert.Equal(1, populated.ActiveOrderCount);
        Assert.Equal(0, Assert.Single(customers.Value.Items, c => c.Id == emptyCustomer.Id).OrderCount);
        Assert.Equal(3, capture.Commands.Count);
        Assert.Contains(capture.Commands, sql => sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase));

        capture.Commands.Clear();
        var operators = await new GetLineOperatorsQueryHandler(db).Handle(new GetLineOperatorsQuery(Search: prefix), ct);
        Assert.True(operators.IsSuccess);
        var populatedOp = Assert.Single(operators.Value!.Items, l => l.Id == op.Id);
        Assert.Equal(3, populatedOp.OrderCount);
        Assert.Equal(1, populatedOp.ActiveOrderCount);
        Assert.Equal(1, populatedOp.ContainersInYard);
        Assert.Equal(0, Assert.Single(operators.Value.Items, l => l.Id == emptyOp.Id).OrderCount);
        Assert.Equal(4, capture.Commands.Count);
        Assert.Equal(2, capture.Commands.Count(sql => sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase)));

        DeliveryOrder NewOrder(DateTime expiry, string tenant)
        {
            var order = DeliveryOrder.Create("Q-" + Guid.NewGuid().ToString("N"), op.Code,
                customer.TaxCode, customer.Name, expiry, "VESSEL", tenant).Value!;
            order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);
            return order;
        }
    }

    private AppDbContext CapturedContext(string tenantId, SelectQueryCapture capture)
    {
        using var source = factory.Context(tenantId);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(source.Database.GetConnectionString()).UseSnakeCaseNamingConvention()
            .AddInterceptors(capture).Options;
        var tenant = Substitute.For<ITenantProvider>();
        tenant.GetCurrentTenantId().Returns(tenantId);
        return new AppDbContext(options, tenant, TimeProvider.System, Substitute.For<ICurrentUser>());
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
