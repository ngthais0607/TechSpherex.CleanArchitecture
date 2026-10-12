using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Containers.Dtos;

public sealed record ContainerDto(
    Guid Id,
    string ContainerNumber,
    string IsoCode,
    ContainerSize Size,
    ContainerType Type,
    decimal TareWeight,
    decimal MaxGrossWeight,
    string OwnerCode,
    ContainerCondition Condition,
    Classification Classification,
    DateTime? DateOfManufacture);
