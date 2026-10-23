using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.UpdateLineOperator;

public sealed class UpdateLineOperatorCommandHandler : ICommandHandler<UpdateLineOperatorCommand>
{
    private readonly IAppDbContext _context;

    public UpdateLineOperatorCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateLineOperatorCommand command, CancellationToken cancellationToken = default)
    {
        var lineOp = await _context.LineOperators
            .FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken);

        if (lineOp is null)
        {
            return Result.Failure(Error.NotFound("LineOperator.NotFound", "Không tìm thấy hãng khai thác."));
        }

        var updateResult = lineOp.Update(command.Name);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
