using FluentAssertions;
using SnpDepotManagement.Domain.Entities.Identity;

namespace SnpDepotManagement.Domain.UnitTests.Identity;

public sealed class ApplicationUserTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Act
        var result = ApplicationUser.Create("operator01", "operator01@snp.com.vn", "Nguyen", "Van A");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserName.Should().Be("operator01");
        result.Value.Email.Should().Be("operator01@snp.com.vn");
        result.Value.FirstName.Should().Be("Nguyen");
        result.Value.LastName.Should().Be("Van A");
        result.Value.IsActive.Should().BeTrue();
        result.Value.RefreshTokenHash.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyUserName_ShouldReturnFailure(string? userName)
    {
        // Act
        var result = ApplicationUser.Create(userName!, "user@snp.com.vn", "Nguyen", "Van A");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.EmptyUserName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyEmail_ShouldReturnFailure(string? email)
    {
        // Act
        var result = ApplicationUser.Create("operator01", email!, "Nguyen", "Van A");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.EmptyEmail");
    }

    [Fact]
    public void Create_ShouldNormalizeEmailToLowerCase()
    {
        // Act
        var result = ApplicationUser.Create("operator01", "OPERATOR01@SNP.COM.VN", "Nguyen", "Van A");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("operator01@snp.com.vn");
    }

    [Fact]
    public void SetRefreshToken_ShouldUpdateTokenHashAndExpiryTime()
    {
        // Arrange
        var user = ApplicationUser.Create("operator01", "op@snp.com.vn", "Nguyen", "A").Value!;
        var expiry = DateTime.UtcNow.AddDays(7);

        // Act
        user.SetRefreshToken("hashed_secret_token_123", expiry);

        // Assert
        user.RefreshTokenHash.Should().Be("hashed_secret_token_123");
        user.RefreshTokenExpiryTime.Should().Be(expiry);
    }

    [Fact]
    public void RevokeRefreshToken_ShouldClearTokenHashAndExpiryTime()
    {
        // Arrange
        var user = ApplicationUser.Create("operator01", "op@snp.com.vn", "Nguyen", "A").Value!;
        user.SetRefreshToken("hashed_secret_token_123", DateTime.UtcNow.AddDays(7));

        // Act
        user.RevokeRefreshToken();

        // Assert
        user.RefreshTokenHash.Should().BeNull();
        user.RefreshTokenExpiryTime.Should().BeNull();
    }
}
