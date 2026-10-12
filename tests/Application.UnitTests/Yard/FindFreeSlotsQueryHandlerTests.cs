using SnpDepotManagement.Application.Common.Rules;
using SnpDepotManagement.Application.Features.Yard.Queries.FindFreeSlots;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.UnitTests.Yard;

public class FindFreeSlotsQueryHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly FindFreeSlotsQueryHandler _handler;

    public FindFreeSlotsQueryHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new FindFreeSlotsQueryHandler(_context, new RuleEngine());
    }

    [Fact]
    public async Task Handle_WhenBlockIsNewlyCreated_ShouldReturnGroundTierSlotsFor20Ft()
    {
        // Arrange: Block with Bays: 3 (1, 3), Rows: 1, Tiers: 2 -> 4 slots total
        var block = YardBlock.CreateStandard("BLK-FREE20", "Block", 3, 1, 2);
        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync();

        var query = new FindFreeSlotsQuery(block.Id, ContainerSize.Ft20);

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        // Tier 2 slots are NOT available yet because Tier 1 is empty (violates SlotBelowMustBeOccupiedRule)
        // Only Tier 1 slots (Bay 1 and Bay 3) are valid free slots
        result.Value.Should().HaveCount(2);
        result.Value.Should().AllSatisfy(p => p.Tier.Should().Be(1));
    }

    [Fact]
    public async Task Handle_WhenBlockIsNewlyCreated_ShouldReturnGroundTierEvenBayFor40Ft()
    {
        // Arrange: Block with Bays: 3 (1, 3), Rows: 1, Tiers: 2
        // For 40ft: Bay 2 requires Bay 1 and Bay 3 on ground tier
        var block = YardBlock.CreateStandard("BLK-FREE40", "Block 40", 3, 1, 2);
        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync();

        var query = new FindFreeSlotsQuery(block.Id, ContainerSize.Ft40);

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(1);
        result.Value.First().Bay.Should().Be(2);
        result.Value.First().Tier.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenGroundTierIsOccupied_ShouldIncludeTier2Slots()
    {
        // Arrange: Block with Bay 1, Row 1, Tier 1 occupied
        var block = YardBlock.CreateStandard("BLK-STACKFREE", "Block", 1, 1, 2);
        block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1).Occupy(Guid.NewGuid());

        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync();

        var query = new FindFreeSlotsQuery(block.Id, ContainerSize.Ft20);

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        // Tier 1 is occupied -> only Tier 2 is free and valid!
        result.Value.Should().HaveCount(1);
        result.Value.First().Tier.Should().Be(2);
        result.Value.First().Bay.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenBlockIsVirtual_ShouldReturnEmptyList()
    {
        // Arrange
        var block = YardBlock.CreateVirtual("BLK-VIRTFREE", "Virtual");
        _context.YardBlocks.Add(block);
        await _context.SaveChangesAsync();

        var query = new FindFreeSlotsQuery(block.Id, ContainerSize.Ft20);

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
