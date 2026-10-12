using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Yard;

namespace SnpDepotManagement.Application.Features.Yard.Commands.CreateYardBlock;

public sealed class CreateYardBlockCommandHandler : ICommandHandler<CreateYardBlockCommand, Guid>
{
    private readonly IAppDbContext _context;

    public CreateYardBlockCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateYardBlockCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedCode = command.Code.Trim().ToUpperInvariant();

        var exists = await _context.YardBlocks
            .AnyAsync(b => b.Code == normalizedCode, cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(
                Error.Conflict("YardBlock.AlreadyExists", $"Yard block '{normalizedCode}' already exists."));
        }

        YardBlock block;
        if (command.IsVirtual)
        {
            block = YardBlock.CreateVirtual(normalizedCode, command.Name);
        }
        else
        {
            block = YardBlock.CreateStandard(normalizedCode, command.Name, command.Bays, command.Rows, command.Tiers);
        }

        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(block.Id);
    }
}
