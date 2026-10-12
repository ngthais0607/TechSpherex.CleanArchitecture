using FluentAssertions;
using SnpDepotManagement.Infrastructure.Identity;
using Xunit;

namespace SnpDepotManagement.Infrastructure.UnitTests.Identity;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void HashPassword_ShouldProduceThreePartFormat()
    {
        var password = "SecurePassword@123";
        var hash = _sut.HashPassword(password);

        hash.Should().NotBeNullOrWhiteSpace();
        var parts = hash.Split('.');
        parts.Should().HaveCount(3);
        int.TryParse(parts[0], out var iterations).Should().BeTrue();
        iterations.Should().Be(100_000);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        var password = "CorrectPassword!123";
        var hash = _sut.HashPassword(password);

        var result = _sut.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
    {
        var password = "CorrectPassword!123";
        var hash = _sut.HashPassword(password);

        var result = _sut.VerifyPassword("WrongPassword!123", hash);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-hash")]
    [InlineData("100000.invalidbase64.invalidbase64")]
    public void VerifyPassword_WithMalformedHash_ShouldReturnFalse(string malformedHash)
    {
        var result = _sut.VerifyPassword("anyPassword", malformedHash);

        result.Should().BeFalse();
    }
}
