using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Identity.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Identity.Commands.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResponseDto>
{
    private readonly IAppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public LoginCommandHandler(
        IAppDbContext _context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        TimeProvider timeProvider)
    {
        this._context = _context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == command.UserName, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthResponseDto>(
                Error.NotFound("User.NotFound", "User not found or is inactive."));
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(command.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return Result.Failure<AuthResponseDto>(
                Error.Validation("User.InvalidCredentials", "Invalid username or password."));
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _passwordHasher.HashPassword(rawRefreshToken);

        var refreshDays = _tokenService.RefreshTokenExpirationDays > 0 ? _tokenService.RefreshTokenExpirationDays : 7;
        var accessMinutes = _tokenService.AccessTokenExpirationMinutes > 0 ? _tokenService.AccessTokenExpirationMinutes : 60;

        var expiresAt = _timeProvider.GetUtcNow().AddDays(refreshDays).UtcDateTime;
        var accessExpiresAt = _timeProvider.GetUtcNow().AddMinutes(accessMinutes).UtcDateTime;
        user.SetRefreshToken(refreshTokenHash, expiresAt);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthResponseDto(
            accessToken,
            rawRefreshToken,
            user.UserName,
            user.Role,
            expiresAt,
            accessExpiresAt));
    }
}
