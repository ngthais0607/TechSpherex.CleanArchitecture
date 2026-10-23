using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Yard.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.Features.Yard.Queries.GetYardLayout;

public sealed class GetYardLayoutQueryHandler : IQueryHandler<GetYardLayoutQuery, YardLayoutDto>
{
    private readonly IAppDbContext _context;

    public GetYardLayoutQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<YardLayoutDto>> Handle(GetYardLayoutQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Domain.Entities.Yard.YardBlock> blockQuery = _context.YardBlocks
            .AsNoTracking()
            .Include(b => b.Slots);

        Domain.Entities.Yard.YardBlock? block = null;

        if (query.BlockId.HasValue && query.BlockId.Value != Guid.Empty)
        {
            block = await blockQuery.FirstOrDefaultAsync(b => b.Id == query.BlockId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(query.IdOrCode))
        {
            var raw = query.IdOrCode.Trim();
            if (Guid.TryParse(raw, out var parsedGuid))
            {
                var norm = raw.ToUpperInvariant();
                block = await blockQuery.FirstOrDefaultAsync(b => b.Id == parsedGuid || b.Code == norm, cancellationToken);
            }
            else
            {
                var norm = raw.ToUpperInvariant();
                block = await blockQuery.FirstOrDefaultAsync(b => b.Code == norm, cancellationToken);
            }
        }

        if (block is null)
        {
            return Result.Failure<YardLayoutDto>(
                Error.NotFound("YardBlock.NotFound", "Không tìm thấy dãy bãi."));
        }

        var slotsEnumerable = block.Slots.AsEnumerable();
        if (query.Tier.HasValue && query.Tier.Value > 0)
        {
            slotsEnumerable = slotsEnumerable.Where(s => s.Position.Tier == query.Tier.Value);
        }

        var slotDtos = slotsEnumerable
            .OrderBy(s => s.Position.Bay)
            .ThenBy(s => s.Position.Row)
            .ThenBy(s => s.Position.Tier)
            .Select(s => new YardSlotDto(
                s.Id,
                s.Position.Bay,
                s.Position.Row,
                s.Position.Tier,
                s.ContainerId,
                s.IsOccupied))
            .ToList();

        var containerIds = slotDtos
            .Where(s => s.ContainerId.HasValue)
            .Select(s => s.ContainerId!.Value)
            .Distinct()
            .ToList();

        var containerDtos = new List<BlockContainerDto>();

        if (containerIds.Count > 0)
        {
            var containers = await _context.Containers
                .AsNoTracking()
                .Where(c => containerIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

            var bookings = await _context.Bookings
                .AsNoTracking()
                .Where(b => containerIds.Contains(b.ContainerId) && b.Status == BookingStatus.Reserved)
                .ToListAsync(cancellationToken);

            var orderIds = bookings.Select(b => b.OrderId).Distinct().ToList();
            var orders = orderIds.Count > 0
                ? await _context.DeliveryOrders
                    .AsNoTracking()
                    .Where(o => orderIds.Contains(o.Id))
                    .ToDictionaryAsync(o => o.Id, cancellationToken)
                : new Dictionary<Guid, Domain.Entities.Operations.DeliveryOrder>();

            var bookingByCont = bookings.ToDictionary(b => b.ContainerId);

            foreach (var c in containers)
            {
                string? orderNum = null;
                DateTime? orderExp = null;

                if (bookingByCont.TryGetValue(c.Id, out var bk) && orders.TryGetValue(bk.OrderId, out var ord))
                {
                    orderNum = ord.OrderNumber;
                    orderExp = ord.ExpiryDate;
                }

                containerDtos.Add(new BlockContainerDto(
                    c.Id,
                    c.ContainerNumber.Value,
                    c.IsoCode,
                    c.Size.ToString(),
                    c.Type.ToString(),
                    c.Classification.ToString(),
                    c.Condition.ToString(),
                    c.OwnerCode,
                    orderNum,
                    orderExp));
            }
        }

        var oddBays = (block.MaxBays + 1) / 2;
        var dto = new YardLayoutDto(
            block.Id,
            block.Code,
            block.Name,
            block.IsVirtual,
            block.MaxBays,
            oddBays,
            block.MaxRows,
            block.MaxTiers,
            slotDtos,
            containerDtos);

        return Result.Success(dto);
    }
}
