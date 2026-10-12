using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Identity.Dtos;

namespace SnpDepotManagement.Application.Features.Identity.Commands.Login;

public sealed record LoginCommand(string UserName, string Password) : ICommand<AuthResponseDto>;
