using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Identity.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetCurrentUserQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<CurrentUserDto>(Error.NotFound("User.NotFound", "Không tìm thấy người dùng."));

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, ct);

        if (user is null)
            return Result.Failure<CurrentUserDto>(Error.NotFound("User.NotFound", "Không tìm thấy người dùng."));

        var depotGuid = Guid.TryParse(_currentUser.TenantId, out var parsedGuid) ? parsedGuid : Guid.Empty;
        var depot = depotGuid != Guid.Empty
            ? await _context.Depots.FirstOrDefaultAsync(d => d.Id == depotGuid, ct)
            : await _context.Depots.FirstOrDefaultAsync(ct);

        return Result.Success(new CurrentUserDto(
            user.Id.ToString(),
            user.UserName,
            string.IsNullOrWhiteSpace(user.Role) ? [] : [user.Role],
            _currentUser.TenantId ?? string.Empty,
            depot?.Name ?? "Depot"));
    }
}
