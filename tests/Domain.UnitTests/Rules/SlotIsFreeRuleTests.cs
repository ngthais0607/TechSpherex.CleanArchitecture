using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Domain.UnitTests.Rules;

public sealed class SlotIsFreeRuleTests
{
    private readonly YardBlock _block;

    public SlotIsFreeRuleTests()
    {
        _block = YardBlock.CreateStandard("BLOCK-A", "Block A", bays: 5, rows: 2, tiers: 3);
    }

    [Fact]
    public void IsBroken_When20FtSlotIsFree_ShouldBeFalse()
    {
        var rule = new SlotIsFreeRule(ContainerSize.Ft20, bay: 1, row: 1, tier: 1, _block.Slots);

        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void IsBroken_When20FtSlotIsOccupied_ShouldBeTrue()
    {
        var slot = _block.Slots.First(s => s.Bay == 1 && s.Row == 1 && s.Tier == 1);
        slot.Occupy(Guid.NewGuid());

        var rule = new SlotIsFreeRule(ContainerSize.Ft20, bay: 1, row: 1, tier: 1, _block.Slots);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("YardRule.SlotAlreadyOccupied");
    }

    [Fact]
    public void IsBroken_When40FtAndBothSlotsAreFree_ShouldBeFalse()
    {
        // Bay 2 (40ft) occupies adjacent bays 1 and 3
        var rule = new SlotIsFreeRule(ContainerSize.Ft40, bay: 2, row: 1, tier: 1, _block.Slots);

        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void IsBroken_When40FtAndOneSlotIsOccupied_ShouldBeTrue()
    {
        var slot3 = _block.Slots.First(s => s.Bay == 3 && s.Row == 1 && s.Tier == 1);
        slot3.Occupy(Guid.NewGuid());

        var rule = new SlotIsFreeRule(ContainerSize.Ft40, bay: 2, row: 1, tier: 1, _block.Slots);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("YardRule.SlotAlreadyOccupied");
    }

    [Fact]
    public void IsBroken_WhenSlotDoesNotExist_ShouldBeTrue()
    {
        // Bay 99 does not exist
        var rule = new SlotIsFreeRule(ContainerSize.Ft20, bay: 99, row: 1, tier: 1, _block.Slots);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("YardRule.SlotNotFound");
    }
}
