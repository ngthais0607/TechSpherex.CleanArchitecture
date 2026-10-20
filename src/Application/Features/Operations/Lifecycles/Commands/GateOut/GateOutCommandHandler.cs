using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Abstractions.Rules;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;

public sealed class GateOutCommandHandler : ICommandHandler<GateOutCommand>
{
    private readonly IAppDbContext _context;
    private readonly IRuleEngine _ruleEngine;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser? _currentUser;

    public GateOutCommandHandler(
        IAppDbContext context,
        IRuleEngine ruleEngine,
        TimeProvider timeProvider, ICurrentUser? currentUser = null)
    {
        _context = context;
        _ruleEngine = ruleEngine;
        _timeProvider = timeProvider;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(GateOutCommand command, CancellationToken cancellationToken = default)
    {
        var container = await _context.Containers
            .FirstOrDefaultAsync(c => c.Id == command.ContainerId, cancellationToken);

        if (container is null)
        {
            return Result.Failure(
                Error.NotFound("Container.NotFound", "Container not found."));
        }

        var lifecycle = await _context.ContainerLifecycles
            .FirstOrDefaultAsync(l => l.ContainerId == command.ContainerId && l.Status == LifecycleStatus.InYard, cancellationToken);

        if (lifecycle is null)
        {
            return Result.Failure(
                Error.Conflict("Lifecycle.NotInYard", "Container is not currently present in the yard."));
        }

        var order = await _context.DeliveryOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == command.DeliveryOrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure(
                Error.NotFound("DeliveryOrder.NotFound", "Delivery order not found."));
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        // Evaluate 4 Gate-Out Business Rules
        var rules = new IBusinessRule[]
        {
            new ContainerIsInYardRule(lifecycle),
            new ContainerBelongsToLineOperatorRule(container.OwnerCode, order.LineOperator),
            new DeliveryOrderNotExpiredRule(order.ExpiryDate, utcNow),
            new DeliveryOrderHasRemainingQuantityRule(order, container.Size, container.Type)
        };

        var ruleResult = _ruleEngine.Evaluate(rules);
        if (!ruleResult.IsSuccess)
        {
            return Result.Failure(ruleResult.Error!);
        }

        var occupiedBlockIds = await _context.YardSlots.Where(s => s.ContainerId == command.ContainerId)
            .Select(s => s.BlockId).Distinct().ToListAsync(cancellationToken);
        // Read the block concurrency token together with occupancy BEFORE checking support.
        var occupiedBlocks = await _context.YardBlocks.Include(b => b.Slots)
            .Where(b => occupiedBlockIds.Contains(b.Id)).ToListAsync(cancellationToken);
        var occupiedSlots = occupiedBlocks.SelectMany(b => b.Slots).Where(s => s.ContainerId == command.ContainerId).ToList();
        foreach (var occupied in occupiedSlots)
        {
            var hasContainerAbove = occupiedBlocks.Single(b => b.Id == occupied.BlockId).Slots.Any(s =>
                s.Bay == occupied.Bay && s.Row == occupied.Row && s.Tier > occupied.Tier && s.IsOccupied);
            if (hasContainerAbove)
                return Result.Failure(Error.Conflict("YardSlot.CannotRemoveWithContainersAbove", "Containers above must be relocated before gate-out."));
        }

        // Allocate quantity only after validating that the physical position can be released.
        var targetItem = order.Items.First(i =>
            i.Size == container.Size &&
            i.Type == container.Type &&
            i.RemainingQuantity > 0);

        var allocateResult = targetItem.Allocate(1);
        if (allocateResult.IsFailure)
        {
            return allocateResult;
        }

        if (order.Items.All(i => i.RemainingQuantity == 0))
        {
            order.MarkCompleted();
        }

        // Release yard slots occupied by this container
        MovementPosition? sourcePosition = null;
        var source = occupiedSlots.OrderBy(s => s.Bay).FirstOrDefault();
        if (source is not null)
        {
            var block = occupiedBlocks.Single(b => b.Id == source.BlockId);
            var bay = container.Size == ContainerSize.Ft40 ? source.Bay + 1 : source.Bay;
            sourcePosition = MovementHistory.Position(block, bay, source.Row, source.Tier);
        }
        foreach (var slot in occupiedSlots)
        {
            slot.Release();
        }

        // Gate out lifecycle
        var gateOutResult = lifecycle.GateOut(
            command.OutVehicleNumber,
            command.DeliveryOrderId,
            utcNow,
            command.VehicleType);
        if (gateOutResult.IsFailure)
        {
            return gateOutResult;
        }

        MovementHistory.Append(_context, lifecycle, ContainerMovementType.GateOut, utcNow, _currentUser?.UserId,
            sourcePosition, null, command.OutVehicleNumber, command.VehicleType, command.DeliveryOrderId);

        // Single transaction committing order fulfillment, slot release, and lifecycle update
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
