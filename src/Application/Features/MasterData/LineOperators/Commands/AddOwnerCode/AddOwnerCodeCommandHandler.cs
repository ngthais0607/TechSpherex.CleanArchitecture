using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.AddOwnerCode;

public sealed class AddOwnerCodeCommandHandler : ICommandHandler<AddOwnerCodeCommand>
{
    private readonly IAppDbContext _context;

    public AddOwnerCodeCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(AddOwnerCodeCommand command, CancellationToken cancellationToken = default)
    {
        var lineOpExists = await _context.LineOperators
            .AnyAsync(l => l.Id == command.LineOperatorId, cancellationToken);

        if (!lineOpExists)
        {
            return Result.Failure(Error.NotFound("LineOperator.NotFound", "Hãng khai thác không tồn tại."));
        }

        if (string.IsNullOrWhiteSpace(command.OwnerCode) || command.OwnerCode.Trim().Length > 4)
        {
            return Result.Failure(Error.Validation("LineOperator.InvalidOwnerCode", "Mã tiền tố không hợp lệ (tối đa 4 chữ cái)."));
        }

        var normalized = command.OwnerCode.Trim().ToUpperInvariant();

        var codeExists = await _context.LineOperatorOwnerCodes
            .AnyAsync(x => x.OwnerCode == normalized, cancellationToken);

        if (codeExists)
        {
            return Result.Failure(Error.Conflict("LineOperator.OwnerCodeAlreadyExists", $"Mã tiền tố '{normalized}' đã tồn tại trong hệ thống."));
        }

        var entity = LineOperatorOwnerCode.Create(command.LineOperatorId, normalized);
        _context.LineOperatorOwnerCodes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
