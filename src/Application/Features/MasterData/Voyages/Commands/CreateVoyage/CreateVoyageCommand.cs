using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.MasterData.Voyages.Commands.CreateVoyage;

public sealed record CreateVoyageCommand(string VesselName, string VoyageNumber, DateTime Etd) : ICommand<Guid>;
