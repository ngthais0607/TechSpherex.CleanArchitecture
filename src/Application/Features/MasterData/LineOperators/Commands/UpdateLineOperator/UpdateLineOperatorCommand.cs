using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.UpdateLineOperator;

public sealed record UpdateLineOperatorCommand(Guid Id, string Name) : ICommand;
