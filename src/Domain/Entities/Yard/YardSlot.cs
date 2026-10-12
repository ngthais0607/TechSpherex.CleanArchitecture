using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Domain.Entities.Yard;

public sealed class YardSlot : BaseEntity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public Guid BlockId { get; private set; }
    public YardPosition Position { get; private set; } = null!;

    public int Bay => Position.Bay;
    public int Row => Position.Row;
    public int Tier => Position.Tier;

    public Guid? ContainerId { get; private set; }
    public bool IsOccupied { get; private set; }

    private YardSlot() { }

    internal YardSlot(Guid blockId, YardPosition position, string tenantId = "")
    {
        BlockId = blockId;
        Position = position;
        TenantId = tenantId;
        ContainerId = null;
        IsOccupied = false;
    }

    internal YardSlot(Guid blockId, int bay, int row, int tier, string blockCode = "BLOCK", string tenantId = "")
        : this(blockId, YardPosition.Create(string.IsNullOrWhiteSpace(blockCode) ? "BLOCK" : blockCode, bay, row, tier).Value!, tenantId)
    {
    }

    public void Occupy(Guid containerId)
    {
        ContainerId = containerId;
        IsOccupied = true;
    }

    public void Release()
    {
        ContainerId = null;
        IsOccupied = false;
    }
}