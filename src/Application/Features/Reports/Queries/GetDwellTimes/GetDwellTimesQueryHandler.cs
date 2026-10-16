using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Common.Time;
using SnpDepotManagement.Application.Features.Reports.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetDwellTimes;

public sealed class GetDwellTimesQueryHandler(IAppDbContext db, TimeProvider clock) : IQueryHandler<GetDwellTimesQuery, PagedResult<DwellTimeDto>>
{
    public async Task<Result<PagedResult<DwellTimeDto>>> Handle(GetDwellTimesQuery query, CancellationToken cancellationToken = default)
    {
        var asOf = clock.GetUtcNow().UtcDateTime;
        var offset = DepotTime.UtcOffset;
        var rows = from lifecycle in db.ContainerLifecycles.AsNoTracking()
                   join container in db.Containers.AsNoTracking() on lifecycle.ContainerId equals container.Id
                   where query.IncludeGatedOut || lifecycle.Status == LifecycleStatus.InYard
                   select new { lifecycle.Id, lifecycle.ContainerId, container.ContainerNumber, lifecycle.LineOperator,
                       lifecycle.InDate, lifecycle.OutDate,
                       Days = (((lifecycle.OutDate ?? asOf) + offset).Date - (lifecycle.InDate + offset).Date).Days };
        if (!string.IsNullOrWhiteSpace(query.LineOperator)) rows = rows.Where(r => r.LineOperator == query.LineOperator.Trim().ToUpper());
        if (query.MinDays.HasValue) rows = rows.Where(r => r.Days >= query.MinDays);
        var count = await rows.CountAsync(cancellationToken);
        var page = await rows.OrderByDescending(r => r.Days).ThenBy(r => r.Id)
            .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize)
            .Select(r => new DwellTimeDto(r.Id, r.ContainerId, r.ContainerNumber.Value, r.LineOperator, r.InDate, r.OutDate, r.Days))
            .ToListAsync(cancellationToken);
        return Result.Success(PagedResult<DwellTimeDto>.Create(page, count, query.PageIndex, query.PageSize));
    }
}
