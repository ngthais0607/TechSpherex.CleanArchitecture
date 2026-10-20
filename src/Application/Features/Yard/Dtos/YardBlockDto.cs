using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Yard.Dtos;

public sealed record YardBlockDto(
    Guid Id,
    string Code,
    string Name,
    bool IsVirtual,
    YardBlockType BlockType,
    int MaxBays,
    int OddBays,
    int MaxRows,
    int MaxTiers,
    int TotalSlots,
    int OccupiedSlots);
