namespace SnpDepotManagement.Application.Features.Yard.Queries.YardSearch;

public sealed record YardLocationDto(
    string? BlockCode,
    Guid? BlockId,
    int? Bay,
    int? Row,
    int? Tier,
    bool IsVirtual);

public sealed record YardSearchResult(
    string Status,
    YardLocationDto? Location);
