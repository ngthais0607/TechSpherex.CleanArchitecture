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

    private Error _error = Error.Validation("YardRule.SlotAlreadyOccupied", "The requested slot is already occupied.");

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
                    $"Slot at Bay '{_bay}', Row '{_row}', Tier '{_tier}' was not found.");
                return true;
            }

            if (slot.IsOccupied)
            {
                _error = Error.Conflict(
                    "YardRule.SlotAlreadyOccupied",
                    $"Slot at Bay '{_bay}', Row '{_row}', Tier '{_tier}' is already occupied.");
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
                    $"One or both required 40ft slots (Bays '{bayBefore}', '{bayAfter}') were not found.");
                return true;
            }

            if (slot1.IsOccupied || slot2.IsOccupied)
            {
                _error = Error.Conflict(
                    "YardRule.SlotAlreadyOccupied",
                    $"One or both slots for 40ft container (Bays '{bayBefore}', '{bayAfter}') are already occupied.");
                return true;
            }

            return false;
        }

        return false;
    }
}
