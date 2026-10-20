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
        var total = await _context.Customers.CountAsync(cancellationToken);
        var customers = await _context.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var taxCodes = customers.Select(c => c.TaxCode).Distinct().ToList();

        var orders = await _context.DeliveryOrders
            .AsNoTracking()
            .Where(o => taxCodes.Contains(o.CustomerTaxCode))
            .Select(o => new { o.CustomerTaxCode, o.IsCompleted, o.ExpiryDate })
            .ToListAsync(cancellationToken);

        var list = customers.Select(c =>
        {
            var cOrders = orders.Where(o => o.CustomerTaxCode == c.TaxCode).ToList();
            var totalOrders = cOrders.Count;
            var activeOrders = cOrders.Count(o => !o.IsCompleted && o.ExpiryDate >= utcNow);
            return new CustomerDto(c.Id, c.TaxCode, c.Name, c.IsActive, totalOrders, activeOrders);
        }).ToList();

        return Result.Success(PagedResult<CustomerDto>.Create(list, total, query.PageIndex, query.PageSize));
    }
}
