using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.MasterData;

public sealed class Voyage : AuditableEntity
{
    public string VesselName { get; private set; } = string.Empty;
    public string VoyageNumber { get; private set; } = string.Empty;
    public DateTime Etd { get; private set; }

    private Voyage() { }

    public static Result<Voyage> Create(string? vesselName, string? voyageNumber, DateTime etd)
    {
        if (string.IsNullOrWhiteSpace(vesselName))
        {
            return Result.Failure<Voyage>(
                Error.Validation("Voyage.EmptyVesselName", "Vessel name cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(voyageNumber))
        {
            return Result.Failure<Voyage>(
                Error.Validation("Voyage.EmptyVoyageNumber", "Voyage number cannot be empty."));
        }

        return Result.Success(new Voyage
        {
            VesselName = vesselName.Trim().ToUpperInvariant(),
            VoyageNumber = voyageNumber.Trim().ToUpperInvariant(),
            Etd = etd
        });
    }
}
