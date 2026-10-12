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
            .Where(s => s.Bay > command.NewBays || s.Row > command.NewRows || s.Tier > command.NewTiers)
            .ToList();

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
