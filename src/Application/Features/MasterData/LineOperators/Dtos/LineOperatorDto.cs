namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Dtos;

public sealed record LineOperatorDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive = true,
    int ContainersInYard = 0,
    int OrderCount = 0,
    int ActiveOrderCount = 0);
