using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Domain.UnitTests.Rules;

public sealed class SlotBelowMustBeOccupiedRuleTests
{
    private readonly YardBlock _block;

    public SlotBelowMustBeOccupiedRuleTests()
    {
        // Block has 5 bays (odd bays 1, 3, 5), 2 rows, 4 tiers
        _block = YardBlock.CreateStandard("BLOCK-A", "Block A", bays: 5, rows: 2, tiers: 4);
    }

    [Fact]
    public void IsBroken_WhenTierIsOne_ShouldBeFalse()
    {
        // Ground tier never requires container below
        var rule = new SlotBelowMustBeOccupiedRule(tier: 1, ContainerSize.Ft20, bay: 1, row: 1, _block.Slots);

        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void IsBroken_WhenTierGreaterThanOneAnd20FtSlotBelowIsOccupied_ShouldBeFalse()
    {
        var slotBelow = _block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        slotBelow.Occupy(Guid.NewGuid());

        var rule = new SlotBelowMustBeOccupiedRule(tier: 2, ContainerSize.Ft20, bay: 1, row: 1, _block.Slots);

        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void IsBroken_WhenTierGreaterThanOneAnd20FtSlotBelowIsNotOccupied_ShouldBeTrue()
    {
        var rule = new SlotBelowMustBeOccupiedRule(tier: 2, ContainerSize.Ft20, bay: 1, row: 1, _block.Slots);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("YardRule.SlotBelowNotOccupied");
    }

    [Fact]
    public void IsBroken_When40FtAndBothSlotsBelowAreOccupied_ShouldBeFalse()
    {
        // Bay 2 (40ft) rests on odd bays 1 and 3
        var slot1 = _block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        var slot3 = _block.Slots.First(s => s.Bay == 3 && s.Row == 1 && s.Tier == 1);
        slot1.Occupy(Guid.NewGuid());
        slot3.Occupy(Guid.NewGuid());

        var rule = new SlotBelowMustBeOccupiedRule(tier: 2, ContainerSize.Ft40, bay: 2, row: 1, _block.Slots);

        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void IsBroken_When40FtAndOnlyOneSlotBelowIsOccupied_ShouldBeTrue()
    {
        var slot1 = _block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        slot1.Occupy(Guid.NewGuid());
        // slot at bay 3 is NOT occupied

        var rule = new SlotBelowMustBeOccupiedRule(tier: 2, ContainerSize.Ft40, bay: 2, row: 1, _block.Slots);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("YardRule.SlotBelowNotOccupied");
    }
}
