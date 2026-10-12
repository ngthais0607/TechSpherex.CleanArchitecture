using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.CreateLineOperator;

public sealed class CreateLineOperatorCommandHandler : ICommandHandler<CreateLineOperatorCommand, Guid>
{
    private readonly IAppDbContext _context;

    public CreateLineOperatorCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateLineOperatorCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedCode = command.Code.Trim().ToUpperInvariant();

        var exists = await _context.LineOperators
            .AnyAsync(l => l.Code == normalizedCode, cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(
                Error.Conflict("LineOperator.AlreadyExists", $"Line operator with code '{normalizedCode}' already exists."));
        }

        var entityResult = LineOperator.Create(normalizedCode, command.Name);
        if (entityResult.IsFailure)
        {
            return Result.Failure<Guid>(entityResult.Error);
        }

        var entity = entityResult.Value!;
        _context.LineOperators.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);
    }
}
