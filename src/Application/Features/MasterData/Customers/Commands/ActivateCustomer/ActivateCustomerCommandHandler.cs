using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.MasterData.Customers.Commands.ActivateCustomer;

public sealed class ActivateCustomerCommandHandler : ICommandHandler<ActivateCustomerCommand>
{
    private readonly IAppDbContext _context;

    public ActivateCustomerCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ActivateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (customer is null)
        {
            return Result.Failure(Error.NotFound("Customer.NotFound", "Không tìm thấy khách hàng."));
        }

        customer.Activate();
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
