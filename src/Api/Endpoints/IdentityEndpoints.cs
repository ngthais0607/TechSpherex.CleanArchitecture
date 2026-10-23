using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SnpDepotManagement.Api.Authorization;
using SnpDepotManagement.Api.Extensions;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Identity.Commands.Login;
using SnpDepotManagement.Application.Features.Identity.Commands.Logout;
using SnpDepotManagement.Application.Features.Identity.Commands.RefreshToken;
using SnpDepotManagement.Application.Features.Identity.Commands.Register;
using SnpDepotManagement.Application.Features.Identity.Dtos;
using SnpDepotManagement.Application.Features.Identity.Queries.GetCurrentUser;

namespace SnpDepotManagement.Api.Endpoints;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AddEndpointFilter<ValidationFilter>();

        group.MapPost("/login", async (
            LoginCommand command,
            ICommandHandler<LoginCommand, AuthResponseDto> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("Login")
        .WithSummary("User login returning JWT and refresh tokens")
        .RequireRateLimiting("login")
        .AllowAnonymous();

        group.MapPost("/refresh", async (
            RefreshTokenCommand command,
            ICommandHandler<RefreshTokenCommand, AuthResponseDto> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            return result.ToHttpResult();
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
            return result.ToHttpResult(id => Results.Json(new { userId = id }, statusCode: StatusCodes.Status201Created));
        })
        .WithName("Register")
        .WithSummary("Register a new user in the system")
        .RequireAuthorization(Policies.Admin);

        group.MapGet("/me", async (
            IQueryHandler<GetCurrentUserQuery, CurrentUserDto> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetCurrentUserQuery(), ct);
            return result.ToHttpResult();
        })
        .WithName("GetCurrentUser")
        .WithSummary("Trả thông tin người dùng đang đăng nhập")
        .RequireAuthorization(Policies.AuthenticatedUser);

        group.MapPost("/logout", async (
            ICommandHandler<LogoutCommand> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new LogoutCommand(), ct);
            return result.ToHttpResult();
        })
        .WithName("Logout")
        .WithSummary("Đăng xuất, thu hồi refresh token")
        .RequireAuthorization(Policies.AuthenticatedUser);

        return app;
    }
}
