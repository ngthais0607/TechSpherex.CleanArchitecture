using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Common.Rules;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;
using SnpDepotManagement.Application.Features.Yard.Commands.RemoveContainer;
using SnpDepotManagement.Application.Features.Yard.Commands.RelocateContainer;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Infrastructure.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Infrastructure.IntegrationTests.Operations;

[Collection("PostgreSqlCollection")]
public sealed class YardSafetyRegressionTests(PostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EvenBayOperationCannotCombineTwoDifferentTwentyFootContainers(bool relocate)
    {
        var ct = TestContext.Current.CancellationToken;
        var tenant = "SAFE-" + Guid.NewGuid().ToString("N");
        await using var db = fixture.CreateContext(tenant);
        var block = YardBlock.CreateStandard("SAFETY", "Safety test", 5, 1, 2, tenant);
        var first = Container.Create(ContainerTestHelper.CreateValidContainerNumber(serial: 300001), "22G1", ContainerSize.Ft20, ContainerType.Dry, 2000, 30000, null, "MSK", tenant).Value!;
        var second = Container.Create(ContainerTestHelper.CreateValidContainerNumber(serial: 300002), "22G1", ContainerSize.Ft20, ContainerType.Dry, 2000, 30000, null, "MSK", tenant).Value!;
        db.YardBlocks.Add(block);
        db.Containers.AddRange(first, second);
        await db.SaveChangesAsync(ct);
        var gateIn = new GateInCommandHandler(db, TimeProvider.System);
        await gateIn.Handle(new(first.Id, "TRUCK", block.Slots.First(s => s.Bay == 1 && s.Tier == 1).Id), ct);
        await gateIn.Handle(new(second.Id, "TRUCK", block.Slots.First(s => s.Bay == 3 && s.Tier == 1).Id), ct);
        var result = relocate
            ? await new RelocateContainerCommandHandler(db, new RuleEngine()).Handle(new(block.Id, 2, 1, 1, 5, 1, 1), ct)
            : await new RemoveContainerCommandHandler(db).Handle(new(block.Id, 2, 1, 1), ct);
        Assert.True(result.IsFailure);
        Assert.Equal(2, await db.YardSlots.CountAsync(s => s.IsOccupied, ct));
        Assert.Equal(2, await db.ContainerMovements.CountAsync(ct));
    }

    [Fact]
    public async Task GateOutCannotLeaveAnOccupiedTierWithoutItsSupportingContainer()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenant = "SAFE-" + Guid.NewGuid().ToString("N");
        await using var db = fixture.CreateContext(tenant);
        var block = YardBlock.CreateStandard("STACK", "Stack test", 3, 1, 2, tenant);
        var bottom = Container.Create(ContainerTestHelper.CreateValidContainerNumber(serial: 300003), "22G1", ContainerSize.Ft20, ContainerType.Dry, 2000, 30000, null, "MSK", tenant).Value!;
        var top = Container.Create(ContainerTestHelper.CreateValidContainerNumber(serial: 300004), "22G1", ContainerSize.Ft20, ContainerType.Dry, 2000, 30000, null, "MSK", tenant).Value!;
        var order = DeliveryOrder.Create("STACK-DO", "MSK", "0101234567", "Test customer", DateTime.UtcNow.AddDays(2), "Test vessel", tenant).Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 3);
        db.YardBlocks.Add(block);
        db.Containers.AddRange(bottom, top);
        db.DeliveryOrders.Add(order);
        await db.SaveChangesAsync(ct);
        var gateIn = new GateInCommandHandler(db, TimeProvider.System);
        await gateIn.Handle(new(bottom.Id, "TRUCK", block.Slots.First(s => s.Bay == 1 && s.Tier == 1).Id), ct);
        await gateIn.Handle(new(top.Id, "TRUCK", block.Slots.First(s => s.Bay == 1 && s.Tier == 2).Id), ct);
        var result = await new GateOutCommandHandler(db, new RuleEngine(), TimeProvider.System).Handle(new(bottom.Id, order.Id, "TRUCK"), ct);
        Assert.True(result.IsFailure);
        Assert.Equal(3, order.Items.Single().RemainingQuantity);
        Assert.Equal(2, await db.ContainerMovements.CountAsync(ct));
    }
}
