using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Queries.GetLineOperators;

public sealed record GetLineOperatorsQuery(int PageIndex = 1, int PageSize = 20, string? Search = null) : IQuery<PagedResult<LineOperatorDto>>;
