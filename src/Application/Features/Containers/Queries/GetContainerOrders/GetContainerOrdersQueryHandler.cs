using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.Features.Containers.Queries.GetContainerOrders;

public sealed class GetContainerOrdersQueryHandler : IQueryHandler<GetContainerOrdersQuery, ContainerOrdersDto>
{
    private readonly IAppDbContext _context;

    public GetContainerOrdersQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ContainerOrdersDto>> Handle(GetContainerOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var numberResult = ContainerNumber.Create(query.ContainerNumber);
        if (numberResult.IsFailure)
        {
            return Result.Failure<ContainerOrdersDto>(numberResult.Error);
        }

        var container = await _context.Containers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContainerNumber == numberResult.Value, cancellationToken);

        if (container is null)
        {
            return Result.Failure<ContainerOrdersDto>(
                Error.NotFound("Container.NotFound", $"Container '{query.ContainerNumber}' not found."));
        }

        var bookings = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.ContainerId == container.Id)
            .OrderByDescending(b => b.ReservedAt)
            .ToListAsync(cancellationToken);

        var orderIds = bookings.Select(b => b.OrderId).Distinct().ToList();
        var orders = await _context.DeliveryOrders
            .AsNoTracking()
            .Where(o => orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        var activeBooking = bookings.FirstOrDefault(b => b.Status == BookingStatus.Reserved);
        OrderSummaryDto? currentOrder = null;
        if (activeBooking is not null && orders.TryGetValue(activeBooking.OrderId, out var activeOrder))
        {
            currentOrder = new OrderSummaryDto(
                activeOrder.Id,
                activeOrder.OrderNumber,
                activeOrder.LineOperator,
                activeOrder.CustomerName,
                activeOrder.ExpiryDate,
                "Reserved");
        }

        var historyList = new List<OrderHistoryDto>();
        foreach (var b in bookings.Where(b => b.Status != BookingStatus.Reserved))
        {
            if (orders.TryGetValue(b.OrderId, out var ord))
            {
                historyList.Add(new OrderHistoryDto(
                    ord.Id,
                    ord.OrderNumber,
                    ord.LineOperator,
                    ord.CustomerName,
                    ord.ExpiryDate,
                    b.ShippedAt,
                    b.Status.ToString()));
            }
        }

        var allCustomers = orders.Values.Select(o => o.CustomerName).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var shippedBookings = bookings.Where(b => b.Status == BookingStatus.Shipped).OrderByDescending(b => b.ShippedAt).ToList();
        var lastShippedOrder = shippedBookings.FirstOrDefault();
        string? lastCustomer = null;
        if (lastShippedOrder is not null && orders.TryGetValue(lastShippedOrder.OrderId, out var lastOrd))
        {
            lastCustomer = lastOrd.CustomerName;
        }
        else if (orders.Count > 0)
        {
            lastCustomer = orders.Values.Last().CustomerName;
        }

        var summary = new ContainerOrderHistorySummary(
            CustomerCount: allCustomers.Count,
            ShipmentCount: shippedBookings.Count,
            LastCustomer: lastCustomer);

        return Result.Success(new ContainerOrdersDto(currentOrder, summary, historyList));
    }
}
