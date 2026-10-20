using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Yard.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Yard.Queries.GetBlocks;

public sealed class GetBlocksQueryHandler : IQueryHandler<GetBlocksQuery, IReadOnlyList<YardBlockDto>>
{
    private readonly IAppDbContext _context;

    public GetBlocksQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<YardBlockDto>>> Handle(GetBlocksQuery query, CancellationToken cancellationToken = default)
    {
        var blocks = await _context.YardBlocks
            .AsNoTracking()
            .OrderBy(b => b.IsVirtual)
            .ThenBy(b => b.Code)
            .ToListAsync(cancellationToken);

        var blockIds = blocks.Select(b => b.Id).ToList();

        var slotCounts = await _context.YardSlots
            .AsNoTracking()
            .Where(s => blockIds.Contains(s.BlockId))
            .GroupBy(s => s.BlockId)
            .Select(g => new
            {
                BlockId = g.Key,
                Total = g.Count(),
                Occupied = g.Count(s => s.IsOccupied)
            })
            .ToDictionaryAsync(x => x.BlockId, cancellationToken);

        var dtos = blocks.Select(b =>
        {
            slotCounts.TryGetValue(b.Id, out var count);
            var total = count?.Total ?? 0;
            var occupied = count?.Occupied ?? 0;
            var oddBays = (b.MaxBays + 1) / 2;

            return new YardBlockDto(
                b.Id,
                b.Code,
                b.Name,
                b.IsVirtual,
                b.BlockType,
                b.MaxBays,
                oddBays,
                b.MaxRows,
                b.MaxTiers,
                total,
                occupied);
        }).ToList();

        return Result.Success<IReadOnlyList<YardBlockDto>>(dtos);
    }
}
