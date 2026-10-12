using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.MasterData.Vehicles.Dtos;

public sealed record VehicleDto(Guid Id, string PlateNumber, VehicleType Type);
