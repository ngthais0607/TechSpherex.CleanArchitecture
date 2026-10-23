using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Containers.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Containers.Queries.GetContainers;

public sealed record GetContainersQuery(
    string? Search = null,
    string? OwnerCode = null,
    ContainerSize? Size = null,
    ContainerCondition? Condition = null,
    int PageIndex = 1,
    int PageSize = 20,
    ContainerType? Type = null,
    string? LineOperator = null,
    string? DisplayStatus = null,
    string? Booking = null,
    string? Sort = null,
    bool Descending = false) : IQuery<PagedResult<ContainerDto>>;
