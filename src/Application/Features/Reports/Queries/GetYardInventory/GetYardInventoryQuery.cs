using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Reports.Dtos;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetYardInventory;

public sealed record GetYardInventoryQuery(string? LineOperator = null, ContainerSize? Size = null,
    ContainerType? Type = null, ContainerCondition? Condition = null, Guid? BlockId = null,
    int PageIndex = 1, int PageSize = 20) : IQuery<InventoryReportDto>;
