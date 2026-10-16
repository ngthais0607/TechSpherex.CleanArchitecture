using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Reports.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetExpiringOrders;

public sealed record GetExpiringOrdersQuery(int Days = 7, string? LineOperator = null,
    int PageIndex = 1, int PageSize = 20) : IQuery<PagedResult<ExpiringDeliveryOrderDto>>;
