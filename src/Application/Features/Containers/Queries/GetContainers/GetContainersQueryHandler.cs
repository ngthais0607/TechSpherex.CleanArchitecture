using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Containers.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Containers.Queries.GetContainers;

public sealed class GetContainersQueryHandler : IQueryHandler<GetContainersQuery, PagedResult<ContainerDto>>
{
    private readonly IAppDbContext _context;

    public GetContainersQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<ContainerDto>>> Handle(GetContainersQuery query, CancellationToken cancellationToken = default)
    {
        var filter = new ContainerFilter(query.Search, query.OwnerCode, query.Size, query.Condition,
            query.Type, query.LineOperator, query.DisplayStatus, query.Booking);
        if (!ContainerReadFilter.IsValid(filter) || !ContainerReadFilter.IsValidSort(query.Sort))
            return Result.Failure<PagedResult<ContainerDto>>(Error.Validation("Validation.Failed", "Unsupported container filter or sort."));
        var dbQuery = ContainerReadFilter.Apply(_context.Containers.AsNoTracking(), _context, filter);
        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var pageIndex = Math.Max(1, query.PageIndex);
        var pageSize = Math.Max(1, Math.Min(100, query.PageSize));

        var containers = await ContainerReadFilter.Order(dbQuery, query.Sort, query.Descending)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (containers.Count == 0)
        {
            return Result.Success(PagedResult<ContainerDto>.Create(new List<ContainerDto>(), totalCount, pageIndex, pageSize));
        }

        var containerIds = containers.Select(c => c.Id).ToList();

        // Load lifecycles
        var lifecycles = await _context.ContainerLifecycles
            .AsNoTracking()
            .Where(l => containerIds.Contains(l.ContainerId))
            .ToListAsync(cancellationToken);

        var activeLifecycles = lifecycles
            .Where(l => l.Status == LifecycleStatus.InYard)
            .ToDictionary(l => l.ContainerId);

        var hasGatedOutMap = lifecycles
            .GroupBy(l => l.ContainerId)
            .ToDictionary(g => g.Key, g => g.Any(l => l.Status == LifecycleStatus.GatedOut));

        // Load bookings
        var activeBookings = await _context.Bookings
            .AsNoTracking()
            .Where(b => containerIds.Contains(b.ContainerId) && b.Status == BookingStatus.Reserved)
            .ToListAsync(cancellationToken);

        var bookingMap = activeBookings.ToDictionary(b => b.ContainerId);

        var orderIds = activeBookings.Select(b => b.OrderId).Distinct().ToList();
        var orders = await _context.DeliveryOrders
            .AsNoTracking()
            .Where(o => orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        // Load slots and blocks
        var slotIds = activeLifecycles.Values
            .Where(l => l.CurrentSlotId.HasValue)
            .Select(l => l.CurrentSlotId!.Value)
            .Distinct()
            .ToList();

        var slots = await _context.YardSlots
            .AsNoTracking()
            .Where(s => slotIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        var slotMap = slots.ToDictionary(s => s.Id);

        var blockIds = slots.Select(s => s.BlockId)
            .Concat(activeLifecycles.Values.Where(l => l.VirtualBlockId.HasValue).Select(l => l.VirtualBlockId!.Value))
            .Distinct()
            .ToList();

        var blocks = await _context.YardBlocks
            .AsNoTracking()
            .Where(b => blockIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        var items = new List<ContainerDto>();

        foreach (var c in containers)
        {
            var booking = bookingMap.GetValueOrDefault(c.Id);
            var activeLc = activeLifecycles.GetValueOrDefault(c.Id);
            var hasGatedOut = hasGatedOutMap.GetValueOrDefault(c.Id, false);

            string displayStatus = booking is not null ? "Reserved"
                : activeLc is not null ? "InYard"
                : hasGatedOut ? "OutOfYard"
                : "NeverIn";

            SlotDto? currentSlot = null;
            if (activeLc?.CurrentSlotId is not null &&
                slotMap.TryGetValue(activeLc.CurrentSlotId.Value, out var slot) &&
                blocks.TryGetValue(slot.BlockId, out var block))
            {
                currentSlot = new SlotDto(block.Code, slot.Bay, slot.Row, slot.Tier);
            }
            else if (activeLc?.VirtualBlockId is not null &&
                     blocks.TryGetValue(activeLc.VirtualBlockId.Value, out var vBlock))
            {
                currentSlot = new SlotDto(vBlock.Code, 0, 0, 0);
            }

            BookingSummaryDto? currentBooking = null;
            if (booking is not null && orders.TryGetValue(booking.OrderId, out var order))
            {
                currentBooking = new BookingSummaryDto(order.OrderNumber, order.ExpiryDate);
            }

            items.Add(new ContainerDto(
                c.Id,
                c.ContainerNumber.Value,
                c.IsoCode,
                c.Size,
                c.Type,
                c.TareWeight,
                c.MaxGrossWeight,
                c.OwnerCode,
                c.Condition,
                c.Classification,
                c.DateOfManufacture,
                displayStatus,
                currentSlot,
                currentBooking,
                activeLc is not null));
        }

        var pagedResult = PagedResult<ContainerDto>.Create(items, totalCount, pageIndex, pageSize);
        return Result.Success(pagedResult);
    }
}
