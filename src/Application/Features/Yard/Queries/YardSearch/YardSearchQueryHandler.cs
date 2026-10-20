using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.Features.Yard.Queries.YardSearch;

public sealed class YardSearchQueryHandler : IQueryHandler<YardSearchQuery, YardSearchResult>
{
    private readonly IAppDbContext _context;

    public YardSearchQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<YardSearchResult>> Handle(YardSearchQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ContainerNumber))
        {
            return Result.Success(new YardSearchResult("NotFound", null));
        }

        var normNumber = query.ContainerNumber.Trim().ToUpperInvariant();
        var numberResult = ContainerNumber.Create(normNumber);
        if (numberResult.IsFailure)
        {
            return Result.Success(new YardSearchResult("NotFound", null));
        }

        var container = await _context.Containers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContainerNumber == numberResult.Value, cancellationToken);

        if (container is null)
        {
            return Result.Success(new YardSearchResult("NotFound", null));
        }

        var lifecycle = await _context.ContainerLifecycles
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ContainerId == container.Id && l.Status == LifecycleStatus.InYard, cancellationToken);

        if (lifecycle is null)
        {
            return Result.Success(new YardSearchResult("OutOfYard", null));
        }

        if (lifecycle.CurrentSlotId.HasValue)
        {
            var slot = await _context.YardSlots
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == lifecycle.CurrentSlotId.Value, cancellationToken);

            if (slot is not null)
            {
                var block = await _context.YardBlocks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == slot.BlockId, cancellationToken);

                if (block is not null)
                {
                    return Result.Success(new YardSearchResult(
                        "InBlock",
                        new YardLocationDto(block.Code, block.Id, slot.Position.Bay, slot.Position.Row, slot.Position.Tier, false)));
                }
            }
        }

        if (lifecycle.VirtualBlockId.HasValue)
        {
            var vBlock = await _context.YardBlocks
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == lifecycle.VirtualBlockId.Value, cancellationToken);

            if (vBlock is not null)
            {
                return Result.Success(new YardSearchResult(
                    "Virtual",
                    new YardLocationDto(vBlock.Code, vBlock.Id, null, null, null, true)));
            }
        }

        return Result.Success(new YardSearchResult("OutOfYard", null));
    }
}
