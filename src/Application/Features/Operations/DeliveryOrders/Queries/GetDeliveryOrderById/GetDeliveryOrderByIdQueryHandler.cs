using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Dtos;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Queries.GetDeliveryOrderById;

public sealed class GetDeliveryOrderByIdQueryHandler : IQueryHandler<GetDeliveryOrderByIdQuery, DeliveryOrderDto>
{
    private readonly IAppDbContext _context;

    public GetDeliveryOrderByIdQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DeliveryOrderDto>> Handle(GetDeliveryOrderByIdQuery query, CancellationToken cancellationToken = default)
    {
        var order = await _context.DeliveryOrders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken);

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
                i.RemainingQuantity)).ToList());

        return Result.Success(dto);
    }
}
