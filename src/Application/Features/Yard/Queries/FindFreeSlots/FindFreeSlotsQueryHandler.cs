using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Abstractions.Rules;
using SnpDepotManagement.Application.Features.Yard.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Yard.Queries.FindFreeSlots;

public sealed class FindFreeSlotsQueryHandler(IAppDbContext context, IRuleEngine ruleEngine)
    : IQueryHandler<FindFreeSlotsQuery, PagedResult<YardPositionDto>>
{
    public async Task<Result<PagedResult<YardPositionDto>>> Handle(FindFreeSlotsQuery query, CancellationToken cancellationToken = default)
    {
        _ = ruleEngine; // Constructor retained for existing consumers; predicates below execute at the database.
        var block = await context.YardBlocks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == query.BlockId, cancellationToken);
        if (block is null)
            return Result.Failure<PagedResult<YardPositionDto>>(Error.NotFound("YardBlock.NotFound", "Yard block not found."));
        if (block.IsVirtual)
            return Result.Success(PagedResult<YardPositionDto>.Create([], 0, query.PageIndex, query.PageSize));

        var slots = context.YardSlots.AsNoTracking().Where(s => s.BlockId == query.BlockId);
        var candidates = slots.Where(s => !s.IsOccupied && (s.Position.Tier == 1 || slots.Any(below =>
                below.Position.Bay == s.Position.Bay && below.Position.Row == s.Position.Row &&
                below.Position.Tier == s.Position.Tier - 1 && below.IsOccupied)))
            .Select(s => new { Bay = s.Position.Bay, Row = s.Position.Row, Tier = s.Position.Tier });

        if (query.Size == ContainerSize.Ft40)
            candidates = from lower in slots
                         from upper in slots
                         where !lower.IsOccupied && !upper.IsOccupied &&
                             upper.Position.Bay == lower.Position.Bay + 2 &&
                             upper.Position.Row == lower.Position.Row && upper.Position.Tier == lower.Position.Tier &&
                             (lower.Position.Tier == 1 ||
                              (slots.Any(b => b.Position.Bay == lower.Position.Bay && b.Position.Row == lower.Position.Row &&
                                              b.Position.Tier == lower.Position.Tier - 1 && b.IsOccupied) &&
                               slots.Any(b => b.Position.Bay == upper.Position.Bay && b.Position.Row == upper.Position.Row &&
                                              b.Position.Tier == upper.Position.Tier - 1 && b.IsOccupied)))
                         select new { Bay = lower.Position.Bay + 1, Row = lower.Position.Row, Tier = lower.Position.Tier };

        var total = await candidates.CountAsync(cancellationToken);
        var positions = await candidates.OrderBy(p => p.Tier).ThenBy(p => p.Row).ThenBy(p => p.Bay)
            .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize)
            .Select(p => new YardPositionDto(p.Bay, p.Row, p.Tier)).ToListAsync(cancellationToken);
        return Result.Success(PagedResult<YardPositionDto>.Create(positions, total, query.PageIndex, query.PageSize));
    }
}
