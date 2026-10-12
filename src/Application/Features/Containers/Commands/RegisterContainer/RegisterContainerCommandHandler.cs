using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.Features.Containers.Commands.RegisterContainer;

public sealed class RegisterContainerCommandHandler : ICommandHandler<RegisterContainerCommand, Guid>
{
    private readonly IAppDbContext _context;

    public RegisterContainerCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(RegisterContainerCommand command, CancellationToken cancellationToken = default)
    {
        var numberResult = ContainerNumber.Create(command.ContainerNumber);
        if (numberResult.IsFailure)
        {
            return Result.Failure<Guid>(numberResult.Error);
        }

        var containerNumber = numberResult.Value!;

        var exists = await _context.Containers
            .AnyAsync(c => c.ContainerNumber == containerNumber, cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(
                Error.Conflict("Container.AlreadyExists", $"Container with number '{containerNumber.Value}' already exists in the depot."));
        }

        var containerResult = Container.Create(
            containerNumber,
            command.IsoCode,
            command.Size,
            command.Type,
            command.TareWeight,
            command.MaxGrossWeight,
            command.DateOfManufacture,
            command.OwnerCode);

        if (containerResult.IsFailure)
        {
            return Result.Failure<Guid>(containerResult.Error);
        }

        var container = containerResult.Value!;
        _context.Containers.Add(container);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(container.Id);
    }
}
