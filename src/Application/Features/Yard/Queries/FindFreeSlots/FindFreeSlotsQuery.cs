using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Yard.Dtos;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Yard.Queries.FindFreeSlots;

public sealed record FindFreeSlotsQuery(
    Guid BlockId,
    ContainerSize Size, int PageIndex = 1, int PageSize = 20) : IQuery<PagedResult<YardPositionDto>>;
