using SnpDepotManagement.Domain.Entities.Identity;

namespace SnpDepotManagement.Application.Abstractions.Identity;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user);
    string GenerateRefreshToken();
    int AccessTokenExpirationMinutes { get; }
    int RefreshTokenExpirationDays { get; }
}
