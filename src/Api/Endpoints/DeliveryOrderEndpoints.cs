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

namespace SnpDepotManagement.Api.Endpoints;

public static class DeliveryOrderEndpoints
{
    public static IEndpointRouteBuilder MapDeliveryOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delivery-orders").WithTags("DeliveryOrders").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        group.MapPost("", async (CreateDeliveryOrderCommand command, ICommandHandler<CreateDeliveryOrderCommand, Guid> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult(id => Results.Created($"/api/delivery-orders/{id}", new { id })))
            .WithName("CreateDeliveryOrder").RequireAuthorization(Policies.ManageDeliveryOrders);
        group.MapGet("", async (string? lineOperator, string? customerTaxCode, bool? isCompleted, PaginationRequest page,
                IQueryHandler<GetDeliveryOrdersQuery, PagedResult<DeliveryOrderDto>> handler, CancellationToken ct) =>
            (await handler.Handle(new(lineOperator, customerTaxCode, isCompleted, page.PageIndex, page.PageSize), ct)).ToHttpResult())
            .WithName("GetDeliveryOrders").RequireAuthorization(Policies.ReadDeliveryOrders);
        group.MapGet("/{id:guid}", async (Guid id, IQueryHandler<GetDeliveryOrderByIdQuery, DeliveryOrderDto> handler, CancellationToken ct) =>
            (await handler.Handle(new(id), ct)).ToHttpResult())
            .WithName("GetDeliveryOrderById").RequireAuthorization(Policies.ReadDeliveryOrders);
        group.MapGet("/by-number/{number}", async (string number, IQueryHandler<GetDeliveryOrderByOrderNumberQuery, DeliveryOrderDto> handler, CancellationToken ct) =>
            (await handler.Handle(new(number), ct)).ToHttpResult())
            .WithName("GetDeliveryOrderByOrderNumber").RequireAuthorization(Policies.ReadDeliveryOrders);
        return app;
    }
}
