using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Domain.Entities.Operations;

public sealed class ContainerMovement : BaseEntity, ITenantEntity
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid ContainerId { get; private set; }
    public Guid ContainerLifecycleId { get; private set; }
    public ContainerMovementType EventType { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public MovementPosition? FromPosition { get; private set; }
    public MovementPosition? ToPosition { get; private set; }
    public string? VehicleNumber { get; private set; }
    public VehicleType? VehicleType { get; private set; }
    public Guid? DeliveryOrderId { get; private set; }

    string ITenantEntity.TenantId
    {
        get => TenantId;
        set
        {
            if (!string.IsNullOrEmpty(TenantId) && TenantId != value)
                throw new InvalidOperationException("Movement depot assignment is immutable.");
            TenantId = value;
        }
    }

    private ContainerMovement() { }

    public static Result<ContainerMovement> Create(Guid containerId, Guid lifecycleId, ContainerMovementType eventType,
        DateTime occurredAtUtc, Guid? actorUserId, MovementPosition? source, MovementPosition? destination, string tenantId,
        string? vehicleNumber = null, VehicleType? vehicleType = null, Guid? deliveryOrderId = null)
    {
        static bool ValidPosition(MovementPosition? position) => position is null ||
            (position.BlockId != Guid.Empty && !string.IsNullOrWhiteSpace(position.BlockCode) && position.Bay >= 0 && position.Row >= 0 && position.Tier >= 0);
        var validShape = eventType switch
        {
            ContainerMovementType.GateIn or ContainerMovementType.Place => source is null && destination is not null,
            ContainerMovementType.Relocate => source is not null && destination is not null,
            ContainerMovementType.Remove => source is not null && destination is null,
            ContainerMovementType.GateOut => destination is null && deliveryOrderId is not null,
            ContainerMovementType.GateInCancelled => destination is null,
            ContainerMovementType.GateOutCancelled => source is null,
            _ => false
        };
        var validVehicle = eventType is not (ContainerMovementType.GateIn or ContainerMovementType.GateOut) ||
            (!string.IsNullOrWhiteSpace(vehicleNumber) && vehicleType is not null && Enum.IsDefined(vehicleType.Value));
        if (containerId == Guid.Empty || lifecycleId == Guid.Empty || string.IsNullOrWhiteSpace(tenantId) || tenantId.Trim().Length > 50 ||
            occurredAtUtc.Kind != DateTimeKind.Utc || !validShape || !validVehicle || !ValidPosition(source) || !ValidPosition(destination))
            return Result.Failure<ContainerMovement>(Error.Validation("Movement.InvalidData", "Dữ liệu di chuyển không hợp lệ."));

        return Result.Success(new ContainerMovement
        {
            TenantId = tenantId.Trim(), ContainerId = containerId, ContainerLifecycleId = lifecycleId, EventType = eventType,
            OccurredAtUtc = occurredAtUtc, ActorUserId = actorUserId, FromPosition = source, ToPosition = destination,
            VehicleNumber = vehicleNumber?.Trim().ToUpperInvariant(), VehicleType = vehicleType, DeliveryOrderId = deliveryOrderId
        });
    }
}
