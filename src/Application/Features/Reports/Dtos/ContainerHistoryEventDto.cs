using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.Features.Reports.Dtos;

public sealed record ContainerHistoryEventDto(string EventId, string Source, string EventType, Guid LifecycleId,
    DateTime OccurredAtUtc, MovementPosition? FromPosition, MovementPosition? ToPosition,
    Guid? ActorUserId, string? VehicleNumber, VehicleType? VehicleType, Guid? DeliveryOrderId);
