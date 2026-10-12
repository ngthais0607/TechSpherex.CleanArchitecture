using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SnpDepotManagement.Application.Abstractions.Tenancy;

namespace SnpDepotManagement.Infrastructure.Tenancy;

public sealed class TenantProvider : ITenantProvider
{
    public const string TenantClaimType = "tenant_id";
    public const string TenantHeaderName = "X-Tenant-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public TenantInfo? GetCurrentTenant()
    {
        var tenantId = GetCurrentTenantId();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        return new TenantInfo(tenantId, tenantId);
    }

    public string? GetCurrentTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        // 1. Try get from user claims
        var claim = httpContext.User?.FindFirst(TenantClaimType);
        if (claim is not null && !string.IsNullOrWhiteSpace(claim.Value))
        {
            return claim.Value.Trim();
        }

        // 2. Try get from custom HTTP header
        if (httpContext.Request.Headers.TryGetValue(TenantHeaderName, out var headerValue))
        {
            var value = headerValue.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
