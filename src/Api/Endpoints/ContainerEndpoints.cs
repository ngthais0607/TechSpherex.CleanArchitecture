using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SnpDepotManagement.Api.Authorization;
using SnpDepotManagement.Api.Contracts;
using SnpDepotManagement.Api.Extensions;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Containers.Commands.RegisterContainer;
using SnpDepotManagement.Application.Features.Containers.Commands.MarkContainerDamaged;
using SnpDepotManagement.Application.Features.Containers.Queries.GetContainerByNumber;
using SnpDepotManagement.Application.Features.Containers.Dtos;
using SnpDepotManagement.Application.Features.Containers.Queries.GetContainers;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Api.Endpoints;

public static class ContainerEndpoints
{
    public static IEndpointRouteBuilder MapContainerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/containers")
            .WithTags("Containers")
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter>();

        group.MapGet("/", async (
            string? ownerCode,
            ContainerSize? size,
            ContainerCondition? condition,
            PaginationRequest pagination,
            IQueryHandler<GetContainersQuery, PagedResult<ContainerDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetContainersQuery(
                OwnerCode: ownerCode,
                Size: size,
                Condition: condition,
                PageIndex: pagination.PageIndex,
                PageSize: pagination.PageSize);

            var result = await handler.Handle(query, cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Policies.ReadContainers)
        .WithName("GetContainers")
        .WithSummary("Retrieve paged list of containers in current depot");

        group.MapPost("/", async (
            RegisterContainerCommand command,
            ICommandHandler<RegisterContainerCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/api/containers/{id}", new { id }));
        })
        .RequireAuthorization(Policies.RegisterContainer)
        .WithName("RegisterContainer")
        .WithSummary("Register a new container into the system");

        group.MapGet("/by-number/{number}", async (string number, IQueryHandler<GetContainerByNumberQuery, ContainerDto> handler, CancellationToken ct) =>
            (await handler.Handle(new(number), ct)).ToHttpResult())
            .WithName("GetContainerByNumber").RequireAuthorization(Policies.ReadContainers);
        group.MapPost("/{id:guid}/damage", async (Guid id, ContainerDamageRequest body, ICommandHandler<MarkContainerDamagedCommand> handler, CancellationToken ct) =>
            (await handler.Handle(new(id, body.Condition), ct)).ToHttpResult())
            .WithName("MarkContainerDamaged").RequireAuthorization(Policies.ManageYard);
        return app;
    }
}
