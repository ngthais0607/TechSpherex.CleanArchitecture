using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Reports.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetContainerHistory;

public sealed record GetContainerHistoryQuery(string ContainerNumber, int PageIndex = 1, int PageSize = 20)
    : IQuery<PagedResult<ContainerHistoryEventDto>>;
