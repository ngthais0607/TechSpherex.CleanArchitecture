using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Reports.Dtos;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetGateTraffic;

public sealed record GetGateTrafficQuery(int Days = 7) : IQuery<IReadOnlyList<DailyGateTrafficDto>>;
