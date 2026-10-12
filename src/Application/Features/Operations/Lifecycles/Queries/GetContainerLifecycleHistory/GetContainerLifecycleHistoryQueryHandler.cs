using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Operations.Lifecycles.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Queries.GetContainerLifecycleHistory;

public sealed class GetContainerLifecycleHistoryQueryHandler : IQueryHandler<GetContainerLifecycleHistoryQuery, List<ContainerLifecycleDto>>
{
    private readonly IAppDbContext _context;

    public GetContainerLifecycleHistoryQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<ContainerLifecycleDto>>> Handle(GetContainerLifecycleHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var lifecycles = await _context.ContainerLifecycles
            .AsNoTracking()
            .Where(l => l.ContainerId == query.ContainerId)
            .OrderByDescending(l => l.InDate)
            .ToListAsync(cancellationToken);

        var dtos = lifecycles.Select(l => new ContainerLifecycleDto(
            l.Id,
            l.ContainerId,
            l.LineOperator,
            l.Classification,
            l.Condition,
            l.Status,
            l.InVehicleNumber,
            l.InDate,
            l.OutVehicleNumber,
            l.OutDate,
            l.DeliveryOrderId,
            l.CurrentSlotId)).ToList();

        return Result.Success(dtos);
    }
}
