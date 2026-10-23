using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Caching;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Reports.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetBlockCapacity;

public sealed class GetBlockCapacityQueryHandler : IQueryHandler<GetBlockCapacityQuery, IReadOnlyList<BlockCapacityDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICacheService _cache;

    public GetBlockCapacityQueryHandler(
        IAppDbContext context,
        ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<IReadOnlyList<BlockCapacityDto>>> Handle(GetBlockCapacityQuery query, CancellationToken cancellationToken = default)
    {
        const string cacheKey = "reports_block_capacity";
        var cached = await _cache.GetAsync<IReadOnlyList<BlockCapacityDto>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var blocks = await _context.YardBlocks
            .AsNoTracking()
            .Where(b => !b.IsVirtual)
            .OrderBy(b => b.Code)
            .ToListAsync(cancellationToken);

        var blockIds = blocks.Select(b => b.Id).ToList();

        var slots = await _context.YardSlots
            .AsNoTracking()
            .Where(s => blockIds.Contains(s.BlockId))
            .Select(s => new { s.BlockId, s.IsOccupied, s.ContainerId })
            .ToListAsync(cancellationToken);

        var slotGroups = slots
            .GroupBy(s => s.BlockId)
            .ToDictionary(g => g.Key, g => new
            {
                Total = g.Count(),
                Occupied = g.Count(s => s.IsOccupied),
                ContainersCount = g.Where(s => s.ContainerId.HasValue).Select(s => s.ContainerId!.Value).Distinct().Count()
            });

        var result = blocks.Select(b =>
        {
            slotGroups.TryGetValue(b.Id, out var stats);
            var total = stats?.Total ?? 0;
            var occupied = stats?.Occupied ?? 0;
            var containersCount = stats?.ContainersCount ?? 0;
            var rate = total > 0 ? Math.Round((double)occupied * 100.0 / total, 1) : 0.0;

            return new BlockCapacityDto(
                b.Id,
                b.Code,
                b.Name,
                total,
                occupied,
                rate,
                containersCount);
        }).ToList();

        await _cache.SetAsync(cacheKey, (IReadOnlyList<BlockCapacityDto>)result, TimeSpan.FromSeconds(30), cancellationToken);

        return Result.Success<IReadOnlyList<BlockCapacityDto>>(result);
    }
}
