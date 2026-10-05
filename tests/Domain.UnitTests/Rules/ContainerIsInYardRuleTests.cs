using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Domain.UnitTests.Rules;

public sealed class ContainerIsInYardRuleTests
{
    [Fact]
    public void IsBroken_WhenContainerIsInYard_ShouldBeFalse()
    {
        var lifecycle = ContainerLifecycle.GateIn(
            Guid.NewGuid(),
            "MSC",
            Classification.A,
            ContainerCondition.Normal,
            "51C-12345",
            Guid.NewGuid());

        var rule = new ContainerIsInYardRule(lifecycle);

        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void IsBroken_WhenContainerHasGatedOut_ShouldBeTrue()
    {
        var lifecycle = ContainerLifecycle.GateIn(
            Guid.NewGuid(),
            "MSC",
            Classification.A,
            ContainerCondition.Normal,
            "51C-12345",
            Guid.NewGuid());

        lifecycle.GateOut("51D-99999", Guid.NewGuid());

        var rule = new ContainerIsInYardRule(lifecycle);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("LifecycleRule.ContainerNotInYard");
    }
}
