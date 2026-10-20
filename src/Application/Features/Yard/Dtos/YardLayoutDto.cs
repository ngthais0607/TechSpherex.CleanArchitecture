namespace SnpDepotManagement.Application.Features.Yard.Dtos;

public sealed record YardLayoutDto(
    Guid BlockId,
    string Code,
    string Name,
    bool IsVirtual,
    int MaxBays,
    int OddBays,
    int MaxRows,
    int MaxTiers,
    IReadOnlyList<YardSlotDto> Slots,
    IReadOnlyList<BlockContainerDto> Containers);
