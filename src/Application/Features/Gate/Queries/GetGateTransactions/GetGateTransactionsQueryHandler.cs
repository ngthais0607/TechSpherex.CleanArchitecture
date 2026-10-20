using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Gate.Queries.GetGateTransactions;

public sealed class GetGateTransactionsQueryHandler : IQueryHandler<GetGateTransactionsQuery, PagedResult<GateTransactionDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser? _currentUser;

    public GetGateTransactionsQueryHandler(IAppDbContext context, ICurrentUser? currentUser = null)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<GateTransactionDto>>> Handle(GetGateTransactionsQuery query, CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUser?.TenantId ?? string.Empty;

        var txQuery = _context.GateTransactions
            .AsNoTracking()
            .Include(t => t.Changes)
            .Where(t => string.IsNullOrEmpty(tenantId) || t.TenantId == tenantId);

        if (query.Type.HasValue)
        {
            txQuery = txQuery.Where(t => t.Type == query.Type.Value);
        }

        if (query.Status.HasValue)
        {
            txQuery = txQuery.Where(t => t.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            txQuery = txQuery.Where(t => t.VehicleNumber.Contains(search)
                || (!string.IsNullOrEmpty(t.DriverName) && t.DriverName.Contains(search)));
        }

        var totalCount = await txQuery.CountAsync(cancellationToken);

        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var transactions = await txQuery
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var containerIds = transactions.Select(t => t.ContainerId).Distinct().ToList();
        var containers = await _context.Containers
            .AsNoTracking()
            .Where(c => containerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var orderIds = transactions.Where(t => t.OrderId.HasValue).Select(t => t.OrderId!.Value).Distinct().ToList();
        var orders = await _context.DeliveryOrders
            .AsNoTracking()
            .Where(o => orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        var slotIds = transactions.Where(t => t.SlotId.HasValue).Select(t => t.SlotId!.Value).Distinct().ToList();
        var slots = await _context.YardSlots
            .AsNoTracking()
            .Where(s => slotIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var blockIds = slots.Values.Select(s => s.BlockId).Distinct().ToList();
        var blocks = await _context.YardBlocks
            .AsNoTracking()
            .Where(b => blockIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        var dtos = transactions.Select(t =>
        {
            containers.TryGetValue(t.ContainerId, out var container);
            orders.TryGetValue(t.OrderId ?? Guid.Empty, out var order);

            string? position = null;
            if (t.SlotId.HasValue && slots.TryGetValue(t.SlotId.Value, out var slot) && blocks.TryGetValue(slot.BlockId, out var block))
            {
                var logicalBay = container?.Size == Domain.Enums.ContainerSize.Ft40 ? slot.Bay + 1 : slot.Bay;
                position = $"{block.Code}-{logicalBay:D2}-{slot.Row:D2}-{slot.Tier:D2}";
            }
            else if (!string.IsNullOrEmpty(t.VirtualBlockCode))
            {
                position = t.VirtualBlockCode;
            }

            var changes = t.Changes
                .OrderBy(c => c.ChangedAt)
                .Select(c => new GateTransactionChangeDto(
                    c.Id,
                    c.FieldName,
                    c.OldValue,
                    c.NewValue,
                    c.ChangedAt,
                    c.ChangedBy))
                .ToList();

            return new GateTransactionDto(
                t.Id,
                t.CreatedAt,
                t.Type.ToString(),
                t.Status.ToString(),
                container?.ContainerNumber.Value ?? "UNKNOWN",
                container?.Size.ToString(),
                container?.Type.ToString(),
                t.VehicleNumber,
                t.VehicleType.ToString(),
                t.DriverName,
                position,
                order?.OrderNumber,
                t.CreatedBy,
                t.CancelledAt,
                t.CancelledBy,
                changes);
        }).ToList();

        return Result.Success(PagedResult<GateTransactionDto>.Create(dtos, totalCount, pageIndex, pageSize));
    }
}
