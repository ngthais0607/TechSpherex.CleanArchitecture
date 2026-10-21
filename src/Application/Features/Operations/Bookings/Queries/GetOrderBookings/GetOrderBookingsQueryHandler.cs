using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;
namespace SnpDepotManagement.Application.Features.Operations.Bookings.Queries.GetOrderBookings;

public sealed class GetOrderBookingsQueryHandler(IAppDbContext db) : IQueryHandler<GetOrderBookingsQuery, OrderBookingsDto>
{
    public async Task<Result<OrderBookingsDto>> Handle(GetOrderBookingsQuery query, CancellationToken cancellationToken = default)
    {
        var number = query.OrderNumber.Trim().ToUpperInvariant();
        var order = await db.DeliveryOrders.AsNoTracking().Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.OrderNumber == number, cancellationToken);
        if (order is null) return Result.Failure<OrderBookingsDto>(Error.NotFound("DeliveryOrder.NotFound", "Không tìm thấy lệnh giao."));
        var source = db.Bookings.AsNoTracking().Where(booking => booking.OrderId == order.Id);
        var total = await source.CountAsync(cancellationToken);
        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var bookings = await source.OrderByDescending(booking => booking.ReservedAt).ThenBy(booking => booking.Id)
            .Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var ids = bookings.Select(booking => booking.ContainerId).ToList();
        var numbers = await db.Containers.AsNoTracking().Where(container => ids.Contains(container.Id))
            .Select(container => new { container.Id, Number = container.ContainerNumber.Value })
            .ToDictionaryAsync(container => container.Id, container => container.Number, cancellationToken);
        var reserved = await source.Where(booking => booking.Status == BookingStatus.Reserved).GroupBy(booking => booking.ItemId)
            .Select(group => new { ItemId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.ItemId, group => group.Count, cancellationToken);
        var counts = order.Items.Select(item => new OrderItemBookingCountsDto(item.Id,
            reserved.GetValueOrDefault(item.Id), item.DeliveredQuantity,
            Math.Max(0, item.RemainingQuantity - reserved.GetValueOrDefault(item.Id)))).ToArray();
        var rows = bookings.Select(booking => new OrderBookingDto(booking.Id, booking.ItemId,
            numbers.GetValueOrDefault(booking.ContainerId, ""), booking.Status.ToString(), booking.ReservedAt, booking.ShippedAt)).ToArray();
        return Result.Success(new OrderBookingsDto(PagedResult<OrderBookingDto>.Create(rows, total, pageIndex, pageSize), counts));
    }
}
