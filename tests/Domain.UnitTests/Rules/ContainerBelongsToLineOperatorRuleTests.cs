using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Domain.UnitTests.Rules;

public sealed class ContainerBelongsToLineOperatorRuleTests
{
    [Theory]
    [InlineData("MSC", "MSC")]
    [InlineData("msc", "MSC")]
    [InlineData("CMA", "CMA ")]
    public void IsBroken_WhenLineOperatorMatches_ShouldBeFalse(string containerOperator, string orderOperator)
    {
        var rule = new ContainerBelongsToLineOperatorRule(containerOperator, orderOperator);

        rule.IsBroken().Should().BeFalse();
    }

    [Theory]
    [InlineData("MSC", "ONE")]
    [InlineData("CMA", "HAPAG")]
    [InlineData("", "MSC")]
    public void IsBroken_WhenLineOperatorMismatch_ShouldBeTrue(string containerOperator, string orderOperator)
    {
        var rule = new ContainerBelongsToLineOperatorRule(containerOperator, orderOperator);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("OrderRule.LineOperatorMismatch");
    }
}
