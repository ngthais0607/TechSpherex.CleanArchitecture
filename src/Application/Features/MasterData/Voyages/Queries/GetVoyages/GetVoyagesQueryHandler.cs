using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.Voyages.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.Voyages.Queries.GetVoyages;

public sealed class GetVoyagesQueryHandler : IQueryHandler<GetVoyagesQuery, PagedResult<VoyageDto>>
{
    private readonly IAppDbContext _context;

    public GetVoyagesQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<VoyageDto>>> Handle(GetVoyagesQuery query, CancellationToken cancellationToken = default)
    {
        var total = await _context.Voyages.CountAsync(cancellationToken);
        var list = await _context.Voyages
            .AsNoTracking()
            .OrderBy(v => v.Etd)
            .ThenBy(v => v.Id)
            .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize)
            .Select(v => new VoyageDto(v.Id, v.VesselName, v.VoyageNumber, v.Etd))
            .ToListAsync(cancellationToken);

        return Result.Success(PagedResult<VoyageDto>.Create(list, total, query.PageIndex, query.PageSize));
    }
}
