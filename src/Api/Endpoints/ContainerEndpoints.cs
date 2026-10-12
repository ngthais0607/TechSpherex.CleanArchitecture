using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Containers.Commands.RegisterContainer;
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
            .RequireAuthorization();

        group.MapGet("/", async (
            string? ownerCode,
            ContainerSize? size,
            ContainerCondition? condition,
            int? pageIndex,
            int? pageSize,
            IQueryHandler<GetContainersQuery, PagedResult<ContainerDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetContainersQuery(
                OwnerCode: ownerCode,
                Size: size,
                Condition: condition,
                PageIndex: pageIndex ?? 1,
                PageSize: pageSize ?? 20);

            var result = await handler.Handle(query, cancellationToken);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Code, description = result.Error.Description });
        })
        .WithName("GetContainers")
        .WithSummary("Retrieve paged list of containers in current depot");

        group.MapPost("/", async (
            RegisterContainerCommand command,
            ICommandHandler<RegisterContainerCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/containers/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error.Code, description = result.Error.Description });
        })
        .WithName("RegisterContainer")
        .WithSummary("Register a new container into the system");

        return app;
    }
}
