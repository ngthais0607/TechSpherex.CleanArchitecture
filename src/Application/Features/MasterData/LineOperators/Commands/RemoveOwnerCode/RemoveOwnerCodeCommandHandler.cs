using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.RemoveOwnerCode;

public sealed class RemoveOwnerCodeCommandHandler : ICommandHandler<RemoveOwnerCodeCommand>
{
    private readonly IAppDbContext _context;

    public RemoveOwnerCodeCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(RemoveOwnerCodeCommand command, CancellationToken cancellationToken = default)
    {
        var normalized = command.OwnerCode?.Trim().ToUpperInvariant() ?? string.Empty;

        var entity = await _context.LineOperatorOwnerCodes
            .FirstOrDefaultAsync(x => x.LineOperatorId == command.LineOperatorId && x.OwnerCode == normalized, cancellationToken);

        if (entity is null)
        {
            return Result.Failure(Error.NotFound("LineOperator.OwnerCodeNotFound", $"Mã tiền tố '{normalized}' không tìm thấy cho hãng khai thác này."));
        }

        _context.LineOperatorOwnerCodes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
