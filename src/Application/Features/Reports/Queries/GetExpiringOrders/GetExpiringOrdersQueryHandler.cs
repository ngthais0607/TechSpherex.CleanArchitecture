using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Common.Time;
using SnpDepotManagement.Application.Features.Reports.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetExpiringOrders;

public sealed class GetExpiringOrdersQueryHandler(IAppDbContext db, TimeProvider clock)
    : IQueryHandler<GetExpiringOrdersQuery, PagedResult<ExpiringDeliveryOrderDto>>
{
    public async Task<Result<PagedResult<ExpiringDeliveryOrderDto>>> Handle(GetExpiringOrdersQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Days is < 0 or > 365)
            return Result.Failure<PagedResult<ExpiringDeliveryOrderDto>>(Error.Validation("Validation.Failed", "Days must be between 0 and 365."));
        var asOf = clock.GetUtcNow().UtcDateTime;
        var today = DepotTime.GetBusinessDate(asOf);
        var from = DepotTime.StartOfBusinessDateUtc(today);
        var until = DepotTime.StartOfBusinessDateUtc(today.AddDays(query.Days + 1));
        var offset = DepotTime.UtcOffset;
        var orders = db.DeliveryOrders.AsNoTracking().Where(o => o.ExpiryDate >= from && o.ExpiryDate < until &&
            o.Items.Any(i => i.RequiredQuantity > i.DeliveredQuantity));
        if (!string.IsNullOrWhiteSpace(query.LineOperator)) orders = orders.Where(o => o.LineOperator == query.LineOperator.Trim().ToUpper());
        var count = await orders.CountAsync(cancellationToken);
        var page = await orders.OrderBy(o => o.ExpiryDate).ThenBy(o => o.Id)
            .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize)
            .Select(o => new ExpiringDeliveryOrderDto(o.Id, o.OrderNumber, o.LineOperator, o.CustomerName, o.ExpiryDate,
                ((o.ExpiryDate + offset).Date - (asOf + offset).Date).Days,
                o.Items.Sum(i => i.RequiredQuantity - i.DeliveredQuantity))).ToListAsync(cancellationToken);
        return Result.Success(PagedResult<ExpiringDeliveryOrderDto>.Create(page, count, query.PageIndex, query.PageSize));
    }
}
