using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;
using Error = SnpDepotManagement.Domain.Common.Error;

namespace SnpDepotManagement.Domain.Entities.Operations;

public sealed class ContainerLifecycle: AuditableEntity
{
    public Guid ContainerId { get; private set; } 
    public string LineOperator { get; private set; } = string.Empty;
    public ContainerClassification Classification { get; private set; }
    public ContainerCondition Condition { get; private set; }
    // Thông tin chiều nhập bãi(Gate - In)
    public string InVehicleNumber { get; private set; } = string.Empty; // Biển số xe chở cont vào bãi
    public DateTime InDate { get; private set; }                       // Thời điểm vào bãi

    // Thông tin chiều xuất bãi (Gate-Out)
    public string? OutVehicleNumber { get; private set; }              // Biển số xe lấy cont ra khỏi bãi
    public DateTime? OutDate { get; private set; }                     // Thời điểm ra khỏi bãi
    public Guid? DeliveryOrderId { get; private set; }                 // Lệnh cấp vỏ dùng để xuất cont

    // Khóa ngoại vị trí slot bãi hiện tại mà cont đang chiếm giữ
    public Guid? CurrentSlotId { get; private set; }

    // Biểu thức kiểm tra: container đang nằm trong bãi nếu có gắn slot và chưa ghi nhận ngày ra
    public bool IsInDepot => CurrentSlotId.HasValue && OutDate == null;

    private ContainerLifecycle() { }

    /// 
    /// Factory Method: Tiếp nhận container qua cổng vào bãi (Gate-In)
    /// 
    public static ContainerLifecycle GateIn(
        Guid containerId,
        string lineOperator,
        ContainerClassification classification,
        ContainerCondition condition,
        string inVehicleNumber,
        Guid slotId)
    {
        return new ContainerLifecycle
        {
            ContainerId = containerId,
            LineOperator = lineOperator.Trim().ToUpperInvariant(),
            Classification = classification,
            Condition = condition,
            InVehicleNumber = inVehicleNumber.Trim().ToUpperInvariant(),
            InDate = DateTime.UtcNow,
            CurrentSlotId = slotId // Ghi nhận vị trí lưu bãi
        };
    }

    /// 
    /// Nghiệp vụ xuất container rời khỏi depot (Gate-Out)
    /// 
    public Result GateOut(string outVehicleNumber, Guid deliveryOrderId)
    {
        // Kiểm tra xem container có thực sự đang nằm trong depot hay không
        if (!IsInDepot)
            return Result.Failure(Error.Conflict("Lifecycle.NotInDepot", "Container hiện không nằm trong bãi lưu trữ."));

        OutVehicleNumber = outVehicleNumber.Trim().ToUpperInvariant();
        OutDate = DateTime.UtcNow;
        DeliveryOrderId = deliveryOrderId;

        // Rời khỏi vị trí bãi: đặt slot về null để đánh dấu giải phóng ô bãi
        CurrentSlotId = null;

        return Result.Success();
    }


}
