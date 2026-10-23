using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Yard.Queries.GetVirtualBlockContainers;

public sealed class GetVirtualBlockContainersQueryHandler : IQueryHandler<GetVirtualBlockContainersQuery, IReadOnlyList<VirtualBlockContainerDto>>
{
    private readonly IAppDbContext _context;

    public GetVirtualBlockContainersQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<VirtualBlockContainerDto>>> Handle(GetVirtualBlockContainersQuery query, CancellationToken cancellationToken = default)
    {
        var normCode = query.Code.Trim().ToUpperInvariant();

        var block = await _context.YardBlocks
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Code == normCode && b.IsVirtual, cancellationToken);

        if (block is null)
        {
            return Result.Failure<IReadOnlyList<VirtualBlockContainerDto>>(
                Error.NotFound("YardBlock.NotFound", $"Không tìm thấy dãy ảo '{query.Code}'."));
        }

        var lifecycles = await _context.ContainerLifecycles
            .AsNoTracking()
            .Where(l => l.VirtualBlockId == block.Id && l.Status == LifecycleStatus.InYard)
            .OrderByDescending(l => l.InDate)
            .ToListAsync(cancellationToken);

        if (lifecycles.Count == 0)
        {
            return Result.Success<IReadOnlyList<VirtualBlockContainerDto>>(Array.Empty<VirtualBlockContainerDto>());
        }

        var containerIds = lifecycles.Select(l => l.ContainerId).Distinct().ToList();
        var containers = await _context.Containers
            .AsNoTracking()
            .Where(c => containerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var result = new List<VirtualBlockContainerDto>();
        foreach (var lc in lifecycles)
        {
            if (containers.TryGetValue(lc.ContainerId, out var cont))
            {
                result.Add(new VirtualBlockContainerDto(
                    cont.Id,
                    cont.ContainerNumber.Value,
                    cont.IsoCode,
                    cont.Size.ToString(),
                    cont.Type.ToString(),
                    cont.Classification.ToString(),
                    cont.Condition.ToString(),
                    lc.InDate));
            }
        }

        return Result.Success<IReadOnlyList<VirtualBlockContainerDto>>(result);
    }
}
