using SnpDepotManagement.Api.Authorization;
using SnpDepotManagement.Api.Contracts;
using SnpDepotManagement.Api.Extensions;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Yard.Commands.CreateYardBlock;
using SnpDepotManagement.Application.Features.Yard.Commands.ExpandYardBlock;
using SnpDepotManagement.Application.Features.Yard.Commands.PlaceContainer;
using SnpDepotManagement.Application.Features.Yard.Commands.PlaceFromVirtualBlock;
using SnpDepotManagement.Application.Features.Yard.Commands.RelocateContainer;
using SnpDepotManagement.Application.Features.Yard.Commands.RemoveContainer;
using SnpDepotManagement.Application.Features.Yard.Commands.ShrinkYardBlock;
using SnpDepotManagement.Application.Features.Yard.Dtos;
using SnpDepotManagement.Application.Features.Yard.Queries.FindFreeSlots;
using SnpDepotManagement.Application.Features.Yard.Queries.GetBlocks;
using SnpDepotManagement.Application.Features.Yard.Queries.GetVirtualBlockContainers;
using SnpDepotManagement.Application.Features.Yard.Queries.GetYardLayout;
using SnpDepotManagement.Application.Features.Yard.Queries.YardSearch;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Api.Endpoints;

public sealed record PlaceFromVirtualBlockRequest(string TargetBlockCode, int Bay, int Row, int Tier);

public static class YardEndpoints
{
    public static IEndpointRouteBuilder MapYardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/yard").WithTags("Yard").RequireAuthorization().AddEndpointFilter<ValidationFilter>();

        group.MapGet("/blocks", async (IQueryHandler<GetBlocksQuery, IReadOnlyList<YardBlockDto>> handler, CancellationToken ct) =>
            (await handler.Handle(new(), ct)).ToHttpResult())
            .WithName("GetYardBlocks").RequireAuthorization(Policies.ReadYard);

        group.MapPost("/blocks", async (CreateYardBlockCommand command, ICommandHandler<CreateYardBlockCommand, Guid> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult(id => Results.Created($"/api/yard/blocks/{id}/layout", new { id })))
            .WithName("CreateYardBlock").RequireAuthorization(Policies.ManageYard);

        group.MapPost("/blocks/{id:guid}/expand", async (Guid id, YardBlockSizeRequest body, ICommandHandler<ExpandYardBlockCommand> handler, CancellationToken ct) =>
            (await handler.Handle(new(id, body.NewBays, body.NewRows, body.NewTiers), ct)).ToHttpResult())
            .WithName("ExpandYardBlock").RequireAuthorization(Policies.ManageYard);

        group.MapPost("/blocks/{id:guid}/shrink", async (Guid id, YardBlockSizeRequest body, ICommandHandler<ShrinkYardBlockCommand> handler, CancellationToken ct) =>
            (await handler.Handle(new(id, body.NewBays, body.NewRows, body.NewTiers), ct)).ToHttpResult())
            .WithName("ShrinkYardBlock").RequireAuthorization(Policies.ManageYard);

        group.MapGet("/blocks/{idOrCode}/layout", async (string idOrCode, int? tier, IQueryHandler<GetYardLayoutQuery, YardLayoutDto> handler, CancellationToken ct) =>
            (await handler.Handle(new(IdOrCode: idOrCode, Tier: tier), ct)).ToHttpResult())
            .WithName("GetYardLayout").RequireAuthorization(Policies.ReadYard);

        group.MapGet("/search", async (string containerNumber, IQueryHandler<YardSearchQuery, YardSearchResult> handler, CancellationToken ct) =>
            (await handler.Handle(new(containerNumber), ct)).ToHttpResult())
            .WithName("YardSearch").RequireAuthorization(Policies.ReadYard);

        group.MapGet("/virtual-blocks/{code}/containers", async (string code, IQueryHandler<GetVirtualBlockContainersQuery, IReadOnlyList<VirtualBlockContainerDto>> handler, CancellationToken ct) =>
            (await handler.Handle(new(code), ct)).ToHttpResult())
            .WithName("GetVirtualBlockContainers").RequireAuthorization(Policies.ReadYard);

        group.MapPost("/virtual-blocks/{code}/containers/{id:guid}/place", async (string code, Guid id, PlaceFromVirtualBlockRequest body, ICommandHandler<PlaceFromVirtualBlockCommand> handler, CancellationToken ct) =>
            (await handler.Handle(new(code, id, body.TargetBlockCode, body.Bay, body.Row, body.Tier), ct)).ToHttpResult())
            .WithName("PlaceFromVirtualBlock").RequireAuthorization(Policies.ManageYard);

        group.MapGet("/free-slots", async (Guid blockId, ContainerSize size, PaginationRequest page, IQueryHandler<FindFreeSlotsQuery, PagedResult<YardPositionDto>> handler, CancellationToken ct) =>
            (await handler.Handle(new(blockId, size, page.PageIndex, page.PageSize), ct)).ToHttpResult())
            .WithName("FindFreeSlots").RequireAuthorization(Policies.ReadYard);

        group.MapPost("/placements", async (PlaceContainerCommand command, ICommandHandler<PlaceContainerCommand> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult())
            .WithName("PlaceContainer").RequireAuthorization(Policies.ManageYard);

        group.MapPost("/removals", async (RemoveContainerCommand command, ICommandHandler<RemoveContainerCommand> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult())
            .WithName("RemoveContainer").RequireAuthorization(Policies.ManageYard);

        group.MapPost("/relocations", async (RelocateContainerCommand command, ICommandHandler<RelocateContainerCommand> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult())
            .WithName("RelocateContainer").RequireAuthorization(Policies.ManageYard);

        return app;
    }
}
