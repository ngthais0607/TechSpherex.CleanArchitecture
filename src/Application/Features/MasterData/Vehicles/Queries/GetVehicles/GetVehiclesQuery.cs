using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.Vehicles.Dtos;

namespace SnpDepotManagement.Application.Features.MasterData.Vehicles.Queries.GetVehicles;

public sealed record GetVehiclesQuery : IQuery<IReadOnlyList<VehicleDto>>;
