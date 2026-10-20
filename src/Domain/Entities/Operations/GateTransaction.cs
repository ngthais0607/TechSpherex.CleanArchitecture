using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.Entities.Operations;

public enum GateTransactionStatus
{
    Active = 1,
    Cancelled = 2
}

public enum GateTransactionType
{
    GateIn = 1,
    GateOut = 2
}

public sealed class GateTransaction : AuditableEntity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public Guid ContainerId { get; private set; }
    public GateTransactionType Type { get; private set; }
    public GateTransactionStatus Status { get; private set; } = GateTransactionStatus.Active;
    public string VehicleNumber { get; private set; } = string.Empty;
    public VehicleType VehicleType { get; private set; } = VehicleType.Truck;
    public string? DriverName { get; private set; }
    public Guid? SlotId { get; private set; }
    public string? VirtualBlockCode { get; private set; }
    public Guid? OrderId { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelledBy { get; private set; }

    private readonly List<GateTransactionChange> _changes = new();
    public IReadOnlyCollection<GateTransactionChange> Changes => _changes.AsReadOnly();

    private GateTransaction() { }

    public static GateTransaction CreateGateIn(
        Guid containerId,
        string vehicleNumber,
        VehicleType vehicleType,
        string? driverName,
        Guid? slotId,
        string? virtualBlockCode,
        string tenantId)
    {
        return new GateTransaction
        {
            ContainerId = containerId,
            Type = GateTransactionType.GateIn,
            Status = GateTransactionStatus.Active,
            VehicleNumber = vehicleNumber.Trim().ToUpperInvariant(),
            VehicleType = vehicleType,
            DriverName = driverName?.Trim(),
            SlotId = slotId,
            VirtualBlockCode = virtualBlockCode?.Trim().ToUpperInvariant(),
            TenantId = tenantId
        };
    }

    public static GateTransaction CreateGateOut(
        Guid containerId,
        Guid orderId,
        string vehicleNumber,
        VehicleType vehicleType,
        string? driverName,
        Guid? slotId,
        string tenantId)
    {
        return new GateTransaction
        {
            ContainerId = containerId,
            OrderId = orderId,
            Type = GateTransactionType.GateOut,
            Status = GateTransactionStatus.Active,
            VehicleNumber = vehicleNumber.Trim().ToUpperInvariant(),
            VehicleType = vehicleType,
            DriverName = driverName?.Trim(),
            SlotId = slotId,
            TenantId = tenantId
        };
    }

    public Result Cancel(DateTime utcNow, string? cancelledBy = null)
    {
        if (Status == GateTransactionStatus.Cancelled)
        {
            return Result.Failure(Error.Conflict("GateTransaction.AlreadyCancelled", "Giao dịch cổng đã được hủy trước đó."));
        }

        Status = GateTransactionStatus.Cancelled;
        CancelledAt = utcNow;
        CancelledBy = cancelledBy;
        return Result.Success();
    }

    public void RecordChange(string fieldName, string? oldValue, string? newValue, DateTime utcNow, string? changedBy = null)
    {
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal)) return;

        var change = GateTransactionChange.Create(Id, fieldName, oldValue, newValue, utcNow, changedBy);
        _changes.Add(change);
    }

    public void UpdateDetails(string vehicleNumber, VehicleType vehicleType, string? driverName, DateTime utcNow, string? updatedBy = null)
    {
        var normVehicle = vehicleNumber.Trim().ToUpperInvariant();
        if (VehicleNumber != normVehicle)
        {
            RecordChange(nameof(VehicleNumber), VehicleNumber, normVehicle, utcNow, updatedBy);
            VehicleNumber = normVehicle;
        }

        if (VehicleType != vehicleType)
        {
            RecordChange(nameof(VehicleType), VehicleType.ToString(), vehicleType.ToString(), utcNow, updatedBy);
            VehicleType = vehicleType;
        }

        var normDriver = driverName?.Trim();
        if (DriverName != normDriver)
        {
            RecordChange(nameof(DriverName), DriverName, normDriver, utcNow, updatedBy);
            DriverName = normDriver;
        }
    }
}
