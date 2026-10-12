using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Dtos;

namespace SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Commands.CreateDeliveryOrder;

public sealed record CreateDeliveryOrderCommand(
    string OrderNumber,
    string LineOperator,
    string CustomerTaxCode,
    string CustomerName,
    DateTime ExpiryDate,
    string OutboundVessel,
    List<CreateDeliveryOrderItemInput> Items) : ICommand<Guid>;
