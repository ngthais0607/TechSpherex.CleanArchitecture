using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.Rules;

/// <summary>
/// Kiểm tra kích thước container bên dưới có tương thích không.
/// 20ft chỉ đặt lên 20ft. 40ft đặt lên 40ft cùng vị trí, hoặc hai 20ft liền nhau.
/// </summary>
public sealed class StackCompatibilityRule : IBusinessRule
{
    private readonly ContainerSize _size;
    private readonly int _bay, _row, _tier;
    // (slot, kích thước container đang chiếm slot đó — null nếu slot trống)
    private readonly IEnumerable<(int Bay, int Row, int Tier, Guid? ContainerId, ContainerSize? ContainerSize)> _slots;

    public StackCompatibilityRule(
        ContainerSize size, int bay, int row, int tier,
        IEnumerable<(int Bay, int Row, int Tier, Guid? ContainerId, ContainerSize? ContainerSize)> slots)
    {
        _size = size;
        _bay = bay;
        _row = row;
        _tier = tier;
        _slots = slots;
    }

    public StackCompatibilityRule(
        ContainerSize size, int bay, int row, int tier,
        IEnumerable<(YardSlot Slot, ContainerSize? ContainerSize)> slots)
        : this(size, bay, row, tier, slots.Select(s => (s.Slot.Bay, s.Slot.Row, s.Slot.Tier, s.Slot.ContainerId, s.ContainerSize)))
    {
    }

    public Error Error => Error.Validation(
        "YardRule.StackIncompatible",
        "Container bên dưới không tương thích về kích thước.");

    public bool IsBroken()
    {
        if (_tier <= 1) return false;
        int below = _tier - 1;

        if (_size == ContainerSize.Ft20)
        {
            // ô bên dưới phải là 20ft (hoặc trống — SlotBelowMustBeOccupied kiểm tra việc trống)
            var underSlot = _slots.FirstOrDefault(s => s.Bay == _bay && s.Row == _row && s.Tier == below);
            if (underSlot.ContainerId == null) return false; // trống → để SlotBelowMustBeOccupied xử lý
            return underSlot.ContainerSize == ContainerSize.Ft40;
        }

        if (_size == ContainerSize.Ft40)
        {
            int bayLeft = _bay - 1, bayRight = _bay + 1;
            var left  = _slots.FirstOrDefault(s => s.Bay == bayLeft  && s.Row == _row && s.Tier == below);
            var right = _slots.FirstOrDefault(s => s.Bay == bayRight && s.Row == _row && s.Tier == below);

            if (left.ContainerId == null || right.ContainerId == null) return false;

            // Hai 20ft ở bên dưới → OK
            if (left.ContainerSize == ContainerSize.Ft20 && right.ContainerSize == ContainerSize.Ft20)
                return false;

            // Một 40ft chiếm cả hai ô (cùng containerId) → OK
            if (left.ContainerId == right.ContainerId && left.ContainerSize == ContainerSize.Ft40)
                return false;

            return true; // không khớp
        }

        return false;
    }
}
