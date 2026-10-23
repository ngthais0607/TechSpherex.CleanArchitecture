namespace SnpDepotManagement.Application.Features.Lookups.Dtos;

public sealed record LookupsDto(
    Dictionary<string, string> ContainerSizes,
    Dictionary<string, string> ContainerTypes,
    Dictionary<string, string> ContainerConditions,
    Dictionary<string, string> Classifications,
    Dictionary<string, string> VehicleTypes);
