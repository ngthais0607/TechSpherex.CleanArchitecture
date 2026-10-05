using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Domain.UnitTests.Rules;

public sealed class ValidBayContainerSizeRuleTests
{
    private const int MaxBays = 5;

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void IsBroken_When20FtOnOddBayWithinBounds_ShouldBeFalse(int bay)
    {
        var rule = new ValidBayContainerSizeRule(bay, ContainerSize.Ft20, MaxBays);

        rule.IsBroken().Should().BeFalse();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void IsBroken_When20FtOnEvenBay_ShouldBeTrue(int bay)
    {
        var rule = new ValidBayContainerSizeRule(bay, ContainerSize.Ft20, MaxBays);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("YardRule.InvalidBayForSize");
    }

    [Theory]
    [InlineData(2)] // Adjacent bays: 1 and 3 (valid within 5)
    public void IsBroken_When40FtOnEvenBayWithAdjacentBaysInBlock_ShouldBeFalse(int bay)
    {
        var rule = new ValidBayContainerSizeRule(bay, ContainerSize.Ft40, MaxBays);

        rule.IsBroken().Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void IsBroken_When40FtOnOddBay_ShouldBeTrue(int bay)
    {
        var rule = new ValidBayContainerSizeRule(bay, ContainerSize.Ft40, MaxBays);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("YardRule.InvalidBayForSize");
    }

    [Fact]
    public void IsBroken_When40FtAtEdgeWhereAdjacentBayExceedsBlock_ShouldBeTrue()
    {
        // Block has 4 bays. Bay 4 is even, but bay + 1 = 5 > 4 (mép block)
        var rule = new ValidBayContainerSizeRule(bay: 4, ContainerSize.Ft40, maxBays: 4);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("YardRule.InvalidBayForSize");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void IsBroken_WhenBayOutOfBounds_ShouldBeTrue(int bay)
    {
        var rule = new ValidBayContainerSizeRule(bay, ContainerSize.Ft20, MaxBays);

        rule.IsBroken().Should().BeTrue();
    }
}
