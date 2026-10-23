using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.ActivateLineOperator;

public sealed class ActivateLineOperatorCommandHandler : ICommandHandler<ActivateLineOperatorCommand>
{
    private readonly IAppDbContext _context;

    public ActivateLineOperatorCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ActivateLineOperatorCommand command, CancellationToken cancellationToken = default)
    {
        var lineOp = await _context.LineOperators
            .FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken);

        if (lineOp is null)
        {
            return Result.Failure(Error.NotFound("LineOperator.NotFound", "Không tìm thấy hãng khai thác."));
        }

        lineOp.Activate();
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
