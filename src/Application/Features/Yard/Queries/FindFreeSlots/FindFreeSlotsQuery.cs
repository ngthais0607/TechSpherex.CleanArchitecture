using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Yard.Dtos;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Yard.Queries.FindFreeSlots;

public sealed record FindFreeSlotsQuery(
    Guid BlockId,
    ContainerSize Size) : IQuery<List<YardPositionDto>>;
