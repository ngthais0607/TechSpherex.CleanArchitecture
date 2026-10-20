using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Containers.Queries.GetContainerSummary;

public sealed record GetContainerSummaryQuery(string? OwnerCode = null) : IQuery<ContainerSummaryDto>;

public sealed record ContainerSummaryDto(
    int Total,
    IReadOnlyDictionary<string, int> BySize,
    IReadOnlyDictionary<string, int> ByType,
    IReadOnlyDictionary<string, int> ByCondition);
