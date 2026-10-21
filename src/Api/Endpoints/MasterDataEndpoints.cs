using SnpDepotManagement.Api.Authorization;
using SnpDepotManagement.Api.Contracts;
using SnpDepotManagement.Api.Extensions;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Lookups.Dtos;
using SnpDepotManagement.Application.Features.Lookups.Queries.GetLookups;
using SnpDepotManagement.Application.Features.MasterData.Customers.Commands.ActivateCustomer;
using SnpDepotManagement.Application.Features.MasterData.Customers.Commands.CreateCustomer;
using SnpDepotManagement.Application.Features.MasterData.Customers.Commands.DeactivateCustomer;
using SnpDepotManagement.Application.Features.MasterData.Customers.Commands.UpdateCustomer;
using SnpDepotManagement.Application.Features.MasterData.Customers.Dtos;
using SnpDepotManagement.Application.Features.MasterData.Customers.Queries.GetCustomers;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.ActivateLineOperator;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.AddOwnerCode;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.CreateLineOperator;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.DeactivateLineOperator;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.RemoveOwnerCode;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.UpdateLineOperator;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Dtos;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Queries.GetLineOperators;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Queries.GetOwnerCodes;
using SnpDepotManagement.Application.Features.MasterData.Vehicles.Commands.CreateVehicle;
using SnpDepotManagement.Application.Features.MasterData.Vehicles.Dtos;
using SnpDepotManagement.Application.Features.MasterData.Vehicles.Queries.GetVehicles;
using SnpDepotManagement.Application.Features.MasterData.Voyages.Commands.CreateVoyage;
using SnpDepotManagement.Application.Features.MasterData.Voyages.Dtos;
using SnpDepotManagement.Application.Features.MasterData.Voyages.Queries.GetVoyages;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Api.Endpoints;

public static class MasterDataEndpoints
{
    public static IEndpointRouteBuilder MapMasterDataEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/lookups
        app.MapGet("/api/lookups", async (IQueryHandler<GetLookupsQuery, LookupsDto> handler, CancellationToken ct) =>
            (await handler.Handle(new(), ct)).ToHttpResult())
            .WithName("GetLookups")
            .WithTags("Lookups")
            .RequireAuthorization(Policies.ReadMasterData);

        // Helper to register line operator endpoints on a group
        void RegisterLineOperatorEndpoints(RouteGroupBuilder group, string suffix)
        {
            group.MapGet("", async (PaginationRequest page, string? search, IQueryHandler<GetLineOperatorsQuery, PagedResult<LineOperatorDto>> handler, CancellationToken ct) =>
                (await handler.Handle(new(page.PageIndex, page.PageSize, search), ct)).ToHttpResult())
                .WithName($"GetLineOperators{suffix}").RequireAuthorization(Policies.ReadMasterData);

            group.MapPost("", async (CreateLineOperatorCommand command, ICommandHandler<CreateLineOperatorCommand, Guid> handler, CancellationToken ct) =>
                (await handler.Handle(command, ct)).ToHttpResult(id => Results.Created($"/api/line-operators/{id}", new { id })))
                .WithName($"CreateLineOperator{suffix}").RequireAuthorization(Policies.Admin);

            group.MapPut("{id:guid}", async (Guid id, UpdateLineOperatorRequest request, ICommandHandler<UpdateLineOperatorCommand> handler, CancellationToken ct) =>
                (await handler.Handle(new(id, request.Name), ct)).ToHttpResult())
                .WithName($"UpdateLineOperator{suffix}").RequireAuthorization(Policies.Admin);

            group.MapPost("{id:guid}/deactivate", async (Guid id, ICommandHandler<DeactivateLineOperatorCommand> handler, CancellationToken ct) =>
                (await handler.Handle(new(id), ct)).ToHttpResult())
                .WithName($"DeactivateLineOperator{suffix}").RequireAuthorization(Policies.Admin);

            group.MapPost("{id:guid}/activate", async (Guid id, ICommandHandler<ActivateLineOperatorCommand> handler, CancellationToken ct) =>
                (await handler.Handle(new(id), ct)).ToHttpResult())
                .WithName($"ActivateLineOperator{suffix}").RequireAuthorization(Policies.Admin);

            group.MapGet("{id:guid}/owner-codes", async (Guid id, IQueryHandler<GetOwnerCodesQuery, IReadOnlyList<string>> handler, CancellationToken ct) =>
                (await handler.Handle(new(id), ct)).ToHttpResult())
                .WithName($"GetLineOperatorOwnerCodes{suffix}").RequireAuthorization(Policies.ReadMasterData);

            group.MapPost("{id:guid}/owner-codes", async (Guid id, AddOwnerCodeRequest request, ICommandHandler<AddOwnerCodeCommand> handler, CancellationToken ct) =>
                (await handler.Handle(new(id, request.OwnerCode), ct)).ToHttpResult())
                .WithName($"AddLineOperatorOwnerCode{suffix}").RequireAuthorization(Policies.Admin);

            group.MapDelete("{id:guid}/owner-codes/{ownerCode}", async (Guid id, string ownerCode, ICommandHandler<RemoveOwnerCodeCommand> handler, CancellationToken ct) =>
                (await handler.Handle(new(id, ownerCode), ct)).ToHttpResult())
                .WithName($"RemoveLineOperatorOwnerCode{suffix}").RequireAuthorization(Policies.Admin);
        }

        // Helper to register customer endpoints on a group
        void RegisterCustomerEndpoints(RouteGroupBuilder group, string suffix)
        {
            group.MapGet("", async (PaginationRequest page, string? search, IQueryHandler<GetCustomersQuery, PagedResult<CustomerDto>> handler, CancellationToken ct) =>
                (await handler.Handle(new(page.PageIndex, page.PageSize, search), ct)).ToHttpResult())
                .WithName($"GetCustomers{suffix}").RequireAuthorization(Policies.ReadMasterData);

            group.MapPost("", async (CreateCustomerCommand command, ICommandHandler<CreateCustomerCommand, Guid> handler, CancellationToken ct) =>
                (await handler.Handle(command, ct)).ToHttpResult(id => Results.Created($"/api/customers/{id}", new { id })))
                .WithName($"CreateCustomer{suffix}").RequireAuthorization(Policies.Admin);

            group.MapPut("{id:guid}", async (Guid id, UpdateCustomerRequest request, ICommandHandler<UpdateCustomerCommand> handler, CancellationToken ct) =>
                (await handler.Handle(new(id, request.Name), ct)).ToHttpResult())
                .WithName($"UpdateCustomer{suffix}").RequireAuthorization(Policies.Admin);

            group.MapPost("{id:guid}/deactivate", async (Guid id, ICommandHandler<DeactivateCustomerCommand> handler, CancellationToken ct) =>
                (await handler.Handle(new(id), ct)).ToHttpResult())
                .WithName($"DeactivateCustomer{suffix}").RequireAuthorization(Policies.Admin);

            group.MapPost("{id:guid}/activate", async (Guid id, ICommandHandler<ActivateCustomerCommand> handler, CancellationToken ct) =>
                (await handler.Handle(new(id), ct)).ToHttpResult())
                .WithName($"ActivateCustomer{suffix}").RequireAuthorization(Policies.Admin);
        }

        var operators = app.MapGroup("/api/line-operators").WithTags("LineOperators").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        RegisterLineOperatorEndpoints(operators, "");

        var mdOperators = app.MapGroup("/api/master-data/line-operators").WithTags("LineOperators")
            .WithMetadata(new DeprecatedEndpointMetadata("/api/line-operators")).RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        RegisterLineOperatorEndpoints(mdOperators, "Md");

        var customers = app.MapGroup("/api/customers").WithTags("Customers").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        RegisterCustomerEndpoints(customers, "");

        var mdCustomers = app.MapGroup("/api/master-data/customers").WithTags("Customers")
            .WithMetadata(new DeprecatedEndpointMetadata("/api/customers")).RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        RegisterCustomerEndpoints(mdCustomers, "Md");

        var voyages = app.MapGroup("/api/voyages").WithTags("Voyages").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        voyages.MapGet("", async (PaginationRequest page, IQueryHandler<GetVoyagesQuery, PagedResult<VoyageDto>> handler, CancellationToken ct) =>
            (await handler.Handle(new(page.PageIndex, page.PageSize), ct)).ToHttpResult())
            .WithName("GetVoyages").RequireAuthorization(Policies.ReadMasterData);
        voyages.MapPost("", async (CreateVoyageCommand command, ICommandHandler<CreateVoyageCommand, Guid> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult(id => Results.Created($"/api/voyages/{id}", new { id })))
            .WithName("CreateVoyage").RequireAuthorization(Policies.Admin);

        var vehicles = app.MapGroup("/api/vehicles").WithTags("Vehicles").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        vehicles.MapGet("", async (PaginationRequest page, IQueryHandler<GetVehiclesQuery, PagedResult<VehicleDto>> handler, CancellationToken ct) =>
            (await handler.Handle(new(page.PageIndex, page.PageSize), ct)).ToHttpResult())
            .WithName("GetVehicles").RequireAuthorization(Policies.ReadMasterData);
        vehicles.MapPost("", async (CreateVehicleCommand command, ICommandHandler<CreateVehicleCommand, Guid> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult(id => Results.Created($"/api/vehicles/{id}", new { id })))
            .WithName("CreateVehicle").RequireAuthorization(Policies.Admin);

        return app;
    }
}
