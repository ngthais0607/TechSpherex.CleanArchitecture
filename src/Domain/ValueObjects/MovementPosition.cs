namespace SnpDepotManagement.Domain.ValueObjects;

public sealed record MovementPosition(Guid BlockId, string BlockCode, int Bay, int Row, int Tier);
