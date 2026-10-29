using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SnpDepotManagement.Api.IntegrationTests.Fixtures;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Application.Features.Operations.Bookings.Commands.AssignContainers;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Infrastructure.Persistence;
using Xunit;

namespace SnpDepotManagement.Api.IntegrationTests.Operations;

[Collection("Api")]
public sealed class AssignmentQueryPerformanceTests(ApiFactory factory)
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(10, false)]
    [InlineData(200, false)]
    [InlineData(10, true)]
    public async Task AssignmentReadsAreBoundedRegardlessOfBatchSize(int count, bool expiredBookings)
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = "QUERY-" + Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var op = LineOperator.Create("Q" + Guid.NewGuid().ToString("N")[..6], "Query test").Value!;
        var order = DeliveryOrder.Create("NEW-" + Guid.NewGuid().ToString("N"), op.Code,
            "1234567890", "Query customer", now.AddDays(7), "VESSEL", tenantId).Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, count);
        var oldOrder = DeliveryOrder.Create("OLD-" + Guid.NewGuid().ToString("N"), op.Code,
            "1234567890", "Query customer", now.AddDays(-7), "VESSEL", tenantId).Value!;
        oldOrder.AddItem(ContainerSize.Ft20, ContainerType.Dry, count);
        // Alternate mapped owner and direct operator lookup within the same request.
        var containers = Enumerable.Range(0, count)
            .Select(i => OperationScenario.NewContainer(i % 2 == 0 ? "QMP" : op.Code, tenantId)).ToArray();
        await using (var seed = factory.Context(tenantId))
        {
            seed.LineOperators.Add(op);
            seed.LineOperatorOwnerCodes.Add(LineOperatorOwnerCode.Create(op.Id, "QMP", tenantId));
            seed.Containers.AddRange(containers);
            seed.DeliveryOrders.AddRange(order, oldOrder);
            seed.ContainerLifecycles.AddRange(containers.Select(c => ContainerLifecycle.GateIn(
                c.Id, op.Code, Classification.A, ContainerCondition.Normal, "TRUCK", null, now, tenantId)));
            if (expiredBookings)
                seed.Bookings.AddRange(containers.Select(c => DeliveryOrderBooking.Reserve(
                    oldOrder.Id, oldOrder.Items.Single().Id, c.Id, tenantId, now.AddDays(-10))));
            // A conflicting owner mapping in another depot must not affect this request.
            seed.LineOperatorOwnerCodes.Add(LineOperatorOwnerCode.Create(Guid.NewGuid(), "QMP", tenantId + "-OTHER"));
            await seed.SaveChangesAsync(ct);
        }

        var capture = new SelectQueryCapture();
        await using var db = CapturedContext(tenantId, capture);
        var handler = new AssignContainersToOrderCommandHandler(db);
        var command = new AssignContainersToOrderCommand(order.OrderNumber,
            containers.Select(c => c.ContainerNumber.Value).ToArray());
        var result = await handler.Handle(command, ct);
        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(count, result.Value!.AssignedCount);
        TestContext.Current.TestOutputHelper!.WriteLine(
            $"Assignment: containers={count}, expiredBookings={expiredBookings}, SELECTs={capture.Commands.Count}");
        Assert.InRange(capture.Commands.Count, 1, 8);

        await using var verify = factory.Context(tenantId);
        Assert.Equal(count, await verify.Bookings.CountAsync(b => b.OrderId == order.Id && b.Status == BookingStatus.Reserved, ct));
        if (expiredBookings)
            Assert.Equal(count, await verify.Bookings.CountAsync(b => b.OrderId == oldOrder.Id && b.Status == BookingStatus.Expired, ct));

        // Repeating an assignment must neither consume quota again nor create duplicates.
        capture.Commands.Clear();
        var repeated = await handler.Handle(command, ct);
        Assert.True(repeated.IsSuccess, repeated.Error.ToString());
        Assert.Equal(0, repeated.Value!.AssignedCount);
        Assert.InRange(capture.Commands.Count, 1, 8);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task OwnerMappingTakesPrecedenceOverDirectOperatorAndFallback(bool danglingMapping, bool mismatch)
    {
        var ct = TestContext.Current.CancellationToken;
        var tenantId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        await using var seed = factory.Context(tenantId);
        var direct = await seed.LineOperators.FirstOrDefaultAsync(l => l.Code == "QMP", ct);
        if (direct is null)
        {
            direct = LineOperator.Create("QMP", "Direct operator").Value!;
            seed.LineOperators.Add(direct);
        }
        var mapped = LineOperator.Create("Q" + Guid.NewGuid().ToString("N")[..6], "Mapped operator").Value!;
        seed.LineOperators.Add(mapped);
        seed.LineOperatorOwnerCodes.Add(LineOperatorOwnerCode.Create(
            danglingMapping ? Guid.NewGuid() : mapped.Id, "QMP", tenantId));
        var order = DeliveryOrder.Create("Q-" + Guid.NewGuid().ToString("N"),
            danglingMapping || mismatch ? direct.Code : mapped.Code,
            "1234567890", "Customer", now.AddDays(7), "VESSEL", tenantId).Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);
        var container = OperationScenario.NewContainer("QMP", tenantId);
        seed.Containers.Add(container);
        seed.DeliveryOrders.Add(order);
        seed.ContainerLifecycles.Add(ContainerLifecycle.GateIn(container.Id, mapped.Code,
            Classification.A, ContainerCondition.Normal, "TRUCK", null, now, tenantId));
        await seed.SaveChangesAsync(ct);

        await using var db = factory.Context(tenantId);
        var result = await new AssignContainersToOrderCommandHandler(db).Handle(
            new AssignContainersToOrderCommand(order.OrderNumber, [container.ContainerNumber.Value]), ct);
        if (danglingMapping || mismatch)
        {
            Assert.True(result.IsFailure);
            Assert.Equal("OrderRule.LineOperatorMismatch", result.Error.Code);
            Assert.False(await db.Bookings.AnyAsync(ct));
        }
        else
        {
            Assert.True(result.IsSuccess, result.Error.ToString());
            Assert.Equal(1, result.Value!.AssignedCount);
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
}
