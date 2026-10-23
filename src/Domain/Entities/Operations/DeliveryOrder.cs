using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Domain.Entities.Operations;

public sealed class DeliveryOrder : AuditableEntity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string OrderNumber { get; private set; } = string.Empty;
    public string LineOperator { get; private set; } = string.Empty;
    public string CustomerTaxCode { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public DateTime ExpiryDate { get; private set; }
    public string OutboundVessel { get; private set; } = string.Empty;
    public bool IsCompleted { get; private set; }

    private readonly List<DeliveryOrderItem> _items = new();
    public IReadOnlyCollection<DeliveryOrderItem> Items => _items.AsReadOnly();

    private DeliveryOrder() { }

    public static Result<DeliveryOrder> Create(
        string orderNumber,
        string lineOperator,
        string customerTaxCode,
        string customerName,
        DateTime expiryDate,
        string outboundVessel,
        string tenantId = "")
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return Result.Failure<DeliveryOrder>(
                Error.Validation("DeliveryOrder.EmptyOrderNumber", "Số lệnh không được để trống."));
        }

        if (string.IsNullOrWhiteSpace(lineOperator))
        {
            return Result.Failure<DeliveryOrder>(
                Error.Validation("DeliveryOrder.EmptyLineOperator", "Hãng tàu không được để trống."));
        }

        if (string.IsNullOrWhiteSpace(customerTaxCode))
        {
            return Result.Failure<DeliveryOrder>(
                Error.Validation("DeliveryOrder.EmptyCustomerTaxCode", "Mã số thuế khách hàng không được để trống."));
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            return Result.Failure<DeliveryOrder>(
                Error.Validation("DeliveryOrder.EmptyCustomerName", "Tên khách hàng không được để trống."));
        }

        if (string.IsNullOrWhiteSpace(outboundVessel))
        {
            return Result.Failure<DeliveryOrder>(
                Error.Validation("DeliveryOrder.EmptyOutboundVessel", "Tàu xuất không được để trống."));
        }

        return Result.Success(new DeliveryOrder
        {
            OrderNumber = orderNumber.Trim().ToUpperInvariant(),
            LineOperator = lineOperator.Trim().ToUpperInvariant(),
            CustomerTaxCode = customerTaxCode.Trim(),
            CustomerName = customerName.Trim(),
            ExpiryDate = expiryDate,
            OutboundVessel = outboundVessel.Trim().ToUpperInvariant(),
            IsCompleted = false,
            TenantId = tenantId
        });
    }

    public Result AddItem(ContainerSize size, ContainerType type, int quantity)
    {
        if (IsCompleted)
        {
            return Result.Failure(
                Error.Conflict("DeliveryOrder.AlreadyCompleted", "Không thể chỉnh sửa các mục trong lệnh giao đã hoàn tất."));
        }

        if (quantity <= 0)
        {
            return Result.Failure(
                Error.Validation("DeliveryOrder.InvalidQuantity", "Số lượng phải lớn hơn 0."));
        }

        var item = new DeliveryOrderItem(Id, size, type, quantity);
        _items.Add(item);
        return Result.Success();
    }

    public Result ValidateForDispatch(DateTime utcNow)
    {
        var rule = new DeliveryOrderNotExpiredRule(ExpiryDate, utcNow);
        if (rule.IsBroken())
        {
            return Result.Failure(rule.Error);
        }

        if (IsCompleted)
        {
            return Result.Failure(
                Error.Conflict("DeliveryOrder.AlreadyCompleted", "Lệnh giao đã hoàn tất."));
        }

        return Result.Success();
    }

    public OrderDisplayStatus GetDisplayStatus(DateTime utcNow)
    {
        var expiry = DateOnly.FromDateTime(ExpiryDate);
        var now = DateOnly.FromDateTime(utcNow);

        if (expiry < now)
        {
            return OrderDisplayStatus.Expired;
        }

        if (IsCompleted || (_items.Count > 0 && _items.All(i => i.RemainingQuantity == 0)))
        {
            return OrderDisplayStatus.FullyDelivered;
        }

        if ((expiry.DayNumber - now.DayNumber) <= 3)
        {
            return OrderDisplayStatus.ExpiringSoon;
        }

        return OrderDisplayStatus.Active;
    }

    public void MarkCompleted() => IsCompleted = true;
    public void Reopen() => IsCompleted = false;
}

public sealed class DeliveryOrderItem : BaseEntity
{
    public Guid DeliveryOrderId { get; private set; }
    public ContainerSize Size { get; private set; }
    public ContainerType Type { get; private set; }
    public int RequiredQuantity { get; private set; }
    public int DeliveredQuantity { get; private set; }
    public int RemainingQuantity => RequiredQuantity - DeliveredQuantity;

    private DeliveryOrderItem() { }

    internal DeliveryOrderItem(Guid orderId, ContainerSize size, ContainerType type, int requiredQuantity)
    {
        DeliveryOrderId = orderId;
        Size = size;
        Type = type;
        RequiredQuantity = requiredQuantity;
        DeliveredQuantity = 0;
    }

    public Result Allocate(int quantity = 1)
    {
        if (quantity <= 0)
        {
            return Result.Failure(
                Error.Validation("DeliveryOrderItem.InvalidQuantity", "Số lượng phân bổ phải lớn hơn 0."));
        }

        if (quantity > RemainingQuantity)
        {
            return Result.Failure(
                Error.Conflict("DeliveryOrderItem.QuantityExceeded", "Số lượng yêu cầu vượt quá số lượng còn lại."));
        }

        DeliveredQuantity += quantity;
        return Result.Success();
    }

    public Result Deallocate(int quantity = 1)
    {
        if (quantity <= 0)
        {
            return Result.Failure(
                Error.Validation("DeliveryOrderItem.InvalidQuantity", "Số lượng hoàn trả phải lớn hơn 0."));
        }

        if (quantity > DeliveredQuantity)
        {
            return Result.Failure(
                Error.Conflict("DeliveryOrderItem.QuantityExceeded", "Số lượng hoàn trả vượt quá số lượng đã giao."));
        }

        DeliveredQuantity -= quantity;
        return Result.Success();
    }
}
