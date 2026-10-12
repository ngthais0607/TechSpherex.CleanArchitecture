using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Application.Features.MasterData.Vehicles.Commands.CreateVehicle;

public sealed class CreateVehicleCommandHandler : ICommandHandler<CreateVehicleCommand, Guid>
{
    private readonly IAppDbContext _context;

    public CreateVehicleCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateVehicleCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedPlate = command.PlateNumber.Trim().ToUpperInvariant();

        var exists = await _context.Vehicles
            .AnyAsync(v => v.PlateNumber == normalizedPlate, cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(
                Error.Conflict("Vehicle.AlreadyExists", $"Vehicle with plate number '{normalizedPlate}' already exists."));
        }

        var vehicleResult = Vehicle.Create(normalizedPlate, command.Type);
        if (vehicleResult.IsFailure)
        {
            return Result.Failure<Guid>(vehicleResult.Error);
        }

        var vehicle = vehicleResult.Value!;
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(vehicle.Id);
    }
}
