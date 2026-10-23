using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Yard.Dtos;

namespace SnpDepotManagement.Application.Features.Yard.Queries.GetBlocks;

public sealed record GetBlocksQuery() : IQuery<IReadOnlyList<YardBlockDto>>;
