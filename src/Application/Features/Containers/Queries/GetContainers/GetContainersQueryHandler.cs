using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Containers.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Containers.Queries.GetContainers;

public sealed class GetContainersQueryHandler : IQueryHandler<GetContainersQuery, PagedResult<ContainerDto>>
{
    private readonly IAppDbContext _context;

    public GetContainersQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<ContainerDto>>> Handle(GetContainersQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.Containers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.OwnerCode))
        {
            var normalizedOwner = query.OwnerCode.Trim().ToUpperInvariant();
            dbQuery = dbQuery.Where(c => c.OwnerCode == normalizedOwner);
        }

        if (query.Size.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.Size == query.Size.Value);
        }

        if (query.Condition.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.Condition == query.Condition.Value);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Max(1, Math.Min(100, query.PageSize));

        var items = await dbQuery
            .OrderBy(c => c.IsoCode)
            .ThenBy(c => c.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ContainerDto(
                c.Id,
                c.ContainerNumber.Value,
                c.IsoCode,
                c.Size,
                c.Type,
                c.TareWeight,
                c.MaxGrossWeight,
                c.OwnerCode,
                c.Condition,
                c.Classification,
                c.DateOfManufacture))
            .ToListAsync(cancellationToken);

        var pagedResult = PagedResult<ContainerDto>.Create(items, totalCount, pageIndex, pageSize);
        return Result.Success(pagedResult);
    }
}
