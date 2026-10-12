namespace SnpDepotManagement.Application.Abstractions.Identity;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? TenantId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
