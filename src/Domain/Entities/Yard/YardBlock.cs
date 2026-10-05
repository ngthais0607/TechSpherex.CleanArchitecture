using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.Entities.Yard;

public sealed class YardBlock : AuditableEntity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsVirtual { get; private set; }
    public YardBlockType BlockType { get; private set; }

    public int MaxBays { get; private set; }
    public int MaxRows { get; private set; }
    public int MaxTiers { get; private set; }

    private readonly List<YardSlot> _slots = new();
    public IReadOnlyCollection<YardSlot> Slots => _slots.AsReadOnly();

    private YardBlock() { }

    public static YardBlock CreateStandard(
        string code,
        string name,
        int bays,
        int rows,
        int tiers,
        string tenantId = "")
    {
        var block = new YardBlock
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            IsVirtual = false,
            BlockType = YardBlockType.Standard,
            MaxBays = Math.Max(0, bays),
            MaxRows = Math.Max(0, rows),
            MaxTiers = Math.Max(0, tiers),
            TenantId = tenantId
        };

        block.InitializeSlots();
        return block;
    }

    public static YardBlock CreateVirtual(string code, string name, string tenantId = "")
    {
        return new YardBlock
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            IsVirtual = true,
            BlockType = YardBlockType.Virtual,
            MaxBays = 0,
            MaxRows = 0,
            MaxTiers = 0,
            TenantId = tenantId
        };
    }

    private void InitializeSlots()
    {
        // YardSlot only exists at odd bays (20ft standard slots)
        for (int bay = 1; bay <= MaxBays; bay += 2)
        {
            for (int row = 1; row <= MaxRows; row++)
            {
                for (int tier = 1; tier <= MaxTiers; tier++)
                {
                    _slots.Add(new YardSlot(Id, bay, row, tier));
                }
            }
        }
    }

    public Result Expand(int newMaxBays, int newMaxRows, int newMaxTiers)
    {
        if (IsVirtual)
        {
            return Result.Failure(
                Error.Conflict("YardBlock.VirtualCannotExpand", "Virtual blocks cannot be expanded."));
        }

        if (newMaxBays < MaxBays || newMaxRows < MaxRows || newMaxTiers < MaxTiers)
        {
            return Result.Failure(
                Error.Validation("YardBlock.InvalidExpansion", "New dimensions must be greater than or equal to current dimensions."));
        }

        for (int bay = 1; bay <= newMaxBays; bay += 2)
        {
            for (int row = 1; row <= newMaxRows; row++)
            {
                for (int tier = 1; tier <= newMaxTiers; tier++)
                {
                    bool exists = _slots.Any(s => s.Bay == bay && s.Row == row && s.Tier == tier);
                    if (!exists)
                    {
                        _slots.Add(new YardSlot(Id, bay, row, tier));
                    }
                }
            }
        }

        MaxBays = newMaxBays;
        MaxRows = newMaxRows;
        MaxTiers = newMaxTiers;

        return Result.Success();
    }

    public Result Shrink(int newMaxBays, int newMaxRows, int newMaxTiers)
    {
        if (IsVirtual)
        {
            return Result.Failure(
                Error.Conflict("YardBlock.VirtualCannotShrink", "Virtual blocks cannot be shrunk."));
        }

        if (newMaxBays > MaxBays || newMaxRows > MaxRows || newMaxTiers > MaxTiers)
        {
            return Result.Failure(
                Error.Validation("YardBlock.InvalidShrink", "New dimensions must be less than or equal to current dimensions."));
        }

        var slotsToRemove = _slots
            .Where(s => s.Bay > newMaxBays || s.Row > newMaxRows || s.Tier > newMaxTiers)
            .ToList();

        if (slotsToRemove.Any(s => s.IsOccupied))
        {
            return Result.Failure(
                Error.Conflict("YardBlock.CannotShrinkOccupiedSlots", "Cannot shrink block because slots in the removed area are currently occupied."));
        }

        foreach (var slot in slotsToRemove)
        {
            _slots.Remove(slot);
        }

        MaxBays = newMaxBays;
        MaxRows = newMaxRows;
        MaxTiers = newMaxTiers;

        return Result.Success();
    }
}
