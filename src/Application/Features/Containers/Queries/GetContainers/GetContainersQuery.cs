using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Containers.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Containers.Queries.GetContainers;

public sealed record GetContainersQuery(
    string? OwnerCode = null,
    ContainerSize? Size = null,
    ContainerCondition? Condition = null,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<ContainerDto>>;
