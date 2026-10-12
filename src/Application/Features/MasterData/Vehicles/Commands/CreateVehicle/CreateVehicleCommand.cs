using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.MasterData.Vehicles.Commands.CreateVehicle;

public sealed record CreateVehicleCommand(string PlateNumber, VehicleType Type) : ICommand<Guid>;
