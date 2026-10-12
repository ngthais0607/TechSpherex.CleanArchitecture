using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Identity.Dtos;

namespace SnpDepotManagement.Application.Features.Identity.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string UserName, string RefreshToken) : ICommand<AuthResponseDto>;
