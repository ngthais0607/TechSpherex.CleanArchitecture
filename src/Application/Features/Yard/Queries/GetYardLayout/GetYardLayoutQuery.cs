using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Yard.Dtos;

namespace SnpDepotManagement.Application.Features.Yard.Queries.GetYardLayout;

public sealed record GetYardLayoutQuery(Guid BlockId) : IQuery<YardLayoutDto>;
