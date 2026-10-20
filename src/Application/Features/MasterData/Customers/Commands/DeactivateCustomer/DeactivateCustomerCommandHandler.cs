using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.Customers.Commands.DeactivateCustomer;

public sealed class DeactivateCustomerCommandHandler : ICommandHandler<DeactivateCustomerCommand>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _timeProvider;

    public DeactivateCustomerCommandHandler(IAppDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(DeactivateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (customer is null)
        {
            return Result.Failure(Error.NotFound("Customer.NotFound", "Không tìm thấy khách hàng."));
        }

        // Chặn nếu còn lệnh hiệu lực -> 409
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var hasActiveOrders = await _context.DeliveryOrders
            .AnyAsync(o => o.CustomerTaxCode == customer.TaxCode && !o.IsCompleted && o.ExpiryDate >= utcNow, cancellationToken);

        if (hasActiveOrders)
        {
            return Result.Failure(Error.Conflict(
                "Customer.HasActiveOrders",
                "Không thể ngưng hoạt động khách hàng còn lệnh giao đang hiệu lực."));
        }

        customer.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
