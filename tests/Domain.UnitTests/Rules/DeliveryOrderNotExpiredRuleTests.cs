using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Domain.UnitTests.Rules;

public sealed class DeliveryOrderNotExpiredRuleTests
{
    [Fact]
    public void PreviousBusinessDayExpiresEvenWhenUtcDateIsStillTheSame()
    {
        var expiry = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
        var dispatch = new DateTime(2026, 10, 15, 17, 30, 0, DateTimeKind.Utc);
        new DeliveryOrderNotExpiredRule(expiry, dispatch).IsBroken().Should().BeTrue();
    }

    private readonly DateTime _expiryDate = new(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

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
