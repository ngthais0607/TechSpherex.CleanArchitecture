using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.Identity;

public sealed class ApplicationUser : AuditableEntity
{
    public string UserName { get; private set; } = string.Empty;
    public string TenantId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string? RefreshTokenHash { get; private set; }
    public DateTime? RefreshTokenExpiryTime { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private ApplicationUser() { }

    public static Result<ApplicationUser> Create(
        string userName,
        string email,
        string firstName,
        string lastName,
        string tenantId,
        string role = "",
        string passwordHash = "")
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return Result.Failure<ApplicationUser>(
                Error.Validation("User.EmptyUserName", "Tên người dùng không được để trống."));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure<ApplicationUser>(
                Error.Validation("User.EmptyEmail", "Email không được để trống."));
        }

        if (string.IsNullOrWhiteSpace(tenantId) || tenantId.Trim().Length > 50)
        {
            return Result.Failure<ApplicationUser>(
                Error.Validation("User.InvalidTenant", "Depot được gán tối đa 50 ký tự và không được để trống."));
        }

        return Result.Success(new ApplicationUser
        {
            UserName = userName.Trim(),
            TenantId = tenantId.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Role = role.Trim(),
            PasswordHash = passwordHash,
            IsActive = true
        });
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void SetRole(string role)
    {
        Role = role.Trim();
    }

    public void AddRole(Guid roleId)
    {
        if (!_userRoles.Any(ur => ur.RoleId == roleId))
        {
            _userRoles.Add(new UserRole(Id, roleId));
        }
    }

    public void SetRefreshToken(string refreshTokenHash, DateTime expiryTime)
    {
        RefreshTokenHash = refreshTokenHash;
        RefreshTokenExpiryTime = expiryTime;
    }

    public void RevokeRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiryTime = null;
    }
}
