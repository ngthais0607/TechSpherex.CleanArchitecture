using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Reports.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetYardInventory;

public sealed class GetYardInventoryQueryHandler(IAppDbContext db) : IQueryHandler<GetYardInventoryQuery, InventoryReportDto>
{
    public async Task<Result<InventoryReportDto>> Handle(GetYardInventoryQuery query, CancellationToken cancellationToken = default)
    {
        // Physical occupancy also handles stale CurrentSlotId links in pre-rollout lifecycles.
        var positions = db.YardSlots.AsNoTracking().Where(s => s.IsOccupied && s.ContainerId != null)
            .Select(s => new { s.ContainerId, s.BlockId }).Distinct();
        var rows = from lifecycle in db.ContainerLifecycles.AsNoTracking()
                   join container in db.Containers.AsNoTracking() on lifecycle.ContainerId equals container.Id
                   join position in positions on (Guid?)container.Id equals position.ContainerId into location
                   from position in location.DefaultIfEmpty()
                   join block in db.YardBlocks.AsNoTracking() on position.BlockId equals block.Id into blocks
                   from block in blocks.DefaultIfEmpty()
                   where lifecycle.Status == LifecycleStatus.InYard && lifecycle.OutDate == null
                   select new { lifecycle.LineOperator, container.Size, container.Type, lifecycle.Condition,
                       BlockId = (Guid?)block.Id, BlockCode = (string?)block.Code };
        if (!string.IsNullOrWhiteSpace(query.LineOperator)) rows = rows.Where(r => r.LineOperator == query.LineOperator.Trim().ToUpper());
        if (query.Size.HasValue) rows = rows.Where(r => r.Size == query.Size);
        if (query.Type.HasValue) rows = rows.Where(r => r.Type == query.Type);
        if (query.Condition.HasValue) rows = rows.Where(r => r.Condition == query.Condition);
        if (query.BlockId.HasValue) rows = rows.Where(r => r.BlockId == query.BlockId);
        var totalContainers = await rows.CountAsync(cancellationToken);
        var groups = rows.GroupBy(r => new { r.LineOperator, r.Size, r.Type, r.Condition, r.BlockId, r.BlockCode })
            .Select(g => new { g.Key.LineOperator, g.Key.Size, g.Key.Type, g.Key.Condition, g.Key.BlockId, g.Key.BlockCode, Count = g.Count() });
        var totalGroups = await groups.CountAsync(cancellationToken);
        var page = await groups.OrderBy(g => g.LineOperator).ThenBy(g => g.Size).ThenBy(g => g.Type)
            .ThenBy(g => g.Condition).ThenBy(g => g.BlockId).Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize)
            .Select(g => new InventoryGroupDto(g.LineOperator, g.Size, g.Type, g.Condition, g.BlockId, g.BlockCode, g.Count))
            .ToListAsync(cancellationToken);
        return Result.Success(new InventoryReportDto(PagedResult<InventoryGroupDto>.Create(page, totalGroups, query.PageIndex, query.PageSize), totalContainers));
    }
}
