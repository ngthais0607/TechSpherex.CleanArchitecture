using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.Vehicles.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.Vehicles.Queries.GetVehicles;

public sealed record GetVehiclesQuery(int PageIndex = 1, int PageSize = 20) : IQuery<PagedResult<VehicleDto>>;
