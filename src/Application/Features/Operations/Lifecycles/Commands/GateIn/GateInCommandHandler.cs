using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Rules;
using SnpDepotManagement.Application.Common.Rules;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Rules;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

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
        Container? container = null;

        if (!string.IsNullOrWhiteSpace(command.ContainerNumber))
        {
            var parsed = ContainerNumber.Create(command.ContainerNumber);
            if (parsed.IsFailure)
            {
                return Result.Failure<Guid>(parsed.Error);
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
            return Result.Failure<Guid>(
                Error.NotFound("Container.NotFound", "Không tìm thấy container."));
        }

        var activeLifecycle = await _context.ContainerLifecycles
            .FirstOrDefaultAsync(l => l.ContainerId == container.Id && l.Status == LifecycleStatus.InYard, cancellationToken);

        if (activeLifecycle is not null)
        {
            return Result.Failure<Guid>(
                Error.Conflict("Lifecycle.ContainerAlreadyInYard", "Container đã ở trong bãi."));
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        // Xử lý block ảo
        bool isVirtualPlacement = !string.IsNullOrWhiteSpace(command.VirtualBlockCode)
            && !command.SlotId.HasValue
            && string.IsNullOrWhiteSpace(command.BlockCode);

        if (isVirtualPlacement)
        {
            var normVBlock = command.VirtualBlockCode!.Trim().ToUpperInvariant();
            var vBlock = await _context.YardBlocks
                .FirstOrDefaultAsync(b => b.Code == normVBlock && b.IsVirtual, cancellationToken);

            if (vBlock is null)
            {
                return Result.Failure<Guid>(
                    Error.NotFound("YardBlock.NotFound", $"Không tìm thấy block ảo '{command.VirtualBlockCode}'."));
            }

            var lifecycle = ContainerLifecycle.GateIn(
                container.Id,
                container.OwnerCode,
                command.EffectiveGrade,
                command.Condition,
                command.EffectiveVehicleNumber,
                null,
                utcNow,
                tenantId: container.TenantId,
                vehicleType: command.VehicleType,
                virtualBlockId: vBlock.Id);

            _context.ContainerLifecycles.Add(lifecycle);

            var gateTx = GateTransaction.CreateGateIn(
                container.Id,
                command.EffectiveVehicleNumber,
                command.VehicleType,
                command.DriverName,
                null,
                vBlock.Code,
                container.TenantId);
            _context.GateTransactions.Add(gateTx);

            MovementHistory.Append(_context, lifecycle, ContainerMovementType.GateIn, utcNow, _currentUser?.UserId,
                null, new MovementPosition(vBlock.Id, vBlock.Code, 0, 0, 0), command.EffectiveVehicleNumber, command.VehicleType);

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(lifecycle.Id);
        }

        // Xử lý vị trí bãi vật lý
        if (container.Size == ContainerSize.Ft20 && command.Bay.HasValue && command.Bay.Value % 2 == 0)
        {
            return Result.Failure<Guid>(
                Error.Validation("StackRule.EvenBayInvalidFor20Ft", "Container 20ft chỉ được đặt tại bay lẻ."));
        }

        YardSlot? slot = null;
        if (command.SlotId.HasValue && command.SlotId.Value != Guid.Empty)
        {
            slot = await _context.YardSlots
                .FirstOrDefaultAsync(s => s.Id == command.SlotId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(command.BlockCode) && command.Bay.HasValue && command.Row.HasValue && command.Tier.HasValue)
        {
            var normBlock = command.BlockCode.Trim().ToUpperInvariant();
            var blockEntity = await _context.YardBlocks
                .FirstOrDefaultAsync(b => b.Code == normBlock && !b.IsVirtual, cancellationToken);

            if (blockEntity is null)
            {
                return Result.Failure<Guid>(
                    Error.NotFound("YardBlock.NotFound", $"Không tìm thấy dãy bãi '{command.BlockCode}'."));
            }

            slot = await _context.YardSlots
                .FirstOrDefaultAsync(s => s.BlockId == blockEntity.Id && s.Position.Bay == command.Bay.Value && s.Position.Row == command.Row.Value && s.Position.Tier == command.Tier.Value, cancellationToken);
        }

        if (slot is null)
        {
            return Result.Failure<Guid>(
                Error.NotFound("YardSlot.NotFound", "Vị trí bãi không tồn tại."));
        }

        if (slot.IsOccupied)
        {
            return Result.Failure<Guid>(
                Error.Conflict("YardSlot.SlotAlreadyOccupied", "Vị trí bãi đã có container."));
        }

        var block = await _context.YardBlocks.Include(b => b.Slots)
            .FirstAsync(b => b.Id == slot.BlockId, cancellationToken);
        var logicalBay = container.Size == ContainerSize.Ft40 ? slot.Bay + 1 : slot.Bay;

        var belowTier = slot.Tier - 1;
        var occupiedBelowIds = block.Slots
            .Where(s => s.Row == slot.Row && s.Tier == belowTier && s.ContainerId.HasValue)
            .Select(s => s.ContainerId!.Value)
            .Distinct()
            .ToList();

        var containerSizes = occupiedBelowIds.Count > 0
            ? await _context.Containers
                .Where(c => occupiedBelowIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Size })
                .ToDictionaryAsync(c => c.Id, c => (ContainerSize?)c.Size, cancellationToken)
            : new Dictionary<Guid, ContainerSize?>();

        var belowSlotTuples = block.Slots
            .Where(s => s.Row == slot.Row && s.Tier == belowTier)
            .Select(s => (s.Bay, s.Row, s.Tier, s.ContainerId, s.ContainerId.HasValue && containerSizes.TryGetValue(s.ContainerId.Value, out var sz) ? sz : null));

        var ruleResult = _ruleEngine.Evaluate(new IBusinessRule[]
        {
            new ValidBayContainerSizeRule(logicalBay, container.Size, block.MaxBays),
            new TierWithinBlockLimitRule(slot.Tier, block.MaxTiers),
            new SlotIsFreeRule(container.Size, logicalBay, slot.Row, slot.Tier, block.Slots),
            new SlotBelowMustBeOccupiedRule(slot.Tier, container.Size, logicalBay, slot.Row, block.Slots),
            new StackCompatibilityRule(container.Size, logicalBay, slot.Row, slot.Tier, belowSlotTuples)
        });
        if (!ruleResult.IsSuccess) return Result.Failure<Guid>(ruleResult.Error!);

        foreach (var support in block.Slots.Where(s => s.Row == slot.Row && s.Tier == slot.Tier &&
                     (s.Bay == slot.Bay || (container.Size == ContainerSize.Ft40 && s.Bay == slot.Bay + 2))))
            support.Occupy(container.Id);

        // Create lifecycle record
        var physicalLifecycle = ContainerLifecycle.GateIn(
            container.Id,
            container.OwnerCode,
            command.EffectiveGrade,
            command.Condition,
            command.EffectiveVehicleNumber,
            slot.Id,
            utcNow,
            tenantId: container.TenantId,
            vehicleType: command.VehicleType);

        _context.ContainerLifecycles.Add(physicalLifecycle);

        var physicalGateTx = GateTransaction.CreateGateIn(
            container.Id,
            command.EffectiveVehicleNumber,
            command.VehicleType,
            command.DriverName,
            slot.Id,
            null,
            container.TenantId);
        _context.GateTransactions.Add(physicalGateTx);

        MovementHistory.Append(_context, physicalLifecycle, ContainerMovementType.GateIn, utcNow, _currentUser?.UserId,
            null, MovementHistory.Position(block, logicalBay, slot.Row, slot.Tier), command.EffectiveVehicleNumber, command.VehicleType);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(physicalLifecycle.Id);
    }
}
