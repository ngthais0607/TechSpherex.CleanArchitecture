using SnpDepotManagement.Api.Authorization;
using SnpDepotManagement.Api.Contracts;
using SnpDepotManagement.Api.Extensions;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.Customers.Commands.CreateCustomer;
using SnpDepotManagement.Application.Features.MasterData.Customers.Dtos;
using SnpDepotManagement.Application.Features.MasterData.Customers.Queries.GetCustomers;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.CreateLineOperator;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Dtos;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Queries.GetLineOperators;
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
        var operators = app.MapGroup("/api/line-operators").WithTags("LineOperators").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        operators.MapGet("", async (PaginationRequest page, IQueryHandler<GetLineOperatorsQuery, PagedResult<LineOperatorDto>> handler, CancellationToken ct) =>
            (await handler.Handle(new(page.PageIndex, page.PageSize), ct)).ToHttpResult())
            .WithName("GetLineOperators").RequireAuthorization(Policies.ReadMasterData);
        operators.MapPost("", async (CreateLineOperatorCommand command, ICommandHandler<CreateLineOperatorCommand, Guid> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult(id => Results.Created($"/api/line-operators/{id}", new { id })))
            .WithName("CreateLineOperator").RequireAuthorization(Policies.Admin);

        var customers = app.MapGroup("/api/customers").WithTags("Customers").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        customers.MapGet("", async (PaginationRequest page, IQueryHandler<GetCustomersQuery, PagedResult<CustomerDto>> handler, CancellationToken ct) =>
            (await handler.Handle(new(page.PageIndex, page.PageSize), ct)).ToHttpResult())
            .WithName("GetCustomers").RequireAuthorization(Policies.ReadMasterData);
        customers.MapPost("", async (CreateCustomerCommand command, ICommandHandler<CreateCustomerCommand, Guid> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult(id => Results.Created($"/api/customers/{id}", new { id })))
            .WithName("CreateCustomer").RequireAuthorization(Policies.Admin);

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
