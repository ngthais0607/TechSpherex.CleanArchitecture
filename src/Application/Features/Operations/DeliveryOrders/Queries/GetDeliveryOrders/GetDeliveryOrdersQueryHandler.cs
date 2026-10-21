using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Queries.GetDeliveryOrders;

public sealed class GetDeliveryOrdersQueryHandler : IQueryHandler<GetDeliveryOrdersQuery, PagedResult<DeliveryOrderDto>>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _clock;

    public GetDeliveryOrdersQueryHandler(IAppDbContext context, TimeProvider? clock = null)
    {
        _context = context;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<Result<PagedResult<DeliveryOrderDto>>> Handle(GetDeliveryOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var sort = query.Sort?.Trim().ToLowerInvariant();
        var status = query.Status?.Trim().ToLowerInvariant();
        if ((!string.IsNullOrEmpty(sort) && sort is not ("createdat" or "ordernumber" or "expirydate")) ||
            (!string.IsNullOrEmpty(status) && status is not ("active" or "expired" or "expiring" or "expiringsoon" or "completed" or "fullydelivered")))
            return Result.Failure<PagedResult<DeliveryOrderDto>>(Error.Validation("Validation.Failed", "Unsupported delivery order filter or sort."));
        var dbQuery = _context.DeliveryOrders
            .Include(o => o.Items)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            dbQuery = dbQuery.Where(order => order.OrderNumber.Contains(search) || order.CustomerName.ToUpper().Contains(search));
        }

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

        var now = _clock.GetUtcNow().UtcDateTime;
        var today = now.Date;
        var in3Days = today.AddDays(4);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var statusLower = query.Status.Trim().ToLowerInvariant();
            if (statusLower is "expiring" or "expiringsoon")
            {
                dbQuery = dbQuery.Where(o => !o.IsCompleted && o.ExpiryDate >= today && o.ExpiryDate < in3Days && o.Items.Any(i => i.DeliveredQuantity < i.RequiredQuantity));
            }
            else if (statusLower == "expired")
            {
                dbQuery = dbQuery.Where(o => o.ExpiryDate < today);
            }
            else if (statusLower == "active")
            {
                dbQuery = dbQuery.Where(o => !o.IsCompleted && o.ExpiryDate >= in3Days && o.Items.Any(i => i.DeliveredQuantity < i.RequiredQuantity));
            }
            else if (statusLower is "completed" or "fullydelivered")
            {
                dbQuery = dbQuery.Where(o => o.ExpiryDate >= today && (o.IsCompleted || o.Items.All(i => i.DeliveredQuantity >= i.RequiredQuantity)));
            }
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var ordered = (sort, query.Descending) switch
        {
            ("ordernumber", false) => dbQuery.OrderBy(order => order.OrderNumber),
            ("ordernumber", true) => dbQuery.OrderByDescending(order => order.OrderNumber),
            ("expirydate", false) => dbQuery.OrderBy(order => order.ExpiryDate),
            ("expirydate", true) => dbQuery.OrderByDescending(order => order.ExpiryDate),
            (_, false) => dbQuery.OrderBy(order => order.CreatedAt),
            _ => dbQuery.OrderByDescending(order => order.CreatedAt)
        };

        var orders = await ordered
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
                i.RemainingQuantity)).ToList(),
            o.GetDisplayStatus(now).ToString()
        )).ToList();

        var pagedResult = PagedResult<DeliveryOrderDto>.Create(dtos, totalCount, pageIndex, pageSize);

        return Result.Success(pagedResult);
    }
}
