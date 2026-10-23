using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.Rules;

public sealed class DeliveryOrderHasRemainingQuantityRule : IBusinessRule
{
    private readonly DeliveryOrder _order;
    private readonly ContainerSize _size;
    private readonly ContainerType _type;

    public DeliveryOrderHasRemainingQuantityRule(DeliveryOrder order, ContainerSize size, ContainerType type)
    {
        _order = order;
        _size = size;
        _type = type;
    }

    public Error Error => Error.Conflict(
        "OrderRule.NoRemainingQuantity",
        $"Lệnh giao '{_order.OrderNumber}' đã hết số lượng khả dụng cho kích thước '{_size}' và loại '{_type}'.");

    public string Message => Error.Description;

    public bool IsBroken()
    {
        return !_order.Items.Any(i => i.Size == _size && i.Type == _type && i.RemainingQuantity > 0);
    }
}
