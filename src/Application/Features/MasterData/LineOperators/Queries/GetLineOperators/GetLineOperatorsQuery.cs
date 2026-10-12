using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Dtos;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Queries.GetLineOperators;

public sealed record GetLineOperatorsQuery : IQuery<IReadOnlyList<LineOperatorDto>>;
