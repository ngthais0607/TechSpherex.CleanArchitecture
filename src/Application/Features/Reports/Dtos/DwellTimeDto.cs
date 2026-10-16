namespace SnpDepotManagement.Application.Features.Reports.Dtos;

public sealed record DwellTimeDto(Guid LifecycleId, Guid ContainerId, string ContainerNumber,
    string LineOperator, DateTime InDate, DateTime? OutDate, int DaysInDepot);
