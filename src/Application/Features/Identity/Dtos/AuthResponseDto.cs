namespace SnpDepotManagement.Application.Features.Identity.Dtos;

public sealed record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    string UserName,
    string Role,
    DateTime ExpiresAt);
