using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Dtos;

public sealed record ContainerLifecycleDto(
    Guid Id,
    Guid ContainerId,
    string LineOperator,
    Classification Classification,
    ContainerCondition Condition,
    LifecycleStatus Status,
    string InVehicleNumber,
    DateTime InDate,
    string? OutVehicleNumber,
    DateTime? OutDate,
    Guid? DeliveryOrderId,
    Guid? CurrentSlotId);
