using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Abstractions.Rules;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Application.Features.Yard.Commands.RelocateContainer;

public sealed class RelocateContainerCommandHandler : ICommandHandler<RelocateContainerCommand>
{
    private readonly IAppDbContext _context;
    private readonly IRuleEngine _ruleEngine;

    public RelocateContainerCommandHandler(IAppDbContext context, IRuleEngine ruleEngine)
    {
        _context = context;
        _ruleEngine = ruleEngine;
    }

    public async Task<Result> Handle(RelocateContainerCommand command, CancellationToken cancellationToken = default)
    {
        var block = await _context.YardBlocks
            .Include(b => b.Slots)
            .FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);

        if (block is null)
        {
            return Result.Failure(
                Error.NotFound("YardBlock.NotFound", "Yard block not found."));
        }

        bool isSource40Ft = command.SourceBay % 2 == 0;
        YardSlot? sourceSlot1 = null;
        YardSlot? sourceSlot2 = null;
        Guid containerId;

        if (!isSource40Ft)
        {
            sourceSlot1 = block.Slots.FirstOrDefault(s =>
                s.Bay == command.SourceBay &&
                s.Row == command.SourceRow &&
                s.Tier == command.SourceTier);

            if (sourceSlot1 is null || !sourceSlot1.IsOccupied || !sourceSlot1.ContainerId.HasValue)
            {
                return Result.Failure(
                    Error.NotFound("YardSlot.SlotNotOccupied", "No container found at source yard slot."));
            }

            // Check if there is container stacked above
            bool hasAbove = block.Slots.Any(s =>
                s.Bay == command.SourceBay &&
                s.Row == command.SourceRow &&
                s.Tier == command.SourceTier + 1 &&
                s.IsOccupied);

            if (hasAbove)
            {
                return Result.Failure(
                    Error.Conflict("YardSlot.CannotRemoveWithContainersAbove", "Cannot relocate container because another container is stacked on top of it."));
            }

            containerId = sourceSlot1.ContainerId.Value;
        }
        else
        {
            int bayBefore = command.SourceBay - 1;
            int bayAfter = command.SourceBay + 1;

            sourceSlot1 = block.Slots.FirstOrDefault(s => s.Bay == bayBefore && s.Row == command.SourceRow && s.Tier == command.SourceTier);
            sourceSlot2 = block.Slots.FirstOrDefault(s => s.Bay == bayAfter && s.Row == command.SourceRow && s.Tier == command.SourceTier);

            if (sourceSlot1 is null || sourceSlot2 is null || !sourceSlot1.IsOccupied || !sourceSlot2.IsOccupied || !sourceSlot1.ContainerId.HasValue)
            {
                return Result.Failure(
                    Error.NotFound("YardSlot.SlotNotOccupied", "No container found at source 40ft yard position."));
            }

            bool hasAbove = block.Slots.Any(s =>
                (s.Bay == bayBefore || s.Bay == bayAfter) &&
                s.Row == command.SourceRow &&
                s.Tier == command.SourceTier + 1 &&
                s.IsOccupied);

            if (hasAbove)
            {
                return Result.Failure(
                    Error.Conflict("YardSlot.CannotRemoveWithContainersAbove", "Cannot relocate container because another container is stacked on top of it."));
            }

            containerId = sourceSlot1.ContainerId.Value;
        }

        var container = await _context.Containers
            .FirstOrDefaultAsync(c => c.Id == containerId, cancellationToken);

        if (container is null)
        {
            return Result.Failure(
                Error.NotFound("Container.NotFound", "Container not found."));
        }

        // Temporarily release source slot(s) in memory for evaluation
        sourceSlot1.Release();
        sourceSlot2?.Release();

        // Evaluate 4 rules for target slot
        var rules = new IBusinessRule[]
        {
            new ValidBayContainerSizeRule(command.TargetBay, container.Size, block.MaxBays),
            new TierWithinBlockLimitRule(command.TargetTier, block.MaxTiers),
            new SlotIsFreeRule(container.Size, command.TargetBay, command.TargetRow, command.TargetTier, block.Slots),
            new SlotBelowMustBeOccupiedRule(command.TargetTier, container.Size, command.TargetBay, command.TargetRow, block.Slots)
        };

        var ruleResult = _ruleEngine.Evaluate(rules);
        if (!ruleResult.IsSuccess)
        {
            // Rollback source release in memory
            sourceSlot1.Occupy(containerId);
            sourceSlot2?.Occupy(containerId);

            return Result.Failure(ruleResult.Error!);
        }

        // Occupy target slot(s)
        if (container.Size == ContainerSize.Ft20)
        {
            var targetSlot = block.Slots.First(s =>
                s.Bay == command.TargetBay &&
                s.Row == command.TargetRow &&
                s.Tier == command.TargetTier);

            targetSlot.Occupy(containerId);
        }
        else
        {
            var targetSlot1 = block.Slots.First(s =>
                s.Bay == command.TargetBay - 1 &&
                s.Row == command.TargetRow &&
                s.Tier == command.TargetTier);

            var targetSlot2 = block.Slots.First(s =>
                s.Bay == command.TargetBay + 1 &&
                s.Row == command.TargetRow &&
                s.Tier == command.TargetTier);

            targetSlot1.Occupy(containerId);
            targetSlot2.Occupy(containerId);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
