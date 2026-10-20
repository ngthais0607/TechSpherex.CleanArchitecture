using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Domain.UnitTests.Rules;

public class StackCompatibilityRuleTests
{
    private static (int Bay, int Row, int Tier, Guid? ContainerId, ContainerSize? ContainerSize) Slot(
        int bay, int row, int tier, Guid? containerId, ContainerSize? size) =>
        (bay, row, tier, containerId, size);

    [Fact]
    public void Ft20_OnTop_Of_Ft40_IsBroken()
    {
        // tier=2, slot bên dưới có container 40ft
        var slots = new[] { Slot(1, 1, 1, Guid.NewGuid(), ContainerSize.Ft40) };
        var rule = new StackCompatibilityRule(ContainerSize.Ft20, bay: 1, row: 1, tier: 2, slots);
        Assert.True(rule.IsBroken());
    }

    [Fact]
    public void Ft20_OnTop_Of_Ft20_IsOk()
    {
        var slots = new[] { Slot(1, 1, 1, Guid.NewGuid(), ContainerSize.Ft20) };
        var rule = new StackCompatibilityRule(ContainerSize.Ft20, bay: 1, row: 1, tier: 2, slots);
        Assert.False(rule.IsBroken());
    }

    [Fact]
    public void Ft40_OnTop_Of_SingleFt40_SamePosition_IsOk()
    {
        var containerId = Guid.NewGuid();
        // 40ft chiếm bay 1 và bay 3 (slot.Bay=1 và slot.Bay=3 cùng containerId)
        var slots = new[]
        {
            Slot(1, 1, 1, containerId, ContainerSize.Ft40),
            Slot(3, 1, 1, containerId, ContainerSize.Ft40)
        };
        var rule = new StackCompatibilityRule(ContainerSize.Ft40, bay: 2, row: 1, tier: 2, slots);
        Assert.False(rule.IsBroken());
    }

    [Fact]
    public void Ft40_OnTop_Of_Two_Ft20_IsOk()
    {
        var slots = new[]
        {
            Slot(1, 1, 1, Guid.NewGuid(), ContainerSize.Ft20),
            Slot(3, 1, 1, Guid.NewGuid(), ContainerSize.Ft20)
        };
        var rule = new StackCompatibilityRule(ContainerSize.Ft40, bay: 2, row: 1, tier: 2, slots);
        Assert.False(rule.IsBroken());
    }

    [Fact]
    public void Ft40_OnTop_Of_Misaligned_Ft40_IsBroken()
    {
        // bên dưới có 40ft nhưng ở wrong position hoặc khác containerId
        var slots = new[]
        {
            Slot(1, 1, 1, Guid.NewGuid(), ContainerSize.Ft40),
            Slot(3, 1, 1, Guid.NewGuid(), ContainerSize.Ft40)
        };
        var rule = new StackCompatibilityRule(ContainerSize.Ft40, bay: 2, row: 1, tier: 2, slots);
        Assert.True(rule.IsBroken());
    }

    [Fact]
    public void Tier1_IsAlwaysOk()
    {
        var slots = Array.Empty<(int Bay, int Row, int Tier, Guid? ContainerId, ContainerSize? ContainerSize)>();
        var rule = new StackCompatibilityRule(ContainerSize.Ft20, bay: 1, row: 1, tier: 1, slots);
        Assert.False(rule.IsBroken());
    }
}
