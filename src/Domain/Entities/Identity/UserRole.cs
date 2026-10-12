using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.Identity;

public sealed class UserRole : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    private UserRole() { }

    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}
