using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.CreateLineOperator;

public sealed record CreateLineOperatorCommand(string Code, string Name) : ICommand<Guid>;
