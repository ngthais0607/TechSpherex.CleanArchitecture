using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.Voyages.Dtos;

namespace SnpDepotManagement.Application.Features.MasterData.Voyages.Queries.GetVoyages;

public sealed record GetVoyagesQuery : IQuery<IReadOnlyList<VoyageDto>>;
