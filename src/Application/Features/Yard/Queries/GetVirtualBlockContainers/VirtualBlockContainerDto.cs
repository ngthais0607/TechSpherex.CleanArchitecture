namespace SnpDepotManagement.Application.Features.Yard.Queries.GetVirtualBlockContainers;

public sealed record VirtualBlockContainerDto(
    Guid Id,
    string ContainerNumber,
    string IsoCode,
    string Size,
    string Type,
    string Grade,
    string Condition,
    DateTime GateInTime);
