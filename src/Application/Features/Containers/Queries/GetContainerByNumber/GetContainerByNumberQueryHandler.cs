using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Containers.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.Features.Containers.Queries.GetContainerByNumber;

public sealed class GetContainerByNumberQueryHandler : IQueryHandler<GetContainerByNumberQuery, ContainerDto>
{
    private readonly IAppDbContext _context;

    public GetContainerByNumberQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ContainerDto>> Handle(GetContainerByNumberQuery query, CancellationToken cancellationToken = default)
    {
        var numberResult = ContainerNumber.Create(query.ContainerNumber);
        if (numberResult.IsFailure)
        {
            return Result.Failure<ContainerDto>(numberResult.Error);
        }

        var containerNumber = numberResult.Value!;

        var container = await _context.Containers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContainerNumber == containerNumber, cancellationToken);

        if (container is null)
        {
            return Result.Failure<ContainerDto>(
                Error.NotFound("Container.NotFound", $"Container '{containerNumber.Value}' not found."));
        }

        var lifecycles = await _context.ContainerLifecycles
            .AsNoTracking()
            .Where(l => l.ContainerId == container.Id)
            .ToListAsync(cancellationToken);

        var activeLc = lifecycles.FirstOrDefault(l => l.Status == LifecycleStatus.InYard);
        var hasGatedOut = lifecycles.Any(l => l.Status == LifecycleStatus.GatedOut);

        var activeBooking = await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.ContainerId == container.Id && b.Status == BookingStatus.Reserved, cancellationToken);

        string displayStatus = activeBooking is not null ? "Reserved"
            : activeLc?.VirtualBlockId is not null ? "VirtualBlock"
            : activeLc is not null ? "InYard"
            : hasGatedOut ? "OutOfYard"
            : "NeverIn";

        SlotDto? currentSlot = null;
        if (activeLc?.CurrentSlotId is not null)
        {
            var slot = await _context.YardSlots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == activeLc.CurrentSlotId.Value, cancellationToken);
            if (slot is not null)
            {
                var block = await _context.YardBlocks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == slot.BlockId, cancellationToken);
                if (block is not null)
                {
                    currentSlot = new SlotDto(block.Code, slot.Bay, slot.Row, slot.Tier);
                }
            }
        }
        else if (activeLc?.VirtualBlockId is not null)
        {
            var vBlock = await _context.YardBlocks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == activeLc.VirtualBlockId.Value, cancellationToken);
            if (vBlock is not null)
            {
                currentSlot = new SlotDto(vBlock.Code, 0, 0, 0);
            }
        }

        BookingSummaryDto? currentBooking = null;
        if (activeBooking is not null)
        {
            var order = await _context.DeliveryOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == activeBooking.OrderId, cancellationToken);
            if (order is not null)
            {
                currentBooking = new BookingSummaryDto(order.OrderNumber, order.ExpiryDate);
            }
        }

        var dto = new ContainerDto(
            container.Id,
            container.ContainerNumber.Value,
            container.IsoCode,
            container.Size,
            container.Type,
            container.TareWeight,
            container.MaxGrossWeight,
            container.OwnerCode,
            container.Condition,
            container.Classification,
            container.DateOfManufacture,
            displayStatus,
            currentSlot,
            currentBooking,
            activeLc is not null);

        return Result.Success(dto);
    }
}
