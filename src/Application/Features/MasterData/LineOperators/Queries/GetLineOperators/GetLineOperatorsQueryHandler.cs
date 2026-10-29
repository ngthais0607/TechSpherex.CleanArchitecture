using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Queries.GetLineOperators;

public sealed class GetLineOperatorsQueryHandler : IQueryHandler<GetLineOperatorsQuery, PagedResult<LineOperatorDto>>
{
    private readonly IAppDbContext _context;

    public GetLineOperatorsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<LineOperatorDto>>> Handle(GetLineOperatorsQuery query, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var source = _context.LineOperators.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            source = source.Where(l => l.Code.Contains(search) || l.Name.ToUpper().Contains(search));
        }
        var total = await source.CountAsync(cancellationToken);
        var operators = await source
            .OrderBy(l => l.Code)
            .ThenBy(l => l.Id)
            .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var codes = operators.Select(l => l.Code).Distinct().ToList();

        var containersInYard = await _context.ContainerLifecycles
            .AsNoTracking()
            .Where(lc => lc.Status == LifecycleStatus.InYard && codes.Contains(lc.LineOperator))
            .GroupBy(lc => lc.LineOperator)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Code, x => x.Count, cancellationToken);

        var orderCounts = await _context.DeliveryOrders
            .AsNoTracking()
            .Where(o => codes.Contains(o.LineOperator))
            .GroupBy(o => o.LineOperator)
            .Select(g => new
            {
                Code = g.Key,
                Total = g.Count(),
                Active = g.Count(o => !o.IsCompleted && o.ExpiryDate >= utcNow)
            })
            .ToDictionaryAsync(g => g.Code, cancellationToken);

        var list = operators.Select(l =>
        {
            var inYard = containersInYard.GetValueOrDefault(l.Code, 0);
            orderCounts.TryGetValue(l.Code, out var stats);
            var totalOrders = stats?.Total ?? 0;
            var activeOrders = stats?.Active ?? 0;
            return new LineOperatorDto(l.Id, l.Code, l.Name, l.IsActive, inYard, totalOrders, activeOrders);
        }).ToList();

        return Result.Success(PagedResult<LineOperatorDto>.Create(list, total, query.PageIndex, query.PageSize));
    }
}
