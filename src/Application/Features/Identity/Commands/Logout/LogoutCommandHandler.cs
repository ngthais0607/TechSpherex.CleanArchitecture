using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Identity.Commands.Logout;

public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public LogoutCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(LogoutCommand command, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure(Error.NotFound("User.NotFound", "Không tìm thấy người dùng."));

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, ct);

        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", "Không tìm thấy người dùng."));

        user.RevokeRefreshToken();
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
