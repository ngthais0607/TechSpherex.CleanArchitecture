using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Common;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Identity;

namespace SnpDepotManagement.Application.Features.Identity.Commands.Register;

public sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand, Guid>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(
        IAppDbContext context,
        ICurrentUser currentUser,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        // Only Admin can create users
        if (_currentUser.Role != Roles.Admin)
        {
            return Result.Failure<Guid>(
                Error.Conflict("Auth.Forbidden", "Only Admin can register new users."));
        }

        if (string.IsNullOrWhiteSpace(_currentUser.TenantId))
            return Result.Failure<Guid>(Error.Validation("User.InvalidTenant", "A depot assignment is required."));

        var normalizedUserName = command.UserName.Trim();
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        var exists = await _context.Users
            .AnyAsync(u => u.UserName == normalizedUserName || u.Email == normalizedEmail, cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(
                Error.Conflict("User.AlreadyExists", "Username or email is already registered."));
        }

        var passwordHash = _passwordHasher.HashPassword(command.Password);

        var userResult = ApplicationUser.Create(
            normalizedUserName,
            normalizedEmail,
            command.FirstName.Trim(),
            command.LastName.Trim(),
            _currentUser.TenantId,
            command.Role.Trim(),
            passwordHash);

        if (userResult.IsFailure)
        {
            return Result.Failure<Guid>(userResult.Error);
        }

        var user = userResult.Value!;
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Id);
    }
}
