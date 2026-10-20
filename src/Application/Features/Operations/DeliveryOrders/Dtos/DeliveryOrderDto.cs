using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Dtos;

public sealed record DeliveryOrderItemDto(
    Guid Id,
    ContainerSize Size,
    ContainerType Type,
    int RequiredQuantity,
    int DeliveredQuantity,
    int RemainingQuantity);

public sealed record DeliveryOrderDto(
    Guid Id,
    string OrderNumber,
    string LineOperator,
    string CustomerTaxCode,
    string CustomerName,
    DateTime ExpiryDate,
    string OutboundVessel,
    bool IsCompleted,
    List<DeliveryOrderItemDto> Items,
    string Status = "");

public sealed record CreateDeliveryOrderItemInput(
    ContainerSize Size,
    ContainerType Type,
    int Quantity);
