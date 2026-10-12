using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Dtos;

namespace SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Queries.GetDeliveryOrderById;

public sealed record GetDeliveryOrderByIdQuery(Guid Id) : IQuery<DeliveryOrderDto>;
