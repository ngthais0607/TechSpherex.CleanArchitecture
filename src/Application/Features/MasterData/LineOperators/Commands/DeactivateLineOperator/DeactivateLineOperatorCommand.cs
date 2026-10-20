using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.DeactivateLineOperator;

public sealed record DeactivateLineOperatorCommand(Guid Id) : ICommand;
