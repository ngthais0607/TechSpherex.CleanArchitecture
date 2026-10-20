using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Reports.Dtos;

namespace SnpDepotManagement.Application.Features.Reports.Queries.GetBlockCapacity;

public sealed record GetBlockCapacityQuery : IQuery<IReadOnlyList<BlockCapacityDto>>;
