using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Containers.Queries.GetContainerSummary;

public sealed class GetContainerSummaryQueryHandler : IQueryHandler<GetContainerSummaryQuery, ContainerSummaryDto>
{
    private readonly IAppDbContext _context;

    public GetContainerSummaryQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ContainerSummaryDto>> Handle(GetContainerSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.Containers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.OwnerCode))
        {
            var normalizedOwner = query.OwnerCode.Trim().ToUpperInvariant();
            dbQuery = dbQuery.Where(c => c.OwnerCode == normalizedOwner);
        }

        var data = await dbQuery
            .GroupBy(c => new { c.Size, c.Type, c.Condition })
            .Select(g => new { g.Key.Size, g.Key.Type, g.Key.Condition, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var total = data.Sum(x => x.Count);
        var bySize = data.GroupBy(x => x.Size).ToDictionary(g => g.Key.ToString(), g => g.Sum(x => x.Count));
        var byType = data.GroupBy(x => x.Type).ToDictionary(g => g.Key.ToString(), g => g.Sum(x => x.Count));
        var byCondition = data.GroupBy(x => x.Condition).ToDictionary(g => g.Key.ToString(), g => g.Sum(x => x.Count));

        return Result.Success(new ContainerSummaryDto(
            Total: total,
            BySize: bySize,
            ByType: byType,
            ByCondition: byCondition
        ));
    }
}
