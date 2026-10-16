using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles;

internal static class MovementHistory
{
    internal static MovementPosition Position(YardBlock block, int bay, int row, int tier) => new(block.Id, block.Code, bay, row, tier);

    internal static void Append(IAppDbContext context, ContainerLifecycle lifecycle, ContainerMovementType type,
        DateTime utcNow, Guid? actor, MovementPosition? source, MovementPosition? destination,
        string? vehicle = null, VehicleType? vehicleType = null, Guid? orderId = null)
    {
        var movement = ContainerMovement.Create(lifecycle.ContainerId, lifecycle.Id, type, utcNow, actor, source, destination,
            lifecycle.TenantId, vehicle, vehicleType, orderId);
        if (movement.IsFailure) throw new InvalidOperationException("Cannot persist invalid movement history.");
        context.ContainerMovements.Add(movement.Value!);
    }
}
