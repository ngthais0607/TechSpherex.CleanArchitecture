using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.Customers.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.Customers.Queries.GetCustomers;

public sealed class GetCustomersQueryHandler : IQueryHandler<GetCustomersQuery, IReadOnlyList<CustomerDto>>
{
    private readonly IAppDbContext _context;

    public GetCustomersQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<CustomerDto>>> Handle(GetCustomersQuery query, CancellationToken cancellationToken = default)
    {
        var list = await _context.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CustomerDto(c.Id, c.TaxCode, c.Name))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CustomerDto>>(list);
    }
}
