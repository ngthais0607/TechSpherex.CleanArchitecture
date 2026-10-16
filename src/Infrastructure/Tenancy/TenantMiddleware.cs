using Microsoft.AspNetCore.Authorization;
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
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await Reject(context, 401, "Auth.Unauthorized", "Authentication is required.");
            return;
        }

        var tenantId = context.User.FindFirst(TenantProvider.TenantClaimType)?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(tenantId) || tenantId.Length > 50)
        {
            await Reject(context, 403, "Tenant.Invalid", "A valid depot assignment is required.");
            return;
        }

        if (context.Request.Headers.TryGetValue(TenantProvider.TenantHeaderName, out var header)
            && !string.Equals(header.ToString().Trim(), tenantId, StringComparison.Ordinal))
        {
            await Reject(context, 403, "Tenant.Mismatch", "The requested depot is not authorized.");
            return;
        }

        if (_tenantProvider.GetCurrentTenantId() != tenantId)
        {
            await Reject(context, 403, "Tenant.Invalid", "A valid depot assignment is required.");
            return;
        }

        await next(context);
    }

    private static Task Reject(HttpContext context, int status, string code, string detail) =>
        Results.Problem(statusCode: status, title: status == 401 ? "Unauthorized" : "Forbidden",
            detail: detail, extensions: new Dictionary<string, object?>
            { ["code"] = code, ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
}
