using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Configuration;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Gate.Evaluator;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.Features.Gate.Queries.ValidateGate;

public sealed class ValidateGateQueryHandler : IQueryHandler<ValidateGateQuery, ValidateGateResponseDto>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly IGateSettings _gateSettings;
    private readonly GateRuleEvaluator _evaluator = new();

    public ValidateGateQueryHandler(IAppDbContext context, TimeProvider timeProvider, IGateSettings gateSettings)
    {
        _context = context;
        _timeProvider = timeProvider;
        _gateSettings = gateSettings;
    }

    public async Task<Result<ValidateGateResponseDto>> Handle(ValidateGateQuery query, CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var rules = new List<(string code, string label, Func<RuleState> check, string? detail)>();

        // 1. Check Container Number format (C1)
        var numberResult = ContainerNumber.Create(query.ContainerNumber);
        rules.Add((
            "C1.ValidFormat",
            "Định dạng số container (ISO 6346)",
            () => numberResult.IsSuccess ? RuleState.Pass : RuleState.Fail,
            numberResult.IsFailure ? numberResult.Error.Description : null
        ));

        Container? container = null;
        ContainerLifecycle? lifecycle = null;
        DeliveryOrderBooking? activeBooking = null;
        DeliveryOrder? bookingOrder = null;
        string? currentLocation = null;

        if (numberResult.IsSuccess)
        {
            container = await _context.Containers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ContainerNumber == numberResult.Value, cancellationToken);

            if (container != null)
            {
                lifecycle = await _context.ContainerLifecycles
                    .AsNoTracking()
                    .Where(l => l.ContainerId == container.Id)
                    .OrderByDescending(l => l.InDate)
                    .FirstOrDefaultAsync(cancellationToken);

                activeBooking = await _context.Bookings
                    .AsNoTracking()
                    .Where(b => b.ContainerId == container.Id && b.Status == BookingStatus.Reserved)
                    .FirstOrDefaultAsync(cancellationToken);

                if (activeBooking != null)
                {
                    bookingOrder = await _context.DeliveryOrders
                        .AsNoTracking()
                        .FirstOrDefaultAsync(o => o.Id == activeBooking.OrderId, cancellationToken);
                }

                if (lifecycle != null && lifecycle.IsInDepot)
                {
                    if (lifecycle.CurrentSlotId.HasValue)
                    {
                        var slot = await _context.YardSlots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == lifecycle.CurrentSlotId.Value, cancellationToken);
                        if (slot != null)
                        {
                            var block = await _context.YardBlocks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == slot.BlockId, cancellationToken);
                            currentLocation = $"{block?.Code ?? "?"}-{slot.Bay:D2}-{slot.Row:D2}-{slot.Tier:D2}";
                        }
                    }
                    else if (lifecycle.VirtualBlockId.HasValue)
                    {
                        var vBlock = await _context.YardBlocks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == lifecycle.VirtualBlockId.Value, cancellationToken);
                        currentLocation = $"Ảo: {vBlock?.Code ?? "?"}";
                    }
                }
            }
        }

        // 2. Evaluate according to transaction type
        if (query.Type == GateTransactionType.GateIn)
        {
            // G1: Container does not already exist in yard
            rules.Add((
                "G1.NotInYard",
                "Container chưa có trong bãi",
                () =>
                {
                    if (container == null) return RuleState.Pass; // new container to be registered
                    if (lifecycle != null && lifecycle.IsInDepot) return RuleState.Fail;
                    return RuleState.Pass;
                },
                lifecycle?.IsInDepot == true ? "Container hiện đang có trong bãi, không thể nhập bãi tiếp." : null
            ));

            // Position checks
            if (!string.IsNullOrWhiteSpace(query.VirtualBlockCode))
            {
                var normVBlock = query.VirtualBlockCode.Trim().ToUpperInvariant();
                var vBlockExists = await _context.YardBlocks.AnyAsync(b => b.Code == normVBlock && b.IsVirtual, cancellationToken);
                rules.Add((
                    "Y0.VirtualBlockExists",
                    "Block ảo tồn tại",
                    () => vBlockExists ? RuleState.Pass : RuleState.Fail,
                    vBlockExists ? null : $"Không tìm thấy block ảo '{query.VirtualBlockCode}'."
                ));
            }
            else if (!string.IsNullOrWhiteSpace(query.BlockCode) && query.Bay.HasValue && query.Row.HasValue && query.Tier.HasValue)
            {
                var normBlock = query.BlockCode.Trim().ToUpperInvariant();
                var block = await _context.YardBlocks.AsNoTracking().FirstOrDefaultAsync(b => b.Code == normBlock && !b.IsVirtual, cancellationToken);
                var slot = block == null ? null : await _context.YardSlots.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.BlockId == block.Id && s.Position.Bay == query.Bay.Value && s.Position.Row == query.Row.Value && s.Position.Tier == query.Tier.Value, cancellationToken);

                rules.Add((
                    "Y1.SlotExists",
                    "Vị trí bãi tồn tại",
                    () => slot != null ? RuleState.Pass : RuleState.Fail,
                    slot == null ? $"Không tìm thấy vị trí {query.BlockCode}-{query.Bay.Value:D2}-{query.Row.Value:D2}-{query.Tier.Value:D2}." : null
                ));

                rules.Add((
                    "Y2.SlotAvailable",
                    "Vị trí bãi còn trống",
                    () => (slot != null && !slot.IsOccupied) ? RuleState.Pass : RuleState.Fail,
                    (slot != null && slot.IsOccupied) ? "Vị trí này đã có container chiếm chỗ." : null
                ));
            }
        }
        else // GateOut
        {
            // G2: Container exists and is in yard
            rules.Add((
                "G2.ContainerInYard",
                "Container đang có trong bãi",
                () => (container != null && lifecycle != null && lifecycle.IsInDepot) ? RuleState.Pass : RuleState.Fail,
                (container == null || lifecycle == null || !lifecycle.IsInDepot) ? "Container không có trong bãi." : null
            ));

            // Y5: Container not buried (no container on top)
            if (lifecycle != null && lifecycle.CurrentSlotId.HasValue)
            {
                var currentSlot = await _context.YardSlots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == lifecycle.CurrentSlotId.Value, cancellationToken);
                if (currentSlot != null)
                {
                    var isBuried = await _context.YardSlots.AnyAsync(s =>
                        s.BlockId == currentSlot.BlockId &&
                        s.Position.Row == currentSlot.Position.Row &&
                        (s.Position.Bay == currentSlot.Position.Bay || s.Position.Bay == currentSlot.Position.Bay - 1 || s.Position.Bay == currentSlot.Position.Bay + 1) &&
                        s.Position.Tier > currentSlot.Position.Tier &&
                        s.IsOccupied, cancellationToken);

                    rules.Add((
                        "Y5.NotBuried",
                        "Container không bị đè",
                        () => isBuried ? RuleState.Fail : RuleState.Pass,
                        isBuried ? "Container đang bị đè bởi container khác ở tầng trên." : null
                    ));
                }
            }

            // G3: Delivery Order validation
            DeliveryOrder? order = null;
            if (!string.IsNullOrWhiteSpace(query.OrderNumber))
            {
                var normOrder = query.OrderNumber.Trim().ToUpperInvariant();
                order = await _context.DeliveryOrders
                    .Include(o => o.Items)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrderNumber == normOrder, cancellationToken);

                rules.Add((
                    "D1.OrderExists",
                    "Lệnh giao tồn tại",
                    () => order != null ? RuleState.Pass : RuleState.Fail,
                    order == null ? $"Không tìm thấy lệnh giao '{query.OrderNumber}'." : null
                ));

                if (order != null)
                {
                    var isExpired = utcNow > order.ExpiryDate;
                    rules.Add((
                        "D2.OrderNotExpired",
                        "Lệnh giao còn hạn",
                        () => isExpired ? RuleState.Fail : RuleState.Pass,
                        isExpired ? $"Lệnh giao đã hết hạn vào ngày {order.ExpiryDate:dd/MM/yyyy}." : null
                    ));

                    // D3: Line operator compatibility
                    if (container != null)
                    {
                        var lineOpMatches = string.Equals(order.LineOperator, container.OwnerCode, StringComparison.OrdinalIgnoreCase);
                        if (!lineOpMatches)
                        {
                            var lineOp = await _context.LineOperators.AsNoTracking().FirstOrDefaultAsync(l => l.Code == order.LineOperator, cancellationToken);
                            if (lineOp != null)
                            {
                                lineOpMatches = await _context.LineOperatorOwnerCodes.AnyAsync(m => m.LineOperatorId == lineOp.Id && m.OwnerCode == container.OwnerCode, cancellationToken);
                            }
                        }

                        rules.Add((
                            "D3.LineOperatorMatch",
                            "Hãng khai thác container khớp với lệnh",
                            () => lineOpMatches ? RuleState.Pass : RuleState.Fail,
                            lineOpMatches ? null : $"Container thuộc hãng '{container.OwnerCode}' không khớp hãng '{order.LineOperator}' của lệnh."
                        ));

                        // Remaining quantity check
                        var matchingItem = order.Items.FirstOrDefault(i => i.Size == container.Size && i.Type == container.Type);
                        var hasRemaining = matchingItem != null && matchingItem.RemainingQuantity > 0;
                        rules.Add((
                            "D4.RemainingQuantity",
                            "Lệnh còn số lượng xuất",
                            () => hasRemaining ? RuleState.Pass : RuleState.Fail,
                            matchingItem == null ? $"Lệnh không có dòng hàng cho container cỡ {container.Size} loại {container.Type}." :
                            !hasRemaining ? "Dòng hàng của lệnh đã xuất hết số lượng." : null
                        ));

                        // G4: Booking check
                        if (activeBooking != null)
                        {
                            var bookingMatchesOrder = activeBooking.OrderId == order.Id;
                            rules.Add((
                                "G4.BookingMatchesOrder",
                                "Container giữ chỗ đúng cho lệnh này",
                                () => bookingMatchesOrder ? RuleState.Pass : RuleState.Fail,
                                bookingMatchesOrder ? null : $"Container đang được giữ chỗ cho lệnh khác ({bookingOrder?.OrderNumber ?? "?"})."
                            ));
                        }
                        else
                        {
                            // G5: Direct ship without reservation
                            rules.Add((
                                "G5.DirectShip",
                                "Xuất thẳng không qua giữ chỗ trước",
                                () => _gateSettings.AllowDirectShip ? RuleState.Pass : RuleState.Fail,
                                _gateSettings.AllowDirectShip ? null : "Hệ thống không cho phép xuất thẳng khi chưa giữ chỗ trước."
                            ));
                        }
                    }
                }
            }
        }

        var checkResults = _evaluator.Evaluate(rules);

        // Build Container info DTO
        ContainerGateInfoDto? containerInfo = null;
        if (container != null)
        {
            var displayStatus = activeBooking != null ? "Reserved" :
                (lifecycle != null && lifecycle.IsInDepot) ? "InYard" :
                lifecycle != null ? "OutOfYard" : "NeverIn";

            containerInfo = new ContainerGateInfoDto(
                container.Id,
                container.ContainerNumber.Value,
                container.IsoCode,
                container.Size.ToString(),
                container.Type.ToString(),
                container.OwnerCode,
                displayStatus,
                currentLocation,
                bookingOrder?.OrderNumber
            );
        }

        // Build Order info DTO
        OrderGateInfoDto? orderInfo = null;
        if (!string.IsNullOrWhiteSpace(query.OrderNumber))
        {
            var normOrder = query.OrderNumber.Trim().ToUpperInvariant();
            var ord = await _context.DeliveryOrders.Include(o => o.Items).AsNoTracking().FirstOrDefaultAsync(o => o.OrderNumber == normOrder, cancellationToken);
            if (ord != null)
            {
                var itemIds = ord.Items.Select(i => i.Id).ToList();
                var reservedCounts = await _context.Bookings
                    .Where(b => itemIds.Contains(b.ItemId) && b.Status == BookingStatus.Reserved)
                    .GroupBy(b => b.ItemId)
                    .Select(g => new { ItemId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ItemId, x => x.Count, cancellationToken);

                var items = ord.Items.Select(i =>
                {
                    reservedCounts.TryGetValue(i.Id, out var reservedCount);
                    return new OrderItemGateDto(
                        i.Id,
                        i.Size.ToString(),
                        i.Type.ToString(),
                        i.RequiredQuantity,
                        reservedCount,
                        i.DeliveredQuantity,
                        i.RemainingQuantity
                    );
                }).ToList();

                orderInfo = new OrderGateInfoDto(
                    ord.Id,
                    ord.OrderNumber,
                    ord.LineOperator,
                    ord.CustomerName,
                    ord.ExpiryDate,
                    items
                );
            }
        }

        return Result.Success(new ValidateGateResponseDto(checkResults, containerInfo, orderInfo));
    }
}
