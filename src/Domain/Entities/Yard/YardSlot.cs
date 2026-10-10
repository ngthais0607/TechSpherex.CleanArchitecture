using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.Yard;

public sealed class YardSlot : BaseEntity
{
    public Guid BlockId { get; private set; }
    public int Bay { get; private set; }
    public int Row { get; private set; }
    public int Tier { get; private set; }
    public Guid? ContainerId { get; private set; }
    public bool IsOccupied { get; private set; }

    private YardSlot() { }

    internal YardSlot(Guid blockId, int bay, int row, int tier)
    {
        BlockId = blockId;
        Bay = bay;
        Row = row;
        Tier = tier;
        ContainerId = null;
        IsOccupied = false;
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