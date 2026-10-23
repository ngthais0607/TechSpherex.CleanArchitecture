using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Identity.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<CurrentUserDto>;

public sealed record CurrentUserDto(
    string UserId,
    string UserName,
    string[] Roles,
    string TenantId,
    string DepotName);
