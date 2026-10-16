using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.Voyages.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.Voyages.Queries.GetVoyages;

public sealed record GetVoyagesQuery(int PageIndex = 1, int PageSize = 20) : IQuery<PagedResult<VoyageDto>>;
