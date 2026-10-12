namespace SnpDepotManagement.Application.Features.MasterData.Voyages.Dtos;

public sealed record VoyageDto(Guid Id, string VesselName, string VoyageNumber, DateTime Etd);
