using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Domain.UnitTests.Operations;

public sealed class ContainerMovementTests
{
    [Fact]
    public void GateInMovementRequiresDepotAndDestination()
    {
        var invalid = ContainerMovement.Create(Guid.NewGuid(), Guid.NewGuid(), ContainerMovementType.GateIn,
            DateTime.UtcNow, null, null, new(Guid.NewGuid(), "A", 1, 1, 1), "", "TRUCK", VehicleType.Truck);
        Assert.True(invalid.IsFailure);
        var missingPosition = ContainerMovement.Create(Guid.NewGuid(), Guid.NewGuid(), ContainerMovementType.GateIn,
            DateTime.UtcNow, null, null, null, "DEPOT", "TRUCK", VehicleType.Truck);
        Assert.True(missingPosition.IsFailure);
    }

    [Fact]
    public void RelocationStoresBothSnapshotsAndTheActualActor()
    {
        var actor = Guid.NewGuid();
        var source = new MovementPosition(Guid.NewGuid(), "A", 2, 1, 1);
        var destination = source with { Bay = 4 };
        var occurredAt = new DateTime(2026, 10, 16, 1, 0, 0, DateTimeKind.Utc);
        var result = ContainerMovement.Create(Guid.NewGuid(), Guid.NewGuid(), ContainerMovementType.Relocate,
            occurredAt, actor, source, destination, "DEPOT");
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.FromPosition!.Bay);
        Assert.Equal(4, result.Value.ToPosition!.Bay);
        Assert.Equal(actor, result.Value.ActorUserId);
        Assert.Equal(occurredAt, result.Value.OccurredAtUtc);
    }

}
