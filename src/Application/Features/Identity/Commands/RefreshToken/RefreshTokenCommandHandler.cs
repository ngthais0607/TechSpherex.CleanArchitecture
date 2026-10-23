using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Identity.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Identity.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IAppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenCommandHandler(
        IAppDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        TimeProvider timeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var userName = command.UserName.Trim();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthResponseDto>(
                Error.NotFound("User.NotFound", "User not found or is inactive."));
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (string.IsNullOrEmpty(user.RefreshTokenHash) || user.RefreshTokenExpiryTime is null || user.RefreshTokenExpiryTime <= now)
        {
            return Result.Failure<AuthResponseDto>(
                Error.Validation("Auth.RefreshTokenExpired", "Refresh token has expired or is invalid."));
        }

        var isTokenValid = _passwordHasher.VerifyPassword(command.RefreshToken, user.RefreshTokenHash);
        if (!isTokenValid)
        {
            return Result.Failure<AuthResponseDto>(
                Error.Validation("Auth.InvalidRefreshToken", "Invalid refresh token."));
        }

        // Token rotation
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRawRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _passwordHasher.HashPassword(newRawRefreshToken);

        var refreshDays = _tokenService.RefreshTokenExpirationDays > 0 ? _tokenService.RefreshTokenExpirationDays : 7;
        var accessMinutes = _tokenService.AccessTokenExpirationMinutes > 0 ? _tokenService.AccessTokenExpirationMinutes : 60;

        var expiresAt = _timeProvider.GetUtcNow().AddDays(refreshDays).UtcDateTime;
        var accessExpiresAt = _timeProvider.GetUtcNow().AddMinutes(accessMinutes).UtcDateTime;
        user.SetRefreshToken(newRefreshTokenHash, expiresAt);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthResponseDto(
            newAccessToken,
            newRawRefreshToken,
            user.UserName,
            user.Role,
            expiresAt,
            accessExpiresAt));
    }
}
