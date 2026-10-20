using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.ActivateLineOperator;

public sealed record ActivateLineOperatorCommand(Guid Id) : ICommand;
