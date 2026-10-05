using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Domain.UnitTests.Rules;

public sealed class DeliveryOrderNotExpiredRuleTests
{
    private readonly DateTime _expiryDate = new(2026, 10, 10, 23, 59, 59, DateTimeKind.Utc);

    [Fact]
    public void IsBroken_WhenDispatchDateBeforeExpiryDate_ShouldBeFalse()
    {
        // Đúng hạn
        var dispatchDate = new DateTime(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);
        var rule = new DeliveryOrderNotExpiredRule(_expiryDate, dispatchDate);

        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void IsBroken_WhenDispatchDateOnSameDayAsExpiryDate_ShouldBeFalse()
    {
        // Đúng ngày cuối cùng
        var dispatchDate = new DateTime(2026, 10, 10, 15, 0, 0, DateTimeKind.Utc);
        var rule = new DeliveryOrderNotExpiredRule(_expiryDate, dispatchDate);

        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void IsBroken_WhenDispatchDateAfterExpiryDate_ShouldBeTrue()
    {
        // Quá hạn
        var dispatchDate = new DateTime(2026, 10, 11, 8, 0, 0, DateTimeKind.Utc);
        var rule = new DeliveryOrderNotExpiredRule(_expiryDate, dispatchDate);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("OrderRule.DeliveryOrderExpired");
    }
}
