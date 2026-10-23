using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.Rules;

public sealed class SlotIsFreeRule : IBusinessRule
{
    private readonly ContainerSize _size;
    private readonly int _bay;
    private readonly int _row;
    private readonly int _tier;
    private readonly IEnumerable<YardSlot> _slots;

    private Error _error = Error.Validation("YardRule.SlotAlreadyOccupied", "Vị trí yêu cầu đã có container.");

    public SlotIsFreeRule(
        ContainerSize size,
        int bay,
        int row,
        int tier,
        IEnumerable<YardSlot> slots)
    {
        _size = size;
        _bay = bay;
        _row = row;
        _tier = tier;
        _slots = slots;
    }

    public Error Error => _error;
    public string Message => _error.Description;

    public bool IsBroken()
    {
        if (_size == ContainerSize.Ft20)
        {
            var slot = _slots.FirstOrDefault(s => s.Bay == _bay && s.Row == _row && s.Tier == _tier);
            if (slot == null)
            {
                _error = Error.NotFound(
                    "YardRule.SlotNotFound",
                    $"Không tìm thấy vị trí tại Bay '{_bay}', Row '{_row}', Tier '{_tier}'.");
                return true;
            }

            if (slot.IsOccupied)
            {
                _error = Error.Conflict(
                    "YardRule.SlotAlreadyOccupied",
                    $"Vị trí tại Bay '{_bay}', Row '{_row}', Tier '{_tier}' đã có container.");
                return true;
            }

            return false;
        }

        if (_size == ContainerSize.Ft40)
        {
            int bayBefore = _bay - 1;
            int bayAfter = _bay + 1;

            var slot1 = _slots.FirstOrDefault(s => s.Bay == bayBefore && s.Row == _row && s.Tier == _tier);
            var slot2 = _slots.FirstOrDefault(s => s.Bay == bayAfter && s.Row == _row && s.Tier == _tier);

            if (slot1 == null || slot2 == null)
            {
                _error = Error.NotFound(
                    "YardRule.SlotNotFound",
                    $"Không tìm thấy một hoặc cả hai vị trí cho container 40ft (Bay '{bayBefore}', '{bayAfter}').");
                return true;
            }

            if (slot1.IsOccupied || slot2.IsOccupied)
            {
                _error = Error.Conflict(
                    "YardRule.SlotAlreadyOccupied",
                    $"Một hoặc cả hai vị trí cho container 40ft (Bay '{bayBefore}', '{bayAfter}') đã có container.");
                return true;
            }

            return false;
        }

        return false;
    }
}
