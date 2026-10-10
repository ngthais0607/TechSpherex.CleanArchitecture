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
                Error.Validation("DeliveryOrder.EmptyOrderNumber", "Order number cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(lineOperator))
        {
            return Result.Failure<DeliveryOrder>(
                Error.Validation("DeliveryOrder.EmptyLineOperator", "Line operator cannot be empty."));
        }

        return Result.Success(new DeliveryOrder
        {
            OrderNumber = orderNumber.Trim().ToUpperInvariant(),
            LineOperator = lineOperator.Trim().ToUpperInvariant(),
            CustomerTaxCode = customerTaxCode?.Trim() ?? string.Empty,
            CustomerName = customerName?.Trim() ?? string.Empty,
            ExpiryDate = expiryDate,
            OutboundVessel = outboundVessel?.Trim() ?? string.Empty,
            IsCompleted = false,
            TenantId = tenantId
        });
    }

    public Result AddItem(ContainerSize size, ContainerType type, int quantity)
    {
        if (IsCompleted)
        {
            return Result.Failure(
                Error.Conflict("DeliveryOrder.AlreadyCompleted", "Cannot modify items in a completed delivery order."));
        }

        if (quantity <= 0)
        {
            return Result.Failure(
                Error.Validation("DeliveryOrder.InvalidQuantity", "Quantity must be greater than zero."));
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
                Error.Conflict("DeliveryOrder.AlreadyCompleted", "Delivery order has already been completed."));
        }

        return Result.Success();
    }

    public void MarkCompleted() => IsCompleted = true;
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
                Error.Validation("DeliveryOrderItem.InvalidQuantity", "Allocation quantity must be greater than zero."));
        }

        if (quantity > RemainingQuantity)
        {
            return Result.Failure(
                Error.Conflict("DeliveryOrderItem.QuantityExceeded", "Requested quantity exceeds remaining quantity."));
        }

        DeliveredQuantity += quantity;
        return Result.Success();
    }
}
