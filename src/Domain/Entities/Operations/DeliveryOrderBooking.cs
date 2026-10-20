using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.Operations;

public enum BookingStatus
{
    Reserved = 1,
    Shipped = 2,
    Cancelled = 3,
    Expired = 4
}

public sealed class DeliveryOrderBooking : BaseEntity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public Guid OrderId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid ContainerId { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTime ReservedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? ExpiredAt { get; private set; }

    private DeliveryOrderBooking() { }

    public static DeliveryOrderBooking Reserve(
        Guid orderId, Guid itemId, Guid containerId, string tenantId, DateTime utcNow)
        => new()
        {
            OrderId = orderId,
            ItemId = itemId,
            ContainerId = containerId,
            TenantId = tenantId,
            Status = BookingStatus.Reserved,
            ReservedAt = utcNow
        };

    public void Ship(DateTime utcNow)
    {
        Status = BookingStatus.Shipped;
        ShippedAt = utcNow;
    }

    public void Cancel(DateTime utcNow)
    {
        Status = BookingStatus.Cancelled;
        CancelledAt = utcNow;
    }

    public void Expire(DateTime utcNow)
    {
        Status = BookingStatus.Expired;
        ExpiredAt = utcNow;
    }

    public void RestoreReserved()
    {
        Status = BookingStatus.Reserved;
        ShippedAt = null;
    }
}
