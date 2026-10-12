namespace SnpDepotManagement.Application.Features.Yard.Dtos;

public sealed record YardSlotDto(
    Guid Id,
    int Bay,
    int Row,
    int Tier,
    Guid? ContainerId,
    bool IsOccupied);
