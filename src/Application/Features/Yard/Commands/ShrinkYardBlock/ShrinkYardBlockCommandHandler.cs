using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Yard.Commands.ShrinkYardBlock;

public sealed class ShrinkYardBlockCommandHandler : ICommandHandler<ShrinkYardBlockCommand>
{
    private readonly IAppDbContext _context;

    public ShrinkYardBlockCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ShrinkYardBlockCommand command, CancellationToken cancellationToken = default)
    {
        var block = await _context.YardBlocks
            .Include(b => b.Slots)
            .FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);

        if (block is null)
        {
            return Result.Failure(
                Error.NotFound("YardBlock.NotFound", "Yard block not found."));
        }

        var slotsToRemove = block.Slots
            .Where(s => s.Position.Bay > command.NewBays || s.Position.Row > command.NewRows || s.Position.Tier > command.NewTiers)
            .ToList();

        var firstOccupied = slotsToRemove.FirstOrDefault(s => s.IsOccupied);
        if (firstOccupied is not null)
        {
            string contInfo = "";
            if (firstOccupied.ContainerId.HasValue)
            {
                var container = await _context.Containers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == firstOccupied.ContainerId.Value, cancellationToken);
                if (container is not null)
                {
                    contInfo = $" ({container.ContainerNumber.Value})";
                }
            }

            return Result.Failure(
                Error.Conflict("YardBlock.CannotShrinkOccupiedSlots",
                    $"Không thể thu hẹp dãy vì vị trí {firstOccupied.Position.Bay:D2}-{firstOccupied.Position.Row:D2}-{firstOccupied.Position.Tier:D2} đang có container{contInfo}."));
        }

        var shrinkResult = block.Shrink(command.NewBays, command.NewRows, command.NewTiers);
        if (shrinkResult.IsFailure)
        {
            return shrinkResult;
        }

        if (slotsToRemove.Count > 0)
        {
            _context.YardSlots.RemoveRange(slotsToRemove);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
