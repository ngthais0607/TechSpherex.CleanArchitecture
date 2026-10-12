using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Identity.Commands.Login;
using SnpDepotManagement.Application.Features.Identity.Commands.RefreshToken;
using SnpDepotManagement.Application.Features.Identity.Commands.Register;
using SnpDepotManagement.Application.Features.Identity.Dtos;

namespace SnpDepotManagement.Api.Endpoints;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/login", async (
            LoginCommand command,
            ICommandHandler<LoginCommand, AuthResponseDto> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Code, description = result.Error.Description });
        })
        .WithName("Login")
        .WithSummary("User login returning JWT and refresh tokens")
        .AllowAnonymous();

        group.MapPost("/refresh", async (
            RefreshTokenCommand command,
            ICommandHandler<RefreshTokenCommand, AuthResponseDto> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Code, description = result.Error.Description });
        })
        .WithName("RefreshToken")
        .WithSummary("Refresh expired JWT token using refresh token")
        .AllowAnonymous();

        group.MapPost("/register", async (
            RegisterCommand command,
            ICommandHandler<RegisterCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.IsSuccess
                ? Results.Ok(new { userId = result.Value })
                : Results.BadRequest(new { error = result.Error.Code, description = result.Error.Description });
        })
        .WithName("Register")
        .WithSummary("Register a new user in the system")
        .RequireAuthorization("Admin");

        return app;
    }
}
