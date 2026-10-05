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
    public DateTime InDate { get; private set; }

    public string? OutVehicleNumber { get; private set; }
    public DateTime? OutDate { get; private set; }
    public Guid? DeliveryOrderId { get; private set; }

    public Guid? CurrentSlotId { get; private set; }

    public bool IsInDepot => Status == LifecycleStatus.InYard && OutDate == null;

    private ContainerLifecycle() { }

    public static ContainerLifecycle GateIn(
        Guid containerId,
        string lineOperator,
        Classification classification,
        ContainerCondition condition,
        string inVehicleNumber,
        Guid slotId,
        string tenantId = "")
    {
        return new ContainerLifecycle
        {
            ContainerId = containerId,
            LineOperator = lineOperator.Trim().ToUpperInvariant(),
            Classification = classification,
            Condition = condition,
            Status = LifecycleStatus.InYard,
            InVehicleNumber = inVehicleNumber.Trim().ToUpperInvariant(),
            InDate = DateTime.UtcNow,
            CurrentSlotId = slotId,
            TenantId = tenantId
        };
    }

    public Result GateOut(string outVehicleNumber, Guid deliveryOrderId)
    {
        if (Status == LifecycleStatus.GatedOut || OutDate != null)
        {
            return Result.Failure(
                Error.Conflict("Lifecycle.AlreadyGatedOut", "Container has already been gated out."));
        }

        if (Status != LifecycleStatus.InYard)
        {
            return Result.Failure(
                Error.Conflict("Lifecycle.NotInYard", "Container is not currently in the yard."));
        }

        Status = LifecycleStatus.GatedOut;
        OutVehicleNumber = outVehicleNumber.Trim().ToUpperInvariant();
        OutDate = DateTime.UtcNow;
        DeliveryOrderId = deliveryOrderId;
        CurrentSlotId = null;

        return Result.Success();
    }
}
