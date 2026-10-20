using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.RemoveOwnerCode;

public sealed record RemoveOwnerCodeCommand(Guid LineOperatorId, string OwnerCode) : ICommand;
