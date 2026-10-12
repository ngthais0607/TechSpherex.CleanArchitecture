using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Containers.Commands.MarkContainerDamaged;

public sealed class MarkContainerDamagedCommandHandler : ICommandHandler<MarkContainerDamagedCommand>
{
    private readonly IAppDbContext _context;

    public MarkContainerDamagedCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(MarkContainerDamagedCommand command, CancellationToken cancellationToken = default)
    {
        var container = await _context.Containers
            .FirstOrDefaultAsync(c => c.Id == command.ContainerId, cancellationToken);

        if (container is null)
        {
            return Result.Failure(
                Error.NotFound("Container.NotFound", "Container not found."));
        }

        var markResult = container.MarkDamaged(command.Condition);
        if (markResult.IsFailure)
        {
            return markResult;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
