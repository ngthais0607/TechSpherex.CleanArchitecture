using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Yard.Commands.ExpandYardBlock;

public sealed class ExpandYardBlockCommandHandler : ICommandHandler<ExpandYardBlockCommand>
{
    private readonly IAppDbContext _context;

    public ExpandYardBlockCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ExpandYardBlockCommand command, CancellationToken cancellationToken = default)
    {
        var block = await _context.YardBlocks
            .Include(b => b.Slots)
            .FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);

        if (block is null)
        {
            return Result.Failure(
                Error.NotFound("YardBlock.NotFound", "Yard block not found."));
        }

        var existingSlotIds = block.Slots.Select(s => s.Id).ToHashSet();
        var expandResult = block.Expand(command.NewBays, command.NewRows, command.NewTiers);
        if (expandResult.IsFailure)
        {
            return expandResult;
        }

        // Slot IDs are assigned by the domain. Explicitly insert newly added children;
        // discovery on a tracked aggregate otherwise treats their nonempty keys as updates.
        _context.YardSlots.AddRange(block.Slots.Where(s => !existingSlotIds.Contains(s.Id)));
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
