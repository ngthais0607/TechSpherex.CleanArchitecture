using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Abstractions.Rules;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;

public sealed class GateOutCommandHandler : ICommandHandler<GateOutCommand>
{
    private readonly IAppDbContext _context;
    private readonly IRuleEngine _ruleEngine;
    private readonly TimeProvider _timeProvider;

    public GateOutCommandHandler(
        IAppDbContext context,
        IRuleEngine ruleEngine,
        TimeProvider timeProvider)
    {
        _context = context;
        _ruleEngine = ruleEngine;
        _timeProvider = timeProvider;
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

        // Evaluate 4 Gate-Out Business Rules
        var rules = new IBusinessRule[]
        {
            new ContainerIsInYardRule(lifecycle),
            new ContainerBelongsToLineOperatorRule(container.OwnerCode, order.LineOperator),
            new DeliveryOrderNotExpiredRule(order.ExpiryDate, _timeProvider.GetUtcNow().UtcDateTime),
            new DeliveryOrderHasRemainingQuantityRule(order, container.Size, container.Type)
        };

        var ruleResult = _ruleEngine.Evaluate(rules);
        if (!ruleResult.IsSuccess)
        {
            return Result.Failure(ruleResult.Error!);
        }

        // Allocate quantity in delivery order
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
        var occupiedSlots = await _context.YardSlots
            .Where(s => s.ContainerId == command.ContainerId)
            .ToListAsync(cancellationToken);

        foreach (var slot in occupiedSlots)
        {
            slot.Release();
        }

        // Gate out lifecycle
        var gateOutResult = lifecycle.GateOut(
            command.OutVehicleNumber,
            command.DeliveryOrderId,
            _timeProvider.GetUtcNow().UtcDateTime);
        if (gateOutResult.IsFailure)
        {
            return gateOutResult;
        }

        // Single transaction committing order fulfillment, slot release, and lifecycle update
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
