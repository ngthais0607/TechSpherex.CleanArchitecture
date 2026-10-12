using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Commands.CreateDeliveryOrder;

public sealed class CreateDeliveryOrderCommandHandler : ICommandHandler<CreateDeliveryOrderCommand, Guid>
{
    private readonly IAppDbContext _context;

    public CreateDeliveryOrderCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateDeliveryOrderCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = command.OrderNumber.Trim().ToUpperInvariant();

        var exists = await _context.DeliveryOrders
            .AnyAsync(o => o.OrderNumber == normalizedNumber, cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(
                Error.Conflict("DeliveryOrder.AlreadyExists", $"Delivery order with number '{normalizedNumber}' already exists."));
        }

        var orderResult = DeliveryOrder.Create(
            command.OrderNumber,
            command.LineOperator,
            command.CustomerTaxCode,
            command.CustomerName,
            command.ExpiryDate,
            command.OutboundVessel);

        if (orderResult.IsFailure)
        {
            return Result.Failure<Guid>(orderResult.Error);
        }

        var order = orderResult.Value!;

        foreach (var itemInput in command.Items)
        {
            var addResult = order.AddItem(itemInput.Size, itemInput.Type, itemInput.Quantity);
            if (addResult.IsFailure)
            {
                return Result.Failure<Guid>(addResult.Error);
            }
        }

        _context.DeliveryOrders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(order.Id);
    }
}
