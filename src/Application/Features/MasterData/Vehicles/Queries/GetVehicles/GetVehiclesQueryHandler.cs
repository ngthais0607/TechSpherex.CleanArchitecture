using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.Vehicles.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.Vehicles.Queries.GetVehicles;

public sealed class GetVehiclesQueryHandler : IQueryHandler<GetVehiclesQuery, PagedResult<VehicleDto>>
{
    private readonly IAppDbContext _context;

    public GetVehiclesQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<VehicleDto>>> Handle(GetVehiclesQuery query, CancellationToken cancellationToken = default)
    {
        var total = await _context.Vehicles.CountAsync(cancellationToken);
        var list = await _context.Vehicles
            .AsNoTracking()
            .OrderBy(v => v.PlateNumber)
            .ThenBy(v => v.Id)
            .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize)
            .Select(v => new VehicleDto(v.Id, v.PlateNumber, v.Type))
            .ToListAsync(cancellationToken);

        return Result.Success(PagedResult<VehicleDto>.Create(list, total, query.PageIndex, query.PageSize));
    }
}
