using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Abstractions.Rules;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;
using SnpDepotManagement.Domain.ValueObjects;
using SnpDepotManagement.Application.Abstractions.Configuration;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;

public sealed class GateOutCommandHandler : ICommandHandler<GateOutCommand>
{
    private readonly IAppDbContext _context;
    private readonly IRuleEngine _ruleEngine;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser? _currentUser;
    private readonly IGateSettings? _gateSettings;

    public GateOutCommandHandler(
        IAppDbContext context,
        IRuleEngine ruleEngine,
        TimeProvider timeProvider,
        ICurrentUser? currentUser = null,
        IGateSettings? gateSettings = null)
    {
        _context = context;
        _ruleEngine = ruleEngine;
        _timeProvider = timeProvider;
        _currentUser = currentUser;
        _gateSettings = gateSettings;
    }

    public async Task<Result> Handle(GateOutCommand command, CancellationToken cancellationToken = default)
    {
        Container? container = null;

        if (!string.IsNullOrWhiteSpace(command.ContainerNumber))
        {
            var parsed = ContainerNumber.Create(command.ContainerNumber);
            if (parsed.IsFailure)
            {
                return Result.Failure(parsed.Error);
            }
            container = await _context.Containers
                .FirstOrDefaultAsync(c => c.ContainerNumber == parsed.Value, cancellationToken);
        }
        else if (command.ContainerId.HasValue && command.ContainerId.Value != Guid.Empty)
        {
            container = await _context.Containers
                .FirstOrDefaultAsync(c => c.Id == command.ContainerId.Value, cancellationToken);
        }

        if (container is null)
        {
            return Result.Failure(
                Error.NotFound("Container.NotFound", "Không tìm thấy container."));
        }

        var lifecycle = await _context.ContainerLifecycles
            .FirstOrDefaultAsync(l => l.ContainerId == container.Id && l.Status == LifecycleStatus.InYard, cancellationToken);

        if (lifecycle is null)
        {
            return Result.Failure(
                Error.Conflict("Lifecycle.NotInYard", "Container hiện không có trong bãi."));
        }

        DeliveryOrder? order = null;
        if (!string.IsNullOrWhiteSpace(command.OrderNumber))
        {
            var normOrderNo = command.OrderNumber.Trim().ToUpperInvariant();
            order = await _context.DeliveryOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderNumber == normOrderNo, cancellationToken);
        }
        else if (command.DeliveryOrderId.HasValue && command.DeliveryOrderId.Value != Guid.Empty)
        {
            order = await _context.DeliveryOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == command.DeliveryOrderId.Value, cancellationToken);
        }

        if (order is null)
        {
            return Result.Failure(
                Error.NotFound("DeliveryOrder.NotFound", "Không tìm thấy lệnh giao."));
        }

        // Tra lineOperator từ container.OwnerCode qua LineOperatorOwnerCode
        var ownerCodeRecord = await _context.LineOperatorOwnerCodes
            .FirstOrDefaultAsync(x => x.OwnerCode == container.OwnerCode, cancellationToken);

        string? resolvedOpCode = null;
        if (ownerCodeRecord is not null)
        {
            resolvedOpCode = await _context.LineOperators
                .Where(l => l.Id == ownerCodeRecord.LineOperatorId)
                .Select(l => l.Code)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            var directOp = await _context.LineOperators
                .FirstOrDefaultAsync(l => l.Code == container.OwnerCode, cancellationToken);
            if (directOp is not null)
            {
                resolvedOpCode = directOp.Code;
            }
            else if (string.Equals(container.OwnerCode, order.LineOperator, StringComparison.OrdinalIgnoreCase))
            {
                resolvedOpCode = container.OwnerCode;
            }
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        // Evaluate 4 Gate-Out Business Rules
        var rules = new IBusinessRule[]
        {
            new ContainerIsInYardRule(lifecycle),
            new ContainerBelongsToLineOperatorRule(resolvedOpCode, order.LineOperator),
            new DeliveryOrderNotExpiredRule(order.ExpiryDate, utcNow),
            new DeliveryOrderHasRemainingQuantityRule(order, container.Size, container.Type)
        };

        var ruleResult = _ruleEngine.Evaluate(rules);
        if (!ruleResult.IsSuccess)
        {
            return Result.Failure(ruleResult.Error!);
        }

        var activeBooking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.ContainerId == container.Id && b.Status == BookingStatus.Reserved, cancellationToken);

        var allowDirectShip = _gateSettings?.AllowDirectShip ?? true;

        if (activeBooking is not null)
        {
            if (activeBooking.OrderId != order.Id)
            {
                return Result.Failure(
                    Error.Conflict("GateOut.WrongOrderReservation", "Container đã được giữ chỗ cho lệnh giao khác."));
            }
        }
        else if (!allowDirectShip)
        {
            return Result.Failure(
                Error.Conflict("G5.DirectShipNotAllowed", "Container chưa được gán vào lệnh giao. Vui lòng cấp số lệnh trước khi cho xe vào."));
        }

        // Vị trí xuất
        MovementPosition? sourcePosition = null;
        Guid? sourceSlotId = null;

        if (lifecycle.VirtualBlockId.HasValue)
        {
            var vBlock = await _context.YardBlocks
                .FirstOrDefaultAsync(b => b.Id == lifecycle.VirtualBlockId.Value, cancellationToken);
            if (vBlock is not null)
            {
                sourcePosition = new MovementPosition(vBlock.Id, vBlock.Code, 0, 0, 0);
            }
        }
        else
        {
            var occupiedBlockIds = await _context.YardSlots.Where(s => s.ContainerId == container.Id)
                .Select(s => s.BlockId).Distinct().ToListAsync(cancellationToken);
            var occupiedBlocks = await _context.YardBlocks.Include(b => b.Slots)
                .Where(b => occupiedBlockIds.Contains(b.Id)).ToListAsync(cancellationToken);
            var occupiedSlots = occupiedBlocks.SelectMany(b => b.Slots).Where(s => s.ContainerId == container.Id).ToList();

            foreach (var occupied in occupiedSlots)
            {
                var hasContainerAbove = occupiedBlocks.Single(b => b.Id == occupied.BlockId).Slots.Any(s =>
                    s.Bay == occupied.Bay && s.Row == occupied.Row && s.Tier > occupied.Tier && s.IsOccupied);
                if (hasContainerAbove)
                    return Result.Failure(Error.Conflict("YardSlot.CannotRemoveWithContainersAbove", "Có container đè phía trên, cần đảo chuyển trước khi xuất."));
            }

            var source = occupiedSlots.OrderBy(s => s.Bay).FirstOrDefault();
            if (source is not null)
            {
                sourceSlotId = source.Id;
                var block = occupiedBlocks.Single(b => b.Id == source.BlockId);
                var bay = container.Size == ContainerSize.Ft40 ? source.Bay + 1 : source.Bay;
                sourcePosition = MovementHistory.Position(block, bay, source.Row, source.Tier);
            }

            foreach (var slot in occupiedSlots)
            {
                slot.Release();
            }
        }

        // Allocate quantity
        var targetItem = order.Items.First(i =>
            i.Size == container.Size &&
            i.Type == container.Type &&
            i.RemainingQuantity > 0);

        var allocateResult = targetItem.Allocate(1);
        if (allocateResult.IsFailure)
        {
            return allocateResult;
        }

        if (activeBooking is not null)
        {
            activeBooking.Ship(utcNow);
        }
        else
        {
            var tenantId = !string.IsNullOrEmpty(_currentUser?.TenantId) ? _currentUser.TenantId : order.TenantId;
            var directBooking = DeliveryOrderBooking.Reserve(order.Id, targetItem.Id, container.Id, tenantId, utcNow);
            directBooking.Ship(utcNow);
            _context.Bookings.Add(directBooking);
        }

        if (order.Items.All(i => i.RemainingQuantity == 0))
        {
            order.MarkCompleted();
        }

        // Gate out lifecycle
        var gateOutResult = lifecycle.GateOut(
            command.EffectiveVehicleNumber,
            order.Id,
            utcNow,
            command.VehicleType);
        if (gateOutResult.IsFailure)
        {
            return gateOutResult;
        }

        // Record GateTransaction
        var gateTx = GateTransaction.CreateGateOut(
            container.Id,
            order.Id,
            command.EffectiveVehicleNumber,
            command.VehicleType,
            command.DriverName,
            sourceSlotId,
            container.TenantId);
        _context.GateTransactions.Add(gateTx);

        MovementHistory.Append(_context, lifecycle, ContainerMovementType.GateOut, utcNow, _currentUser?.UserId,
            sourcePosition, null, command.EffectiveVehicleNumber, command.VehicleType, order.Id);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
