using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.Identity;

public sealed class ApplicationUser : AuditableEntity
{
    public string UserName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string? RefreshTokenHash { get; private set; }
    public DateTime? RefreshTokenExpiryTime { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ApplicationUser() { }

    public static Result<ApplicationUser> Create(
        string userName,
        string email,
        string firstName,
        string lastName,
        string role = "",
        string passwordHash = "")
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return Result.Failure<ApplicationUser>(
                Error.Validation("User.EmptyUserName", "User name cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure<ApplicationUser>(
                Error.Validation("User.EmptyEmail", "Email cannot be empty."));
        }

        return Result.Success(new ApplicationUser
        {
            UserName = userName.Trim(),
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
