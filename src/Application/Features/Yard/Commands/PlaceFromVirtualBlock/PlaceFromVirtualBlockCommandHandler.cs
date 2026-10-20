using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Abstractions.Rules;
using SnpDepotManagement.Application.Features.Operations.Lifecycles;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.Features.Yard.Commands.PlaceFromVirtualBlock;

public sealed class PlaceFromVirtualBlockCommandHandler : ICommandHandler<PlaceFromVirtualBlockCommand>
{
    private readonly IAppDbContext _context;
    private readonly IRuleEngine _ruleEngine;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser? _currentUser;

    public PlaceFromVirtualBlockCommandHandler(
        IAppDbContext context,
        IRuleEngine ruleEngine,
        TimeProvider timeProvider,
        ICurrentUser? currentUser = null)
    {
        _context = context;
        _ruleEngine = ruleEngine;
        _timeProvider = timeProvider;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(PlaceFromVirtualBlockCommand command, CancellationToken cancellationToken = default)
    {
        var normVCode = command.VirtualBlockCode.Trim().ToUpperInvariant();
        var vBlock = await _context.YardBlocks
            .FirstOrDefaultAsync(b => b.Code == normVCode && b.IsVirtual, cancellationToken);

        if (vBlock is null)
        {
            return Result.Failure(
                Error.NotFound("YardBlock.NotFound", $"Không tìm thấy dãy ảo '{command.VirtualBlockCode}'."));
        }

        var container = await _context.Containers
            .FirstOrDefaultAsync(c => c.Id == command.ContainerId, cancellationToken);

        if (container is null)
        {
            return Result.Failure(
                Error.NotFound("Container.NotFound", "Không tìm thấy container."));
        }

        var lifecycle = await _context.ContainerLifecycles
            .FirstOrDefaultAsync(l => l.ContainerId == container.Id && l.Status == LifecycleStatus.InYard && l.VirtualBlockId == vBlock.Id, cancellationToken);

        if (lifecycle is null)
        {
            return Result.Failure(
                Error.Conflict("Lifecycle.NotInYard", "Container hiện không nằm trong block ảo chỉ định."));
        }

        var normTargetCode = command.TargetBlockCode.Trim().ToUpperInvariant();
        var targetBlock = await _context.YardBlocks
            .Include(b => b.Slots)
            .FirstOrDefaultAsync(b => b.Code == normTargetCode && !b.IsVirtual, cancellationToken);

        if (targetBlock is null)
        {
            return Result.Failure(
                Error.NotFound("YardBlock.NotFound", $"Không tìm thấy dãy bãi '{command.TargetBlockCode}'."));
        }

        var slot = targetBlock.Slots.FirstOrDefault(s =>
            s.Position.Bay == command.Bay && s.Position.Row == command.Row && s.Position.Tier == command.Tier);

        if (slot is null)
        {
            return Result.Failure(
                Error.NotFound("YardSlot.NotFound", "Vị trí bãi không tồn tại."));
        }

        if (slot.IsOccupied)
        {
            return Result.Failure(
                Error.Conflict("YardSlot.SlotAlreadyOccupied", "Vị trí bãi đã có container."));
        }

        var belowTier = command.Tier - 1;
        var occupiedBelowIds = targetBlock.Slots
            .Where(s => s.Position.Row == command.Row && s.Position.Tier == belowTier && s.ContainerId.HasValue)
            .Select(s => s.ContainerId!.Value)
            .Distinct()
            .ToList();

        var containerSizes = occupiedBelowIds.Count > 0
            ? await _context.Containers
                .Where(c => occupiedBelowIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Size })
                .ToDictionaryAsync(c => c.Id, c => (ContainerSize?)c.Size, cancellationToken)
            : new Dictionary<Guid, ContainerSize?>();

        var belowSlotTuples = targetBlock.Slots
            .Where(s => s.Position.Row == command.Row && s.Position.Tier == belowTier)
            .Select(s => (s.Position.Bay, s.Position.Row, s.Position.Tier, s.ContainerId, s.ContainerId.HasValue && containerSizes.TryGetValue(s.ContainerId.Value, out var sz) ? sz : null));

        var rules = new IBusinessRule[]
        {
            new ValidBayContainerSizeRule(command.Bay, container.Size, targetBlock.MaxBays),
            new TierWithinBlockLimitRule(command.Tier, targetBlock.MaxTiers),
            new SlotIsFreeRule(container.Size, command.Bay, command.Row, command.Tier, targetBlock.Slots),
            new SlotBelowMustBeOccupiedRule(command.Tier, container.Size, command.Bay, command.Row, targetBlock.Slots),
            new StackCompatibilityRule(container.Size, command.Bay, command.Row, command.Tier, belowSlotTuples)
        };

        var ruleResult = _ruleEngine.Evaluate(rules);
        if (!ruleResult.IsSuccess)
        {
            return Result.Failure(ruleResult.Error!);
        }

        if (container.Size == ContainerSize.Ft20)
        {
            var targetSlot = targetBlock.Slots.First(s => s.Position.Bay == command.Bay && s.Position.Row == command.Row && s.Position.Tier == command.Tier);
            targetSlot.Occupy(container.Id);
        }
        else if (container.Size == ContainerSize.Ft40)
        {
            var slot1 = targetBlock.Slots.First(s => s.Position.Bay == command.Bay - 1 && s.Position.Row == command.Row && s.Position.Tier == command.Tier);
            var slot2 = targetBlock.Slots.First(s => s.Position.Bay == command.Bay + 1 && s.Position.Row == command.Row && s.Position.Tier == command.Tier);
            slot1.Occupy(container.Id);
            slot2.Occupy(container.Id);
        }

        var lowerBay = container.Size == ContainerSize.Ft40 ? command.Bay - 1 : command.Bay;
        var currentSlot = targetBlock.Slots.First(s => s.Position.Bay == lowerBay && s.Position.Row == command.Row && s.Position.Tier == command.Tier);
        lifecycle.SetCurrentSlot(currentSlot.Id);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var sourcePos = new MovementPosition(vBlock.Id, vBlock.Code, 0, 0, 0);
        var destPos = MovementHistory.Position(targetBlock, command.Bay, command.Row, command.Tier);

        MovementHistory.Append(_context, lifecycle, ContainerMovementType.Relocate, utcNow,
            _currentUser?.UserId, sourcePos, destPos);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
