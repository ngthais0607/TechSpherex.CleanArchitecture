using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Containers.Queries.GetContainerOrders;

public sealed record GetContainerOrdersQuery(string ContainerNumber) : IQuery<ContainerOrdersDto>;

public sealed record ContainerOrdersDto(
    OrderSummaryDto? CurrentOrder,
    ContainerOrderHistorySummary Summary,
    IReadOnlyList<OrderHistoryDto> History);

public sealed record ContainerOrderHistorySummary(
    int CustomerCount,
    int ShipmentCount,
    string? LastCustomer);

public sealed record OrderSummaryDto(
    Guid OrderId,
    string OrderNumber,
    string LineOperator,
    string CustomerName,
    DateTime ExpiryDate,
    string Status);

public sealed record OrderHistoryDto(
    Guid OrderId,
    string OrderNumber,
    string LineOperator,
    string CustomerName,
    DateTime ExpiryDate,
    DateTime? ShippedDate,
    string Status);
