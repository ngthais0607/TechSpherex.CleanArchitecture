using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.MasterData;

namespace SnpDepotManagement.Application.Features.MasterData.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand, Guid>
{
    private readonly IAppDbContext _context;

    public CreateCustomerCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedTaxCode = command.TaxCode.Trim();

        var exists = await _context.Customers
            .AnyAsync(c => c.TaxCode == normalizedTaxCode, cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(
                Error.Conflict("Customer.AlreadyExists", $"Customer with tax code '{normalizedTaxCode}' already exists."));
        }

        var customerResult = Customer.Create(normalizedTaxCode, command.Name);
        if (customerResult.IsFailure)
        {
            return Result.Failure<Guid>(customerResult.Error);
        }

        var customer = customerResult.Value!;
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(customer.Id);
    }
}
