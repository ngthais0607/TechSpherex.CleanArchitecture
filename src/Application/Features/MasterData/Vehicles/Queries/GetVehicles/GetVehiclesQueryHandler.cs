using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.Vehicles.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.Vehicles.Queries.GetVehicles;

public sealed class GetVehiclesQueryHandler : IQueryHandler<GetVehiclesQuery, IReadOnlyList<VehicleDto>>
{
    private readonly IAppDbContext _context;

    public GetVehiclesQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<VehicleDto>>> Handle(GetVehiclesQuery query, CancellationToken cancellationToken = default)
    {
        var list = await _context.Vehicles
            .AsNoTracking()
            .OrderBy(v => v.PlateNumber)
            .Select(v => new VehicleDto(v.Id, v.PlateNumber, v.Type))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<VehicleDto>>(list);
    }
}
