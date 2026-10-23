using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.Rules;

public sealed class SlotBelowMustBeOccupiedRule : IBusinessRule
{
    private readonly int _tier;
    private readonly ContainerSize _size;
    private readonly int _bay;
    private readonly int _row;
    private readonly IEnumerable<YardSlot> _slots;

    public SlotBelowMustBeOccupiedRule(
        int tier,
        ContainerSize size,
        int bay,
        int row,
        IEnumerable<YardSlot> slots)
    {
        _tier = tier;
        _size = size;
        _bay = bay;
        _row = row;
        _slots = slots;
    }

    public Error Error => Error.Validation(
        "YardRule.SlotBelowNotOccupied",
        $"Không thể đặt container tại tầng '{_tier}' vì vị trí đỡ bên dưới tại tầng '{_tier - 1}' đang trống.");

    public string Message => Error.Description;

    public bool IsBroken()
    {
        if (_tier <= 1)
        {
            return false;
        }

        int tierBelow = _tier - 1;

        if (_size == ContainerSize.Ft20)
        {
            var slotBelow = _slots.FirstOrDefault(s => s.Bay == _bay && s.Row == _row && s.Tier == tierBelow);
            return slotBelow == null || !slotBelow.IsOccupied;
        }

        if (_size == ContainerSize.Ft40)
        {
            int bayBefore = _bay - 1;
            int bayAfter = _bay + 1;

            var slot1 = _slots.FirstOrDefault(s => s.Bay == bayBefore && s.Row == _row && s.Tier == tierBelow);
            var slot2 = _slots.FirstOrDefault(s => s.Bay == bayAfter && s.Row == _row && s.Tier == tierBelow);

            return slot1 == null || !slot1.IsOccupied || slot2 == null || !slot2.IsOccupied;
        }

        return false;
    }
}
