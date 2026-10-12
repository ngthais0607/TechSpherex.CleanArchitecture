using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.Identity.Commands.Register;

public sealed record RegisterCommand(
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string Role) : ICommand<Guid>;
