using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Abstractions.Rules;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Application.Features.Yard.Commands.PlaceContainer;

public sealed class PlaceContainerCommandHandler : ICommandHandler<PlaceContainerCommand>
{
    private readonly IAppDbContext _context;
    private readonly IRuleEngine _ruleEngine;

    public PlaceContainerCommandHandler(IAppDbContext context, IRuleEngine ruleEngine)
    {
        _context = context;
        _ruleEngine = ruleEngine;
    }

    public async Task<Result> Handle(PlaceContainerCommand command, CancellationToken cancellationToken = default)
    {
        var container = await _context.Containers
            .FirstOrDefaultAsync(c => c.Id == command.ContainerId, cancellationToken);

        if (container is null)
        {
            return Result.Failure(
                Error.NotFound("Container.NotFound", "Container not found."));
        }

        var block = await _context.YardBlocks
            .Include(b => b.Slots)
            .FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);

        if (block is null)
        {
            return Result.Failure(
                Error.NotFound("YardBlock.NotFound", "Yard block not found."));
        }

        if (block.IsVirtual)
        {
            return Result.Failure(
                Error.Conflict("YardBlock.CannotPlaceInVirtual", "Cannot place containers in virtual yard blocks."));
        }

        var alreadyPlaced = await _context.YardSlots
            .AnyAsync(s => s.ContainerId == command.ContainerId, cancellationToken);

        if (alreadyPlaced)
        {
            return Result.Failure(
                Error.Conflict("YardSlot.ContainerAlreadyPlaced", "Container is already placed in a yard slot."));
        }

        var rules = new IBusinessRule[]
        {
            new ValidBayContainerSizeRule(command.Bay, container.Size, block.MaxBays),
            new TierWithinBlockLimitRule(command.Tier, block.MaxTiers),
            new SlotIsFreeRule(container.Size, command.Bay, command.Row, command.Tier, block.Slots),
            new SlotBelowMustBeOccupiedRule(command.Tier, container.Size, command.Bay, command.Row, block.Slots)
        };

        var ruleResult = _ruleEngine.Evaluate(rules);
        if (!ruleResult.IsSuccess)
        {
            return Result.Failure(ruleResult.Error!);
        }

        if (container.Size == ContainerSize.Ft20)
        {
            var slot = block.Slots.First(s => s.Bay == command.Bay && s.Row == command.Row && s.Tier == command.Tier);
            slot.Occupy(container.Id);
        }
        else if (container.Size == ContainerSize.Ft40)
        {
            var slot1 = block.Slots.First(s => s.Bay == command.Bay - 1 && s.Row == command.Row && s.Tier == command.Tier);
            var slot2 = block.Slots.First(s => s.Bay == command.Bay + 1 && s.Row == command.Row && s.Tier == command.Tier);
            slot1.Occupy(container.Id);
            slot2.Occupy(container.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
