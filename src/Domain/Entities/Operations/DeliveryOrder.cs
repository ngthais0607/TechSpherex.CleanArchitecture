using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SnpDepotManagement.Domain.Entities.Operations;

public sealed class DeliveryOrder : AuditableEntity 
{
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

    public static DeliveryOrder Create(
        string orderNumber,
        string lineOperator,
        string customerTaxCode,
        string customerName,
        DateTime expiryDate,
        string outboundVessel)
    {
        return new DeliveryOrder
        {
            OrderNumber = orderNumber.Trim().ToUpperInvariant(),
            LineOperator = lineOperator.Trim().ToUpperInvariant(),
            CustomerTaxCode = customerTaxCode.Trim(),
            CustomerName = customerName.Trim(),
            ExpiryDate = expiryDate,
            OutboundVessel = outboundVessel.Trim(),
            IsCompleted = false
        };
    }

    /// 
    /// Thêm số lượng container được phép cấp theo kích thước và chủng loại
    /// 
    public Result AddItem(ContainerSize size, ContainerType type, int quantity)
    {
        // Không cho phép bổ sung mặt hàng khi lệnh đã chốt hoàn thành
        if (IsCompleted)
            return Result.Failure(Error.Conflict("Order.Completed", "Không thể chỉnh sửa lệnh giao đã hoàn thành."));

        if (quantity <= 0)
            return Result.Failure(Error.Validation("Order.InvalidQuantity", "Số lượng cấp phải lớn hơn 0."));

        _items.Add(new DeliveryOrderItem(Id, size, type, quantity));
        return Result.Success();
    }

    /// 
    /// Kiểm tra lệnh có đủ điều kiện pháp lý để bốc cont ra cổng hay không[cite: 3].
    /// 
    public Result ValidateForDispatch(DateTime utcNow)
    {
        // Kích hoạt rule kiểm tra hạn lệnh[cite: 3]
        var rule = new DeliveryOrderNotExpiredRule(ExpiryDate, utcNow);
        if (rule.IsBroken())
            return Result.Failure(Error.Validation("Order.Expired", rule.Message));

        if (IsCompleted)
            return Result.Failure(Error.Conflict("Order.AlreadyCompleted", "Lệnh giao đã hoàn tất việc xuất cont."));

        return Result.Success();
    }

    public void MarkCompleted() => IsCompleted = true;
}

/// 
/// Chi tiết số lượng vỏ được phép cấp theo từng loại và kích thước[cite: 3].
/// 
public sealed class DeliveryOrderItem : BaseEntity
{
    public Guid DeliveryOrderId { get; private set; }
    public ContainerSize Size { get; private set; }
    public ContainerType Type { get; private set; }
    public int Quantity { get; private set; } // Số lượng cont cần cấp[cite: 3]

    internal DeliveryOrderItem(Guid orderId, ContainerSize size, ContainerType type, int quantity)
    {
        DeliveryOrderId = orderId;
        Size = size;
        Type = type;
        Quantity = quantity;
    }

}
