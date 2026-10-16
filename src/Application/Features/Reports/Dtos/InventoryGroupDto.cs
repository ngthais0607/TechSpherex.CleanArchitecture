using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Reports.Dtos;

public sealed record InventoryGroupDto(string LineOperator, ContainerSize Size, ContainerType Type,
    ContainerCondition Condition, Guid? BlockId, string? BlockCode, int ContainerCount);
