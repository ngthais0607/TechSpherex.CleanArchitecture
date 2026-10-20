using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Queries.GetDeliveryOrders;

public sealed record GetDeliveryOrdersQuery(
    string? LineOperator = null,
    string? CustomerTaxCode = null,
    bool? IsCompleted = null,
    string? Status = null,
    int PageIndex = 1,
    int PageSize = 10) : IQuery<PagedResult<DeliveryOrderDto>>;
