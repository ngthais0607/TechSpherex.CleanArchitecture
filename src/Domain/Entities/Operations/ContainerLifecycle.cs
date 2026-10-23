using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.Entities.Operations;

public sealed class ContainerLifecycle : AuditableEntity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public Guid ContainerId { get; private set; }
    public string LineOperator { get; private set; } = string.Empty;
    public Classification Classification { get; private set; }
    public ContainerCondition Condition { get; private set; }
    public LifecycleStatus Status { get; private set; }

    public string InVehicleNumber { get; private set; } = string.Empty;
    public VehicleType InVehicleType { get; private set; } = VehicleType.Truck;
    public DateTime InDate { get; private set; }

    public string? OutVehicleNumber { get; private set; }
    public VehicleType? OutVehicleType { get; private set; }
    public DateTime? OutDate { get; private set; }
    public Guid? DeliveryOrderId { get; private set; }

    public Guid? CurrentSlotId { get; private set; }
    public Guid? VirtualBlockId { get; private set; }
    public int MovementHistoryVersion { get; private set; }

    public bool IsInDepot => Status == LifecycleStatus.InYard && OutDate == null;

    private ContainerLifecycle() { }

    public static ContainerLifecycle GateIn(
        Guid containerId,
        string lineOperator,
        Classification classification,
        ContainerCondition condition,
        string inVehicleNumber,
        Guid? slotId,
        DateTime utcNow,
        string tenantId = "",
        VehicleType vehicleType = VehicleType.Truck,
        Guid? virtualBlockId = null)
    {
        return new ContainerLifecycle
        {
            ContainerId = containerId,
            LineOperator = lineOperator.Trim().ToUpperInvariant(),
            Classification = classification,
            Condition = condition,
            Status = LifecycleStatus.InYard,
            InVehicleNumber = inVehicleNumber.Trim().ToUpperInvariant(),
            InVehicleType = vehicleType,
            InDate = utcNow,
            CurrentSlotId = slotId,
            VirtualBlockId = virtualBlockId,
            MovementHistoryVersion = 1,
            TenantId = tenantId
        };
    }

    public void SetCurrentSlot(Guid? slotId)
    {
        if (!IsInDepot) throw new InvalidOperationException("Only an active lifecycle can change yard position.");
        CurrentSlotId = slotId;
        if (slotId != null)
        {
            VirtualBlockId = null;
        }
    }

    public void SetVirtualBlock(Guid virtualBlockId)
    {
        if (!IsInDepot) throw new InvalidOperationException("Chỉ lifecycle đang hoạt động mới được thay đổi vị trí.");
        VirtualBlockId = virtualBlockId;
        CurrentSlotId = null;
    }

    public Result GateOut(string outVehicleNumber, Guid deliveryOrderId, DateTime utcNow, VehicleType vehicleType = VehicleType.Truck)
    {
        if (Status == LifecycleStatus.GatedOut || OutDate != null)
        {
            return Result.Failure(
                Error.Conflict("Lifecycle.AlreadyGatedOut", "Container đã được xuất bãi."));
        }

        if (Status != LifecycleStatus.InYard)
        {
            return Result.Failure(
                Error.Conflict("Lifecycle.NotInYard", "Container hiện không có trong bãi."));
        }

        Status = LifecycleStatus.GatedOut;
        OutVehicleNumber = outVehicleNumber.Trim().ToUpperInvariant();
        OutVehicleType = vehicleType;
        OutDate = utcNow;
        DeliveryOrderId = deliveryOrderId;
        CurrentSlotId = null;

        return Result.Success();
    }

    public void CancelGateIn(DateTime utcNow)
    {
        Status = LifecycleStatus.GatedOut;
        OutDate = utcNow;
        CurrentSlotId = null;
        VirtualBlockId = null;
    }

    public void CancelGateOut(Guid? restoredSlotId, Guid? restoredVirtualBlockId = null)
    {
        Status = LifecycleStatus.InYard;
        OutVehicleNumber = null;
        OutVehicleType = null;
        OutDate = null;
        DeliveryOrderId = null;
        CurrentSlotId = restoredSlotId;
        VirtualBlockId = restoredVirtualBlockId;
    }

    public void UpdateConditionAndClassification(ContainerCondition? condition, Classification? grade)
    {
        if (condition.HasValue) Condition = condition.Value;
        if (grade.HasValue) Classification = grade.Value;
    }
}
