using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Queries.GetDeliveryOrders;

public sealed class GetDeliveryOrdersQueryHandler : IQueryHandler<GetDeliveryOrdersQuery, PagedResult<DeliveryOrderDto>>
{
    private readonly IAppDbContext _context;

    public GetDeliveryOrdersQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<DeliveryOrderDto>>> Handle(GetDeliveryOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.DeliveryOrders
            .Include(o => o.Items)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.LineOperator))
        {
            var op = query.LineOperator.Trim().ToUpperInvariant();
            dbQuery = dbQuery.Where(o => o.LineOperator == op);
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerTaxCode))
        {
            var taxCode = query.CustomerTaxCode.Trim();
            dbQuery = dbQuery.Where(o => o.CustomerTaxCode == taxCode);
        }

        if (query.IsCompleted.HasValue)
        {
            dbQuery = dbQuery.Where(o => o.IsCompleted == query.IsCompleted.Value);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        var orders = await dbQuery
            .OrderByDescending(o => o.CreatedAt)
            .ThenBy(o => o.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = orders.Select(o => new DeliveryOrderDto(
            o.Id,
            o.OrderNumber,
            o.LineOperator,
            o.CustomerTaxCode,
            o.CustomerName,
            o.ExpiryDate,
            o.OutboundVessel,
            o.IsCompleted,
            o.Items.Select(i => new DeliveryOrderItemDto(
                i.Id,
                i.Size,
                i.Type,
                i.RequiredQuantity,
                i.DeliveredQuantity,
                i.RemainingQuantity)).ToList()
        )).ToList();

        var pagedResult = PagedResult<DeliveryOrderDto>.Create(dtos, totalCount, pageIndex, pageSize);

        return Result.Success(pagedResult);
    }
}
