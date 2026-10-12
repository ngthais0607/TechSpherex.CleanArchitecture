using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Abstractions.Data;
using SnpDepotManagement.Application.Abstractions.Messaging;
using SnpDepotManagement.Application.Abstractions.Rules;
using SnpDepotManagement.Application.Features.Yard.Dtos;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Common.Rules;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Application.Features.Yard.Queries.FindFreeSlots;

public sealed class FindFreeSlotsQueryHandler : IQueryHandler<FindFreeSlotsQuery, List<YardPositionDto>>
{
    private readonly IAppDbContext _context;
    private readonly IRuleEngine _ruleEngine;

    public FindFreeSlotsQueryHandler(IAppDbContext context, IRuleEngine ruleEngine)
    {
        _context = context;
        _ruleEngine = ruleEngine;
    }

    public async Task<Result<List<YardPositionDto>>> Handle(FindFreeSlotsQuery query, CancellationToken cancellationToken = default)
    {
        var block = await _context.YardBlocks
            .Include(b => b.Slots)
            .FirstOrDefaultAsync(b => b.Id == query.BlockId, cancellationToken);

        if (block is null)
        {
            return Result.Failure<List<YardPositionDto>>(
                Error.NotFound("YardBlock.NotFound", "Yard block not found."));
        }

        if (block.IsVirtual)
        {
            return Result.Success(new List<YardPositionDto>());
        }

        var availablePositions = new List<YardPositionDto>();

        // Prioritize bottom tiers first (Tier 1 -> Tier 2 -> ...)
        for (int tier = 1; tier <= block.MaxTiers; tier++)
        {
            for (int row = 1; row <= block.MaxRows; row++)
            {
                if (query.Size == ContainerSize.Ft20)
                {
                    for (int bay = 1; bay <= block.MaxBays; bay += 2)
                    {
                        var rules = new IBusinessRule[]
                        {
                            new ValidBayContainerSizeRule(bay, query.Size, block.MaxBays),
                            new TierWithinBlockLimitRule(tier, block.MaxTiers),
                            new SlotIsFreeRule(query.Size, bay, row, tier, block.Slots),
                            new SlotBelowMustBeOccupiedRule(tier, query.Size, bay, row, block.Slots)
                        };

                        if (_ruleEngine.Evaluate(rules).IsSuccess)
                        {
                            availablePositions.Add(new YardPositionDto(bay, row, tier));
                        }
                    }
                }
                else if (query.Size == ContainerSize.Ft40)
                {
                    for (int bay = 2; bay <= block.MaxBays; bay += 2)
                    {
                        var rules = new IBusinessRule[]
                        {
                            new ValidBayContainerSizeRule(bay, query.Size, block.MaxBays),
                            new TierWithinBlockLimitRule(tier, block.MaxTiers),
                            new SlotIsFreeRule(query.Size, bay, row, tier, block.Slots),
                            new SlotBelowMustBeOccupiedRule(tier, query.Size, bay, row, block.Slots)
                        };

                        if (_ruleEngine.Evaluate(rules).IsSuccess)
                        {
                            availablePositions.Add(new YardPositionDto(bay, row, tier));
                        }
                    }
                }
            }
        }

        return Result.Success(availablePositions);
    }
}
