using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Rules;
using SnpDepotManagement.Application.Common.Rules;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Rules;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;

public sealed class GateInCommandHandler : ICommandHandler<GateInCommand, Guid>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser? _currentUser;
    private readonly IRuleEngine _ruleEngine;

    public GateInCommandHandler(IAppDbContext context, TimeProvider timeProvider, ICurrentUser? currentUser = null, IRuleEngine? ruleEngine = null)
    {
        _context = context;
        _timeProvider = timeProvider;
        _currentUser = currentUser;
        _ruleEngine = ruleEngine ?? new RuleEngine();
    }

    public async Task<Result<Guid>> Handle(GateInCommand command, CancellationToken cancellationToken = default)
    {
        var container = await _context.Containers
            .FirstOrDefaultAsync(c => c.Id == command.ContainerId, cancellationToken);

        if (container is null)
        {
            return Result.Failure<Guid>(
                Error.NotFound("Container.NotFound", "Container not found."));
        }

        var slot = await _context.YardSlots
            .FirstOrDefaultAsync(s => s.Id == command.SlotId, cancellationToken);

        if (slot is null)
        {
            return Result.Failure<Guid>(
                Error.NotFound("YardSlot.NotFound", "Yard slot not found."));
        }

        if (slot.IsOccupied)
        {
            return Result.Failure<Guid>(
                Error.Conflict("YardSlot.SlotAlreadyOccupied", "Yard slot is already occupied."));
        }

        var activeLifecycle = await _context.ContainerLifecycles
            .FirstOrDefaultAsync(l => l.ContainerId == command.ContainerId && l.Status == LifecycleStatus.InYard, cancellationToken);

        if (activeLifecycle is not null)
        {
            return Result.Failure<Guid>(
                Error.Conflict("Lifecycle.ContainerAlreadyInYard", "Container is already marked as in yard."));
        }

        var block = await _context.YardBlocks.Include(b => b.Slots)
            .FirstAsync(b => b.Id == slot.BlockId, cancellationToken);
        var logicalBay = container.Size == ContainerSize.Ft40 ? slot.Bay + 1 : slot.Bay;
        var ruleResult = _ruleEngine.Evaluate(new IBusinessRule[]
        {
            new ValidBayContainerSizeRule(logicalBay, container.Size, block.MaxBays),
            new TierWithinBlockLimitRule(slot.Tier, block.MaxTiers),
            new SlotIsFreeRule(container.Size, logicalBay, slot.Row, slot.Tier, block.Slots),
            new SlotBelowMustBeOccupiedRule(slot.Tier, container.Size, logicalBay, slot.Row, block.Slots)
        });
        if (!ruleResult.IsSuccess) return Result.Failure<Guid>(ruleResult.Error!);

        foreach (var support in block.Slots.Where(s => s.Row == slot.Row && s.Tier == slot.Tier &&
                     (s.Bay == slot.Bay || (container.Size == ContainerSize.Ft40 && s.Bay == slot.Bay + 2))))
            support.Occupy(container.Id);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        // Create lifecycle record
        var lifecycle = ContainerLifecycle.GateIn(
            container.Id,
            container.OwnerCode,
            command.Classification,
            command.Condition,
            command.InVehicleNumber,
            command.SlotId,
            utcNow,
            tenantId: container.TenantId,
            vehicleType: command.VehicleType);

        _context.ContainerLifecycles.Add(lifecycle);
        MovementHistory.Append(_context, lifecycle, ContainerMovementType.GateIn, utcNow, _currentUser?.UserId,
            null, MovementHistory.Position(block, logicalBay, slot.Row, slot.Tier), command.InVehicleNumber, command.VehicleType);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(lifecycle.Id);
    }
}
