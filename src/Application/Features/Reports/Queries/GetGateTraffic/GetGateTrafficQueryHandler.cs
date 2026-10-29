using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Caching;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Reports.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetGateTraffic;

public sealed class GetGateTrafficQueryHandler : IQueryHandler<GetGateTrafficQuery, IReadOnlyList<DailyGateTrafficDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICacheService _cache;
    private readonly TimeProvider _timeProvider;

    public GetGateTrafficQueryHandler(
        IAppDbContext context,
        ICacheService cache,
        TimeProvider timeProvider)
    {
        _context = context;
        _cache = cache;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<DailyGateTrafficDto>>> Handle(GetGateTrafficQuery query, CancellationToken cancellationToken = default)
    {
        var days = Math.Clamp(query.Days, 1, 90);
        var cacheKey = $"reports_gate_traffic_{days}";
        var cached = await _cache.GetAsync<IReadOnlyList<DailyGateTrafficDto>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var vnNow = utcNow.AddHours(7);
        var vnToday = vnNow.Date;
        var startDateVn = vnToday.AddDays(-(days - 1));
        var startUtc = startDateVn.AddHours(-7);

        var grouped = await _context.GateTransactions
            .AsNoTracking()
            .Where(t => t.Status == GateTransactionStatus.Active && t.CreatedAt >= startUtc)
            .GroupBy(t => t.CreatedAt.AddHours(7).Date)
            .Select(g => new
            {
                Date = g.Key,
                GateIn = g.Count(t => t.Type == GateTransactionType.GateIn),
                GateOut = g.Count(t => t.Type == GateTransactionType.GateOut)
            })
            .ToDictionaryAsync(g => g.Date, cancellationToken);

        var result = new List<DailyGateTrafficDto>();
        for (var i = 0; i < days; i++)
        {
            var day = startDateVn.AddDays(i);
            var dateStr = day.ToString("yyyy-MM-dd");

            if (grouped.TryGetValue(day, out var stats))
            {
                var gateIn = stats.GateIn;
                var gateOut = stats.GateOut;
                result.Add(new DailyGateTrafficDto(dateStr, gateIn, gateOut, gateIn + gateOut));
            }
            else
            {
                result.Add(new DailyGateTrafficDto(dateStr, 0, 0, 0));
            }
        }

        await _cache.SetAsync(cacheKey, (IReadOnlyList<DailyGateTrafficDto>)result, TimeSpan.FromSeconds(30), cancellationToken);

        return Result.Success<IReadOnlyList<DailyGateTrafficDto>>(result);
    }
}
