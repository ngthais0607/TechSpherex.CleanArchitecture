using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Dtos;

namespace SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Queries.GetDeliveryOrderByOrderNumber;

public sealed record GetDeliveryOrderByOrderNumberQuery(string OrderNumber) : IQuery<DeliveryOrderDto>;
