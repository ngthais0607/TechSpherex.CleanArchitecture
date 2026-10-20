using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Caching;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Reports.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IAppDbContext _context;
    private readonly ICacheService _cache;
    private readonly TimeProvider _timeProvider;

    public GetDashboardSummaryQueryHandler(
        IAppDbContext context,
        ICacheService cache,
        TimeProvider timeProvider)
    {
        _context = context;
        _cache = cache;
        _timeProvider = timeProvider;
    }

    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery query, CancellationToken cancellationToken = default)
    {
        const string cacheKey = "reports_summary";
        var cached = await _cache.GetAsync<DashboardSummaryDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        // In-yard containers and TEU
        var inYardContainers = await (
            from lc in _context.ContainerLifecycles
            join c in _context.Containers on lc.ContainerId equals c.Id
            where lc.Status == LifecycleStatus.InYard
            select new { c.Size }
        ).ToListAsync(cancellationToken);

        var totalContainersInYard = inYardContainers.Count;
        var totalTeuInYard = inYardContainers.Sum(c => c.Size == ContainerSize.Ft40 ? 2 : 1);

        // Active orders
        var activeOrdersCount = await _context.DeliveryOrders
            .CountAsync(o => !o.IsCompleted && o.ExpiryDate >= utcNow, cancellationToken);

        // Today's gate transactions (UTC+7)
        var vnNow = utcNow.AddHours(7);
        var vnToday = vnNow.Date;
        var todayStartUtc = vnToday.AddHours(-7);
        var todayEndUtc = todayStartUtc.AddDays(1);

        var todayGateTransactionsCount = await _context.GateTransactions
            .CountAsync(t => t.Status == GateTransactionStatus.Active
                          && t.CreatedAt >= todayStartUtc
                          && t.CreatedAt < todayEndUtc, cancellationToken);

        var summary = new DashboardSummaryDto(
            totalContainersInYard,
            totalTeuInYard,
            activeOrdersCount,
            todayGateTransactionsCount);

        await _cache.SetAsync(cacheKey, summary, TimeSpan.FromSeconds(30), cancellationToken);

        return Result.Success(summary);
    }
}
