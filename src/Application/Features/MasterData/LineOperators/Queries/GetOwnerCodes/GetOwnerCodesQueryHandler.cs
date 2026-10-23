using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Queries.GetOwnerCodes;

public sealed class GetOwnerCodesQueryHandler : IQueryHandler<GetOwnerCodesQuery, IReadOnlyList<string>>
{
    private readonly IAppDbContext _context;

    public GetOwnerCodesQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(GetOwnerCodesQuery query, CancellationToken cancellationToken = default)
    {
        var lineOpExists = await _context.LineOperators
            .AnyAsync(l => l.Id == query.LineOperatorId, cancellationToken);

        if (!lineOpExists)
        {
            return Result.Failure<IReadOnlyList<string>>(Error.NotFound("LineOperator.NotFound", "Hãng khai thác không tồn tại."));
        }

        var codes = await _context.LineOperatorOwnerCodes
            .Where(x => x.LineOperatorId == query.LineOperatorId)
            .OrderBy(x => x.OwnerCode)
            .Select(x => x.OwnerCode)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<string>>(codes);
    }
}
