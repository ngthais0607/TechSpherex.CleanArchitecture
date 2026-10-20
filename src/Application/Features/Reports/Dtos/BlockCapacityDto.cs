namespace SnpDepotManagement.Application.Features.Reports.Dtos;

public sealed record BlockCapacityDto(
    Guid BlockId,
    string BlockCode,
    string BlockName,
    int TotalCapacityTeu,
    int OccupiedTeu,
    double OccupancyRate,
    int ContainersCount);
