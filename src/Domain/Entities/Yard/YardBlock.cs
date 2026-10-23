using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
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
        var sizeRule = new YardBlockSizeRule(bays, rows, tiers);
        if (sizeRule.IsBroken()) throw new BusinessRuleException(sizeRule);

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
                    _slots.Add(new YardSlot(Id, bay, row, tier, Code, TenantId));
                }
            }
        }
    }

    public Result Expand(int newMaxBays, int newMaxRows, int newMaxTiers)
    {
        if (!YardBlockLimits.IsWithinLimits(newMaxBays, newMaxRows, newMaxTiers))
            return Result.Failure(YardBlockLimits.InvalidSize);

        if (IsVirtual)
        {
            return Result.Failure(
                Error.Conflict("YardBlock.VirtualCannotExpand", "Không thể mở rộng dãy ảo."));
        }

        if (newMaxBays < MaxBays || newMaxRows < MaxRows || newMaxTiers < MaxTiers)
        {
            return Result.Failure(
                Error.Validation("YardBlock.InvalidExpansion", "Kích thước mới phải lớn hơn hoặc bằng kích thước hiện tại."));
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
                        _slots.Add(new YardSlot(Id, bay, row, tier, Code, TenantId));
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
        if (!YardBlockLimits.IsWithinLimits(newMaxBays, newMaxRows, newMaxTiers))
            return Result.Failure(YardBlockLimits.InvalidSize);

        if (IsVirtual)
        {
            return Result.Failure(
                Error.Conflict("YardBlock.VirtualCannotShrink", "Không thể thu hẹp dãy ảo."));
        }

        if (newMaxBays > MaxBays || newMaxRows > MaxRows || newMaxTiers > MaxTiers)
        {
            return Result.Failure(
                Error.Validation("YardBlock.InvalidShrink", "Kích thước mới phải nhỏ hơn hoặc bằng kích thước hiện tại."));
        }

        var slotsToRemove = _slots
            .Where(s => s.Bay > newMaxBays || s.Row > newMaxRows || s.Tier > newMaxTiers)
            .ToList();

        var firstOccupied = slotsToRemove.FirstOrDefault(s => s.IsOccupied);
        if (firstOccupied is not null)
        {
            return Result.Failure(
                Error.Conflict("YardBlock.CannotShrinkOccupiedSlots", $"Không thể thu hẹp dãy vì vị trí {firstOccupied.Position.Bay:D2}-{firstOccupied.Position.Row:D2}-{firstOccupied.Position.Tier:D2} đang có container."));
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
