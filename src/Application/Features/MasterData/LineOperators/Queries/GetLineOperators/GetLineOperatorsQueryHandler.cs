using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Queries.GetLineOperators;

public sealed class GetLineOperatorsQueryHandler : IQueryHandler<GetLineOperatorsQuery, PagedResult<LineOperatorDto>>
{
    private readonly IAppDbContext _context;

    public GetLineOperatorsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<LineOperatorDto>>> Handle(GetLineOperatorsQuery query, CancellationToken cancellationToken = default)
    {
        var total = await _context.LineOperators.CountAsync(cancellationToken);
        var list = await _context.LineOperators
            .AsNoTracking()
            .OrderBy(l => l.Code)
            .ThenBy(l => l.Id)
            .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize)
            .Select(l => new LineOperatorDto(l.Id, l.Code, l.Name))
            .ToListAsync(cancellationToken);

        return Result.Success(PagedResult<LineOperatorDto>.Create(list, total, query.PageIndex, query.PageSize));
    }
}
