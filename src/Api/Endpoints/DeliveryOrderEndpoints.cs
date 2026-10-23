using SnpDepotManagement.Api.Authorization;
using SnpDepotManagement.Api.Contracts;
using SnpDepotManagement.Api.Extensions;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Commands.CreateDeliveryOrder;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Queries.GetDeliveryOrders;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Queries.GetDeliveryOrderById;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Queries.GetDeliveryOrderByOrderNumber;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Dtos;
using SnpDepotManagement.Domain.Common;

using SnpDepotManagement.Application.Features.Operations.Bookings.Commands.AssignContainers;
using SnpDepotManagement.Application.Features.Operations.Bookings.Commands.ReleaseBooking;
using SnpDepotManagement.Application.Features.Operations.Bookings.Queries.GetOrderBookings;

namespace SnpDepotManagement.Api.Endpoints;

public static class DeliveryOrderEndpoints
{
    public static IEndpointRouteBuilder MapDeliveryOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delivery-orders").WithTags("DeliveryOrders").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        group.MapPost("", async (CreateDeliveryOrderCommand command, ICommandHandler<CreateDeliveryOrderCommand, Guid> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult(id => Results.Created($"/api/delivery-orders/{id}", new { id })))
            .WithName("CreateDeliveryOrder").RequireAuthorization(Policies.ManageDeliveryOrders);
        group.MapGet("", async (string? lineOperator, string? customerTaxCode, bool? isCompleted, string? status,
                string? search, string? sort, bool? descending, PaginationRequest page,
                IQueryHandler<GetDeliveryOrdersQuery, PagedResult<DeliveryOrderDto>> handler, CancellationToken ct) =>
            (await handler.Handle(new(lineOperator, customerTaxCode, isCompleted, status, page.PageIndex, page.PageSize,
                search, sort, descending ?? true), ct)).ToHttpResult())
            .WithName("GetDeliveryOrders").RequireAuthorization(Policies.ReadDeliveryOrders);
        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetDeliveryOrderByIdQuery, DeliveryOrderDto> handler, CancellationToken ct) =>
            (await handler.Handle(new(id), ct)).ToHttpResult())
            .WithName("GetDeliveryOrderById").RequireAuthorization(Policies.ReadDeliveryOrders);
        group.MapGet("/by-number/{number}", async (string number, IQueryHandler<GetDeliveryOrderByOrderNumberQuery, DeliveryOrderDto> handler, CancellationToken ct) =>
            (await handler.Handle(new(number), ct)).ToHttpResult())
            .WithName("GetDeliveryOrderByOrderNumber").RequireAuthorization(Policies.ReadDeliveryOrders);
        group.MapGet("/{orderNumber}/bookings", async (string orderNumber, PaginationRequest page,
                IQueryHandler<GetOrderBookingsQuery, OrderBookingsDto> handler, CancellationToken ct) =>
            (await handler.Handle(new(orderNumber, page.PageIndex, page.PageSize), ct)).ToHttpResult())
            .WithName("GetOrderBookings").RequireAuthorization(Policies.ReadDeliveryOrders);
        group.MapPost("/{orderNumber}/assign-containers", async (string orderNumber, AssignContainersRequest request,
                ICommandHandler<AssignContainersToOrderCommand, AssignContainersResult> handler, CancellationToken ct) =>
            (await handler.Handle(new(orderNumber, request.ContainerNumbers), ct)).ToHttpResult())
            .WithName("AssignContainersToOrder").RequireAuthorization(Policies.ManageDeliveryOrders);
        group.MapPost("/{orderNumber}/assign", async (string orderNumber, AssignContainersRequest request,
                ICommandHandler<AssignContainersToOrderCommand, AssignContainersResult> handler, CancellationToken ct) =>
            (await handler.Handle(new(orderNumber, request.ContainerNumbers), ct)).ToHttpResult())
            .WithName("AssignContainersToOrderShort")
            .WithMetadata(new DeprecatedEndpointMetadata("/api/delivery-orders/{orderNumber}/assign-containers"))
            .RequireAuthorization(Policies.ManageDeliveryOrders);
        group.MapDelete("/bookings/{id:guid}", async (Guid id, ICommandHandler<ReleaseBookingCommand> handler, CancellationToken ct) =>
            (await handler.Handle(new(id), ct)).ToHttpResult())
            .WithName("ReleaseBooking").RequireAuthorization(Policies.ManageDeliveryOrders);
        return app;
    }
}
