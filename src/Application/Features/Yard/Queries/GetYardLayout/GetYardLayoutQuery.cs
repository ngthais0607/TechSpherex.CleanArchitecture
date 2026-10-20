using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Yard.Dtos;

namespace SnpDepotManagement.Application.Features.Yard.Queries.GetYardLayout;

public sealed record GetYardLayoutQuery(
    Guid? BlockId = null,
    string? IdOrCode = null,
    int? Tier = null) : IQuery<YardLayoutDto>
{
    public GetYardLayoutQuery(Guid blockId) : this(blockId, null, null) { }
}
