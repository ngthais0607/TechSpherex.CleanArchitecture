using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Queries.GetDeliveryOrderByOrderNumber;

public sealed class GetDeliveryOrderByOrderNumberQueryHandler : IQueryHandler<GetDeliveryOrderByOrderNumberQuery, DeliveryOrderDto>
{
    private readonly IAppDbContext _context;
    private readonly TimeProvider _clock;

    public GetDeliveryOrderByOrderNumberQueryHandler(IAppDbContext context, TimeProvider? clock = null)
    {
        _context = context;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<Result<DeliveryOrderDto>> Handle(GetDeliveryOrderByOrderNumberQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.OrderNumber))
        {
            return Result.Failure<DeliveryOrderDto>(
                Error.Validation("DeliveryOrder.EmptyOrderNumber", "Order number cannot be empty."));
        }

        var normalizedOrderNumber = query.OrderNumber.Trim().ToUpperInvariant();

        var order = await _context.DeliveryOrders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNumber == normalizedOrderNumber, cancellationToken);

        if (order is null)
        {
            return Result.Failure<DeliveryOrderDto>(
                Error.NotFound("DeliveryOrder.NotFound", "Delivery order not found."));
        }

        var dto = new DeliveryOrderDto(
            order.Id,
            order.OrderNumber,
            order.LineOperator,
            order.CustomerTaxCode,
            order.CustomerName,
            order.ExpiryDate,
            order.OutboundVessel,
            order.IsCompleted,
            order.Items.Select(i => new DeliveryOrderItemDto(
                i.Id,
                i.Size,
                i.Type,
                i.RequiredQuantity,
                i.DeliveredQuantity,
                i.RemainingQuantity)).ToList(), order.GetDisplayStatus(_clock.GetUtcNow().UtcDateTime).ToString());

        return Result.Success(dto);
    }
}
