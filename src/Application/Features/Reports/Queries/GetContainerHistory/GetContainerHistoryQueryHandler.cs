using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Reports.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetContainerHistory;

public sealed class GetContainerHistoryQueryHandler(IAppDbContext db)
    : IQueryHandler<GetContainerHistoryQuery, PagedResult<ContainerHistoryEventDto>>
{
    public async Task<Result<PagedResult<ContainerHistoryEventDto>>> Handle(GetContainerHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var parsed = ContainerNumber.Create(query.ContainerNumber);
        if (parsed.IsFailure) return Result.Failure<PagedResult<ContainerHistoryEventDto>>(parsed.Error);
        var containerId = await db.Containers.AsNoTracking().Where(c => c.ContainerNumber == parsed.Value)
            .Select(c => (Guid?)c.Id).SingleOrDefaultAsync(cancellationToken);
        if (containerId is null)
            return Result.Failure<PagedResult<ContainerHistoryEventDto>>(Error.NotFound("Container.NotFound", "Container not found."));
        var movements = db.ContainerMovements.AsNoTracking().Where(m => m.ContainerId == containerId).Select(m => new HistoryRow
        {
            Id = m.Id, Source = "Movement", LifecycleId = m.ContainerLifecycleId, OccurredAtUtc = m.OccurredAtUtc,
            EventType = m.EventType == ContainerMovementType.GateIn ? "GateIn" :
                m.EventType == ContainerMovementType.Place ? "Place" :
                m.EventType == ContainerMovementType.Relocate ? "Relocate" :
                m.EventType == ContainerMovementType.Remove ? "Remove" : "GateOut",
            ActorUserId = m.ActorUserId, VehicleNumber = m.VehicleNumber, VehicleType = m.VehicleType, DeliveryOrderId = m.DeliveryOrderId,
            FromBlockId = m.FromPosition == null ? null : (Guid?)m.FromPosition.BlockId,
            FromBlockCode = m.FromPosition == null ? null : (string?)m.FromPosition.BlockCode,
            FromBay = m.FromPosition == null ? null : (int?)m.FromPosition.Bay,
            FromRow = m.FromPosition == null ? null : (int?)m.FromPosition.Row,
            FromTier = m.FromPosition == null ? null : (int?)m.FromPosition.Tier,
            ToBlockId = m.ToPosition == null ? null : (Guid?)m.ToPosition.BlockId,
            ToBlockCode = m.ToPosition == null ? null : (string?)m.ToPosition.BlockCode,
            ToBay = m.ToPosition == null ? null : (int?)m.ToPosition.Bay,
            ToRow = m.ToPosition == null ? null : (int?)m.ToPosition.Row,
            ToTier = m.ToPosition == null ? null : (int?)m.ToPosition.Tier,
        });
        var legacy = db.ContainerLifecycles.AsNoTracking().Where(l => l.ContainerId == containerId && l.MovementHistoryVersion == 0);
        // An open legacy visit can depart after rollout. Its recorded departure wins over
        // the lifecycle fallback, while the genuine pre-rollout intake remains visible.
        var legacyIntakes = legacy.Where(l => !db.ContainerMovements.Any(m =>
            m.ContainerLifecycleId == l.Id && m.EventType == ContainerMovementType.GateIn));
        var legacyDepartures = legacy.Where(l => !db.ContainerMovements.Any(m =>
            m.ContainerLifecycleId == l.Id && m.EventType == ContainerMovementType.GateOut));
        var rows = movements.Concat(LegacyRows(legacyIntakes, false)).Concat(LegacyRows(legacyDepartures, true));
        var total = await rows.CountAsync(cancellationToken);
        var page = await rows.OrderBy(r => r.OccurredAtUtc).ThenBy(r => r.Source).ThenBy(r => r.Id).ThenBy(r => r.EventType)
            .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        // Formatting and snapshot construction happen only after the SQL UNION/order/page.
        var items = page.Select(r => new ContainerHistoryEventDto(
            $"{r.Source}:{r.Id:N}:{r.EventType}", r.Source, r.EventType, r.LifecycleId, r.OccurredAtUtc,
            r.FromBlockId.HasValue ? new MovementPosition(r.FromBlockId.Value, r.FromBlockCode!, r.FromBay!.Value, r.FromRow!.Value, r.FromTier!.Value) : null,
            r.ToBlockId.HasValue ? new MovementPosition(r.ToBlockId.Value, r.ToBlockCode!, r.ToBay!.Value, r.ToRow!.Value, r.ToTier!.Value) : null,
            r.ActorUserId, r.VehicleNumber, r.VehicleType, r.DeliveryOrderId)).ToArray();
        return Result.Success(PagedResult<ContainerHistoryEventDto>.Create(items, total, query.PageIndex, query.PageSize));
    }

    private static IQueryable<HistoryRow> LegacyRows(IQueryable<ContainerLifecycle> lifecycles, bool gateOut) =>
        lifecycles.Where(l => !gateOut || l.OutDate != null).Select(l => new HistoryRow
        {
            Id = l.Id, Source = "LegacyLifecycle", LifecycleId = l.Id,
            OccurredAtUtc = gateOut ? l.OutDate!.Value : l.InDate, EventType = gateOut ? "GateOut" : "GateIn",
            ActorUserId = null, VehicleNumber = gateOut ? l.OutVehicleNumber : l.InVehicleNumber,
            VehicleType = gateOut ? l.OutVehicleType : (VehicleType?)l.InVehicleType, DeliveryOrderId = gateOut ? l.DeliveryOrderId : null,
            FromBlockId = null,
            FromBlockCode = null,
            FromBay = null,
            FromRow = null,
            FromTier = null,
            ToBlockId = null,
            ToBlockCode = null,
            ToBay = null,
            ToRow = null,
            ToTier = null,
        });

    private sealed class HistoryRow
    {
        public Guid Id { get; init; }
        public string Source { get; init; } = "";
        public string EventType { get; init; } = "";
        public Guid LifecycleId { get; init; }
        public DateTime OccurredAtUtc { get; init; }
        public Guid? ActorUserId { get; init; }
        public string? VehicleNumber { get; init; }
        public VehicleType? VehicleType { get; init; }
        public Guid? DeliveryOrderId { get; init; }
        public Guid? FromBlockId { get; init; }
        public string? FromBlockCode { get; init; }
        public int? FromBay { get; init; }
        public int? FromRow { get; init; }
        public int? FromTier { get; init; }
        public Guid? ToBlockId { get; init; }
        public string? ToBlockCode { get; init; }
        public int? ToBay { get; init; }
        public int? ToRow { get; init; }
        public int? ToTier { get; init; }
    }
}
