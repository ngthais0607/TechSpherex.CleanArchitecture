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

        var expandResult = block.Expand(command.NewBays, command.NewRows, command.NewTiers);
        if (expandResult.IsFailure)
        {
            return expandResult;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
