using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.UnitTests.Yard;

public sealed class YardBlockTests
{
    [Fact]
    public void CreateStandard_ShouldGenerateSlotsOnlyForOddBays()
    {
        // 5 bays has odd bays: 1, 3, 5 (3 odd bays). 4 rows, 3 tiers => 3 * 4 * 3 = 36 slots
        var block = YardBlock.CreateStandard("BLOCK-A", "Standard Block A", bays: 5, rows: 4, tiers: 3);

        block.IsVirtual.Should().BeFalse();
        block.BlockType.Should().Be(YardBlockType.Standard);
        block.MaxBays.Should().Be(5);
        block.MaxRows.Should().Be(4);
        block.MaxTiers.Should().Be(3);
        block.Slots.Should().HaveCount(36);
        block.Slots.All(s => s.Bay % 2 != 0).Should().BeTrue("YardSlot must only exist at odd bays");
    }

    [Fact]
    public void CreateVirtual_ShouldHaveNoSlotsAndBeVirtual()
    {
        var block = YardBlock.CreateVirtual("VIRTUAL-01", "Virtual Block for Maintenance");

        block.IsVirtual.Should().BeTrue();
        block.BlockType.Should().Be(YardBlockType.Virtual);
        block.MaxBays.Should().Be(0);
        block.MaxRows.Should().Be(0);
        block.MaxTiers.Should().Be(0);
        block.Slots.Should().BeEmpty();
    }

    [Fact]
    public void Expand_ShouldIncreaseDimensionsAndGenerateNewSlots()
    {
        var block = YardBlock.CreateStandard("BLOCK-A", "Block A", bays: 3, rows: 2, tiers: 2);
        // Odd bays: 1, 3 (2 bays) * 2 * 2 = 8 slots
        block.Slots.Should().HaveCount(8);

        var result = block.Expand(newMaxBays: 5, newMaxRows: 2, newMaxTiers: 2);

        result.IsSuccess.Should().BeTrue();
        block.MaxBays.Should().Be(5);
        // Odd bays now: 1, 3, 5 (3 bays) * 2 * 2 = 12 slots
        block.Slots.Should().HaveCount(12);
        block.Slots.Should().Contain(s => s.Bay == 5 && s.Row == 1 && s.Tier == 1);
    }

    [Fact]
    public void Shrink_WhenCutAreaIsEmpty_ShouldSucceedAndRemoveSlots()
    {
        var block = YardBlock.CreateStandard("BLOCK-A", "Block A", bays: 5, rows: 2, tiers: 2);
        block.Slots.Should().HaveCount(12);

        var result = block.Shrink(newMaxBays: 3, newMaxRows: 2, newMaxTiers: 2);

        result.IsSuccess.Should().BeTrue();
        block.MaxBays.Should().Be(3);
        block.Slots.Should().HaveCount(8);
        block.Slots.Any(s => s.Bay > 3).Should().BeFalse();
    }

    [Fact]
    public void Shrink_WhenCutAreaContainsOccupiedSlot_ShouldBeBlocked()
    {
        var block = YardBlock.CreateStandard("BLOCK-A", "Block A", bays: 5, rows: 2, tiers: 2);
        // Occupy slot at bay 5
        var slotAtBay5 = block.Slots.First(s => s.Bay == 5 && s.Row == 1 && s.Tier == 1);
        slotAtBay5.Occupy(Guid.NewGuid());

        var result = block.Shrink(newMaxBays: 3, newMaxRows: 2, newMaxTiers: 2);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("YardBlock.CannotShrinkOccupiedSlots");
        block.MaxBays.Should().Be(5); // Dimensions unchanged
        block.Slots.Should().HaveCount(12);
    }

    [Fact]
    public void CreateStandard_ShouldAssignTenantIdAndYardPositionToSlots()
    {
        var block = YardBlock.CreateStandard("BLOCK-B", "Block B", bays: 3, rows: 2, tiers: 2, tenantId: "DEPOT-01");

        block.TenantId.Should().Be("DEPOT-01");
        block.Slots.All(s => s.TenantId == "DEPOT-01").Should().BeTrue();
        block.Slots.All(s => s.Position != null).Should().BeTrue();
        block.Slots.All(s => s.Position.Block == "BLOCK-B").Should().BeTrue();
    }
}
