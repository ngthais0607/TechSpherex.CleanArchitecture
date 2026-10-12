using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SnpDepotManagement.Application.Abstractions.Tenancy;

namespace SnpDepotManagement.Infrastructure.Tenancy;

public sealed class TenantMiddleware : IMiddleware
{
    private readonly ITenantProvider _tenantProvider;

    public TenantMiddleware(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip tenant check for anonymous endpoints, documentation and healthchecks
        if (IsWhitelistedPath(path))
        {
            await next(context);
            return;
        }

        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var response = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "Tenant identifier is missing. Provide 'X-Tenant-Id' header or authentic tenant claim."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            return;
        }

        await next(context);
    }

    private static bool IsWhitelistedPath(string path)
    {
        return path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
               path.Equals("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase);
    }
}
