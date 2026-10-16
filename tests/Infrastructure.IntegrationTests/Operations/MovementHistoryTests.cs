using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Common.Rules;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;
using SnpDepotManagement.Application.Features.Yard.Commands.PlaceContainer;
using SnpDepotManagement.Application.Features.Yard.Commands.RelocateContainer;
using SnpDepotManagement.Application.Features.Yard.Commands.RemoveContainer;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Infrastructure.IntegrationTests.Fixtures;
using SnpDepotManagement.Infrastructure.Persistence;
using Xunit;

namespace SnpDepotManagement.Infrastructure.IntegrationTests.Operations;

[Collection("PostgreSqlCollection")]
public sealed class MovementHistoryTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task PlaceRequiresAnActiveLifecycleAndDoesNotOccupySlotsWithoutOne()
    {
        var tenant = "MV-" + Guid.NewGuid().ToString("N");
        await using var db = fixture.CreateContext(tenant);
        var (block, container) = await Seed(db, tenant);
        var result = await new PlaceContainerCommandHandler(db, new RuleEngine())
            .Handle(new(block.Id, 1, 1, 1, container.Id), TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.False(await db.YardSlots.AnyAsync(s => s.ContainerId == container.Id, TestContext.Current.CancellationToken));
        Assert.False(await db.ContainerMovements.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoveClearsLifecyclePositionWhileContainerRemainsInDepot()
    {
        var tenant = "MV-" + Guid.NewGuid().ToString("N");
        await using var db = fixture.CreateContext(tenant);
        var (block, container) = await Seed(db, tenant);
        var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
        slot.Occupy(container.Id);
        var lifecycle = ContainerLifecycle.GateIn(container.Id, container.OwnerCode, Classification.A, ContainerCondition.Normal,
            "TRUCK", slot.Id, DateTime.UtcNow, tenant);
        db.ContainerLifecycles.Add(lifecycle);
        db.Entry(lifecycle).Property(l => l.MovementHistoryVersion).CurrentValue = 0;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var result = await new RemoveContainerCommandHandler(db).Handle(new(block.Id, 1, 1, 1), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        await db.Entry(lifecycle).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.Null(lifecycle.CurrentSlotId);
        Assert.True(lifecycle.IsInDepot);
        Assert.Equal(0, lifecycle.MovementHistoryVersion);
        var movement = await db.ContainerMovements.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ContainerMovementType.Remove, movement.EventType);
        Assert.Equal(1, movement.FromPosition!.Bay);
        Assert.Null(movement.ToPosition);
    }

    [Fact]
    public async Task RelocationUpdatesLifecyclePositionToActualDestination()
    {
        var tenant = "MV-" + Guid.NewGuid().ToString("N");
        await using var db = fixture.CreateContext(tenant);
        var (block, container) = await Seed(db, tenant);
        var source = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
        var target = block.Slots.First(s => s.Bay == 3 && s.Tier == 1);
        source.Occupy(container.Id);
        var lifecycle = ContainerLifecycle.GateIn(container.Id, container.OwnerCode, Classification.A, ContainerCondition.Normal,
            "TRUCK", source.Id, DateTime.UtcNow, tenant);
        db.ContainerLifecycles.Add(lifecycle);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var result = await new RelocateContainerCommandHandler(db, new RuleEngine())
            .Handle(new(block.Id, 1, 1, 1, 3, 1, 1), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        await db.Entry(lifecycle).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(target.Id, lifecycle.CurrentSlotId);
        var movement = await db.ContainerMovements.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, movement.FromPosition!.Bay);
        Assert.Equal(3, movement.ToPosition!.Bay);
    }

    [Fact]
    public async Task GateInFortyFeetOccupiesBothSupportingSlots()
    {
        var tenant = "MV-" + Guid.NewGuid().ToString("N");
        await using var db = fixture.CreateContext(tenant);
        var (block, container) = await Seed(db, tenant, ContainerSize.Ft40);
        var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
        var result = await new GateInCommandHandler(db, TimeProvider.System)
            .Handle(new(container.Id, "TRUCK", slot.Id), TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, await db.YardSlots.CountAsync(s => s.ContainerId == container.Id, TestContext.Current.CancellationToken));
        var movement = await db.ContainerMovements.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ContainerMovementType.GateIn, movement.EventType);
        Assert.Equal(2, movement.ToPosition!.Bay);
        Assert.Equal(1, (await db.ContainerLifecycles.SingleAsync(TestContext.Current.CancellationToken)).MovementHistoryVersion);
    }

    [Fact]
    public async Task PersistedMovementAndItsSnapshotCannotBeEditedOrDeleted()
    {
        var tenant = "MV-" + Guid.NewGuid().ToString("N");
        await using var db = fixture.CreateContext(tenant);
        var (block, container) = await Seed(db, tenant);
        var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
        await new GateInCommandHandler(db, TimeProvider.System).Handle(new(container.Id, "TRUCK", slot.Id), TestContext.Current.CancellationToken);
        var movement = await db.ContainerMovements.SingleAsync(TestContext.Current.CancellationToken);
        db.Entry(movement).Reference(m => m.ToPosition).TargetEntry!.Property("Bay").CurrentValue = 9;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
        db.ChangeTracker.Clear();
        movement = await db.ContainerMovements.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, movement.ToPosition!.Bay);
        db.ContainerMovements.Remove(movement);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FailedRelocationPreservesPositionAndDoesNotCreateAnEvent()
    {
        var tenant = "MV-" + Guid.NewGuid().ToString("N");
        await using var db = fixture.CreateContext(tenant);
        var (block, container) = await Seed(db, tenant);
        var slot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
        await new GateInCommandHandler(db, TimeProvider.System).Handle(new(container.Id, "TRUCK", slot.Id), TestContext.Current.CancellationToken);
        var result = await new RelocateContainerCommandHandler(db, new RuleEngine())
            .Handle(new(block.Id, 1, 1, 1, 5, 1, 2), TestContext.Current.CancellationToken);
        Assert.True(result.IsFailure);
        Assert.Equal(1, await db.ContainerMovements.CountAsync(TestContext.Current.CancellationToken));
        await db.Entry(slot).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(container.Id, slot.ContainerId);
        Assert.Equal(slot.Id, (await db.ContainerLifecycles.SingleAsync(TestContext.Current.CancellationToken)).CurrentSlotId);
    }

    [Fact]
    public async Task ReentryRecordsANewLifecycleWithoutChangingThePreviousHistory()
    {
        var tenant = "MV-" + Guid.NewGuid().ToString("N");
        await using var db = fixture.CreateContext(tenant);
        var (block, container) = await Seed(db, tenant);
        var firstSlot = block.Slots.First(s => s.Bay == 1 && s.Tier == 1);
        var secondSlot = block.Slots.First(s => s.Bay == 3 && s.Tier == 1);
        var order = DeliveryOrder.Create("DO" + Guid.NewGuid().ToString("N")[..6], container.OwnerCode,
            "0101234567", "Test customer", DateTime.UtcNow.AddDays(2), "Test vessel").Value!;
        order.AddItem(container.Size, container.Type, 2);
        db.DeliveryOrders.Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var gateIn = new GateInCommandHandler(db, TimeProvider.System);
        var first = await gateIn.Handle(new(container.Id, "TRUCK1", firstSlot.Id), TestContext.Current.CancellationToken);
        var gateOut = await new GateOutCommandHandler(db, new RuleEngine(), TimeProvider.System)
            .Handle(new(container.Id, order.Id, "TRUCK2"), TestContext.Current.CancellationToken);
        Assert.True(gateOut.IsSuccess);
        var second = await gateIn.Handle(new(container.Id, "TRUCK3", secondSlot.Id), TestContext.Current.CancellationToken);
        Assert.NotEqual(first.Value, second.Value);
        var events = await db.ContainerMovements.OrderBy(m => m.OccurredAtUtc).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(new[] { ContainerMovementType.GateIn, ContainerMovementType.GateOut, ContainerMovementType.GateIn }, events.Select(m => m.EventType));
        Assert.Equal(first.Value, events[0].ContainerLifecycleId);
        Assert.Equal(first.Value, events[1].ContainerLifecycleId);
        Assert.Equal(second.Value, events[2].ContainerLifecycleId);
    }

    private static async Task<(YardBlock Block, Container Container)> Seed(AppDbContext db, string tenant, ContainerSize size = ContainerSize.Ft20)
    {
        var block = YardBlock.CreateStandard("MV" + Guid.NewGuid().ToString("N")[..6], "Movement test", 5, 1, 2, tenant);
        var container = Container.Create(ContainerTestHelper.CreateValidContainerNumber(serial: Random.Shared.Next(100000, 999999)),
            "22G1", size, ContainerType.Dry, 2000, 30000, null, "MSK", tenant).Value!;
        db.YardBlocks.Add(block);
        db.Containers.Add(container);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (block, container);
    }
}
