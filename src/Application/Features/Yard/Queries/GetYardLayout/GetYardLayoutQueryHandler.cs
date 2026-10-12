using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Features.Yard.Dtos;
using SnpDepotManagement.Domain.Common;

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
        var block = await _context.YardBlocks
            .AsNoTracking()
            .Include(b => b.Slots)
            .FirstOrDefaultAsync(b => b.Id == query.BlockId, cancellationToken);

        if (block is null)
        {
            return Result.Failure<YardLayoutDto>(
                Error.NotFound("YardBlock.NotFound", "Yard block not found."));
        }

        var slotDtos = block.Slots
            .OrderBy(s => s.Bay)
            .ThenBy(s => s.Row)
            .ThenBy(s => s.Tier)
            .Select(s => new YardSlotDto(
                s.Id,
                s.Bay,
                s.Row,
                s.Tier,
                s.ContainerId,
                s.IsOccupied))
            .ToList();

        var dto = new YardLayoutDto(
            block.Id,
            block.Code,
            block.Name,
            block.IsVirtual,
            block.MaxBays,
            block.MaxRows,
            block.MaxTiers,
            slotDtos);

        return Result.Success(dto);
    }
}
