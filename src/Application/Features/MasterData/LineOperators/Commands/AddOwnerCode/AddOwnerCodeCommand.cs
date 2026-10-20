using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.AddOwnerCode;

public sealed record AddOwnerCodeCommand(Guid LineOperatorId, string OwnerCode) : ICommand;
