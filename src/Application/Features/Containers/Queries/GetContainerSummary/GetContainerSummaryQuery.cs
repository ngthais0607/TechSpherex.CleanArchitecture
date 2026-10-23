using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Containers.Queries.GetContainerSummary;

public sealed record GetContainerSummaryQuery(string? OwnerCode = null, string? Search = null,
    ContainerSize? Size = null, ContainerCondition? Condition = null, ContainerType? Type = null,
    string? LineOperator = null, string? DisplayStatus = null, string? Booking = null) : IQuery<ContainerSummaryDto>;

public sealed record ContainerSummaryDto(
    int Total,
    IReadOnlyDictionary<string, int> BySize,
    IReadOnlyDictionary<string, int> ByType,
    IReadOnlyDictionary<string, int> ByCondition);
