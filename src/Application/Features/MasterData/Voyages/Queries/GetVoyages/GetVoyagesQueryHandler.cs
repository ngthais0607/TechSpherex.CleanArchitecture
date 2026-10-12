using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.Voyages.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.Voyages.Queries.GetVoyages;

public sealed class GetVoyagesQueryHandler : IQueryHandler<GetVoyagesQuery, IReadOnlyList<VoyageDto>>
{
    private readonly IAppDbContext _context;

    public GetVoyagesQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<VoyageDto>>> Handle(GetVoyagesQuery query, CancellationToken cancellationToken = default)
    {
        var list = await _context.Voyages
            .AsNoTracking()
            .OrderBy(v => v.Etd)
            .Select(v => new VoyageDto(v.Id, v.VesselName, v.VoyageNumber, v.Etd))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<VoyageDto>>(list);
    }
}
