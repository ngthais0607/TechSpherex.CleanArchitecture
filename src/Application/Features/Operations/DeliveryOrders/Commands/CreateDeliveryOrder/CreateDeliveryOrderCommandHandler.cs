using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Commands.CreateDeliveryOrder;

public sealed class CreateDeliveryOrderCommandHandler : ICommandHandler<CreateDeliveryOrderCommand, Guid>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _timeProvider;

    public CreateDeliveryOrderCommandHandler(IAppDbContext context, TimeProvider? timeProvider = null)
    {
        _context = context;
        _timeProvider = timeProvider ?? TimeProvider.System;
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

        // Kiểm tra LineOperator tồn tại
        var lineOp = await _context.LineOperators
            .FirstOrDefaultAsync(l => l.Code == command.LineOperator, cancellationToken);
        if (lineOp is null)
        {
            return Result.Failure<Guid>(Error.Validation("OrderRule.UnknownLineOperator", "Hãng khai thác không tồn tại trong danh mục."));
        }

        // Kiểm tra Customer tồn tại, lấy tên từ DB
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.TaxCode == command.CustomerTaxCode, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<Guid>(Error.Validation("OrderRule.UnknownCustomer", "Khách hàng không tồn tại trong danh mục."));
        }

        // Hạn lệnh không ở quá khứ
        var today = DepotCalendar.GetBusinessDate(_timeProvider.GetUtcNow().UtcDateTime);
        if (DateOnly.FromDateTime(command.ExpiryDate) < today)
        {
            return Result.Failure<Guid>(Error.Validation("OrderRule.ExpiryInPast", "Hạn lệnh giao không được ở quá khứ."));
        }

        // Chặn dòng trùng: kiểm tra items trong command
        var duplicates = command.Items
            .GroupBy(i => (i.Size, i.Type))
            .Where(g => g.Count() > 1)
            .ToList();
        if (duplicates.Any())
        {
            return Result.Failure<Guid>(Error.Validation("OrderRule.DuplicateLine", "Lệnh giao có dòng trùng kích thước và loại container."));
        }

        var orderResult = DeliveryOrder.Create(
            command.OrderNumber,
            command.LineOperator,
            command.CustomerTaxCode,
            customer.Name,
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
