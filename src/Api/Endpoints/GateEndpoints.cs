using SnpDepotManagement.Api.Authorization;
using SnpDepotManagement.Api.Extensions;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;

namespace SnpDepotManagement.Api.Endpoints;

public static class GateEndpoints
{
    public static IEndpointRouteBuilder MapGateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gate").WithTags("Gate").RequireAuthorization(Policies.ManageGate).AddEndpointFilter<ValidationFilter>();
        group.MapPost("/in", async (GateInCommand command, ICommandHandler<GateInCommand, Guid> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult(id => Results.Json(new { id }, statusCode: 201)))
            .WithName("GateInContainer");
        group.MapPost("/out", async (GateOutCommand command, ICommandHandler<GateOutCommand> handler, CancellationToken ct) =>
            (await handler.Handle(command, ct)).ToHttpResult())
            .WithName("GateOutContainer");
        return app;
    }
}
