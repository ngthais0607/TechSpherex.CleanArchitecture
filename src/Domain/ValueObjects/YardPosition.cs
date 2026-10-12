using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.ValueObjects;

public sealed record YardPosition
{
    public string Block { get; private set; } = string.Empty;
    public int Bay { get; private set; }
    public int Row { get; private set; }
    public int Tier { get; private set; }

    private YardPosition() { }

    private YardPosition(string block, int bay, int row, int tier)
    {
        Block = block;
        Bay = bay;
        Row = row;
        Tier = tier;
    }

    public static Result<YardPosition> Create(string? block, int bay, int row, int tier)
    {
        if (string.IsNullOrWhiteSpace(block))
        {
            return Result.Failure<YardPosition>(
                Error.Validation("YardPosition.EmptyBlock", "Block cannot be empty."));
        }

        if (bay <= 0 || row <= 0 || tier <= 0)
        {
            return Result.Failure<YardPosition>(
                Error.Validation("YardPosition.InvalidCoordinate", "Bay, Row, and Tier must be greater than zero."));
        }

        return Result.Success(new YardPosition(block.Trim().ToUpperInvariant(), bay, row, tier));
    }

    public override string ToString() => $"{Block}-{Bay:D2}-{Row:D2}-{Tier:D2}";
}
