using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Yard.Commands.RemoveContainer;

public sealed class RemoveContainerCommandHandler : ICommandHandler<RemoveContainerCommand>
{
    private readonly IAppDbContext _context;

    public RemoveContainerCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(RemoveContainerCommand command, CancellationToken cancellationToken = default)
    {
        var block = await _context.YardBlocks
            .Include(b => b.Slots)
            .FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);

        if (block is null)
        {
            return Result.Failure(
                Error.NotFound("YardBlock.NotFound", "Yard block not found."));
        }

        bool is40Ft = command.Bay % 2 == 0;

        if (!is40Ft)
        {
            var slot = block.Slots.FirstOrDefault(s => s.Bay == command.Bay && s.Row == command.Row && s.Tier == command.Tier);
            if (slot is null || !slot.IsOccupied)
            {
                return Result.Failure(
                    Error.NotFound("YardSlot.SlotNotOccupied", "No container found at the specified yard slot."));
            }

            // Check if there is a container directly on top
            bool hasContainerAbove = block.Slots.Any(s =>
                s.Bay == command.Bay &&
                s.Row == command.Row &&
                s.Tier == command.Tier + 1 &&
                s.IsOccupied);

            if (hasContainerAbove)
            {
                return Result.Failure(
                    Error.Conflict("YardSlot.CannotRemoveWithContainersAbove", "Cannot remove container because another container is stacked on top of it."));
            }

            slot.Release();
        }
        else
        {
            int bayBefore = command.Bay - 1;
            int bayAfter = command.Bay + 1;

            var slot1 = block.Slots.FirstOrDefault(s => s.Bay == bayBefore && s.Row == command.Row && s.Tier == command.Tier);
            var slot2 = block.Slots.FirstOrDefault(s => s.Bay == bayAfter && s.Row == command.Row && s.Tier == command.Tier);

            if (slot1 is null || slot2 is null || !slot1.IsOccupied || !slot2.IsOccupied)
            {
                return Result.Failure(
                    Error.NotFound("YardSlot.SlotNotOccupied", "No container found at the specified 40ft yard position."));
            }

            // Check if there is any container on top of either slot
            bool hasContainerAbove = block.Slots.Any(s =>
                (s.Bay == bayBefore || s.Bay == bayAfter) &&
                s.Row == command.Row &&
                s.Tier == command.Tier + 1 &&
                s.IsOccupied);

            if (hasContainerAbove)
            {
                return Result.Failure(
                    Error.Conflict("YardSlot.CannotRemoveWithContainersAbove", "Cannot remove container because another container is stacked on top of it."));
            }

            slot1.Release();
            slot2.Release();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
