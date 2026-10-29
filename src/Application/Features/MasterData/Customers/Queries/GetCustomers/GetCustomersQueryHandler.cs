using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.MasterData.Customers.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.Customers.Queries.GetCustomers;

public sealed class GetCustomersQueryHandler : IQueryHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    private readonly IAppDbContext _context;

    public GetCustomersQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<CustomerDto>>> Handle(GetCustomersQuery query, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var source = _context.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            source = source.Where(c => c.Name.ToUpper().Contains(search) || c.TaxCode.Contains(search));
        }
        var total = await source.CountAsync(cancellationToken);
        var customers = await source
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var taxCodes = customers.Select(c => c.TaxCode).Distinct().ToList();

        var orderCounts = await _context.DeliveryOrders
            .AsNoTracking()
            .Where(o => taxCodes.Contains(o.CustomerTaxCode))
            .GroupBy(o => o.CustomerTaxCode)
            .Select(g => new
            {
                TaxCode = g.Key,
                Total = g.Count(),
                Active = g.Count(o => !o.IsCompleted && o.ExpiryDate >= utcNow)
            })
            .ToDictionaryAsync(g => g.TaxCode, cancellationToken);

        var list = customers.Select(c =>
        {
            orderCounts.TryGetValue(c.TaxCode, out var stats);
            var totalOrders = stats?.Total ?? 0;
            var activeOrders = stats?.Active ?? 0;
            return new CustomerDto(c.Id, c.TaxCode, c.Name, c.IsActive, totalOrders, activeOrders);
        }).ToList();

        return Result.Success(PagedResult<CustomerDto>.Create(list, total, query.PageIndex, query.PageSize));
    }
}
