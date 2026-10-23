using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.Entities.MasterData;

public sealed class Vehicle : AuditableEntity
{
    public string PlateNumber { get; private set; } = string.Empty;
    public VehicleType Type { get; private set; }

    private Vehicle() { }

    public static Result<Vehicle> Create(string? plateNumber, VehicleType type)
    {
        if (string.IsNullOrWhiteSpace(plateNumber))
        {
            return Result.Failure<Vehicle>(
                Error.Validation("Vehicle.EmptyPlateNumber", "Biển số xe không được để trống."));
        }

        return Result.Success(new Vehicle
        {
            PlateNumber = plateNumber.Trim().ToUpperInvariant(),
            Type = type
        });
    }
}
