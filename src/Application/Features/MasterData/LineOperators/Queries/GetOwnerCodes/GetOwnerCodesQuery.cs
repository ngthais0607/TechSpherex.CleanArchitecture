using SnpDepotManagement.Application.Abstractions.Messaging;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Queries.GetOwnerCodes;

public sealed record GetOwnerCodesQuery(Guid LineOperatorId) : IQuery<IReadOnlyList<string>>;
