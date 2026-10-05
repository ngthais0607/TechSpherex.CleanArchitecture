using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Domain.UnitTests.Rules;

public sealed class TierWithinBlockLimitRuleTests
{
    private const int MaxTiers = 4;

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)] // Exactly at limit
    public void IsBroken_WhenTierWithinLimit_ShouldBeFalse(int tier)
    {
        var rule = new TierWithinBlockLimitRule(tier, MaxTiers);

        rule.IsBroken().Should().BeFalse();
    }

    [Theory]
    [InlineData(5)] // maxTiers + 1
    [InlineData(6)]
    public void IsBroken_WhenTierExceedsMaxLimit_ShouldBeTrue(int tier)
    {
        var rule = new TierWithinBlockLimitRule(tier, MaxTiers);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("YardRule.TierExceedsBlockLimit");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IsBroken_WhenTierZeroOrNegative_ShouldBeTrue(int tier)
    {
        var rule = new TierWithinBlockLimitRule(tier, MaxTiers);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("YardRule.TierExceedsBlockLimit");
    }
}
