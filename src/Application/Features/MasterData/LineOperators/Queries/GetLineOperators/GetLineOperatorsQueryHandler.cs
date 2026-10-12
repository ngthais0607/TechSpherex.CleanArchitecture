using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.LineOperators.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Queries.GetLineOperators;

public sealed class GetLineOperatorsQueryHandler : IQueryHandler<GetLineOperatorsQuery, IReadOnlyList<LineOperatorDto>>
{
    private readonly IAppDbContext _context;

    public GetLineOperatorsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<LineOperatorDto>>> Handle(GetLineOperatorsQuery query, CancellationToken cancellationToken = default)
    {
        var list = await _context.LineOperators
            .AsNoTracking()
            .OrderBy(l => l.Code)
            .Select(l => new LineOperatorDto(l.Id, l.Code, l.Name))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<LineOperatorDto>>(list);
    }
}
