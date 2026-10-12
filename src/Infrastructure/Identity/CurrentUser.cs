using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SnpDepotManagement.Application.Abstractions.Identity;

namespace SnpDepotManagement.Infrastructure.Identity;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null)
            {
                return null;
            }

            var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst("sub")?.Value;

            if (Guid.TryParse(subClaim, out var userId))
            {
                return userId;
            }

            return null;
        }
    }

    public string? TenantId
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
        }
    }

    public string? Role
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Role)?.Value
                   ?? user?.FindFirst("role")?.Value;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
