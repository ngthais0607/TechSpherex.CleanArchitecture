using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using SnpDepotManagement.Domain.Entities.Identity;
using SnpDepotManagement.Infrastructure.Identity;
using Xunit;

namespace SnpDepotManagement.Infrastructure.UnitTests.Identity;

public sealed class TokenServiceTests
{
    private readonly JwtOptions _options = new()
    {
        Secret = "ThisIsASecretKeyWithMoreThan32CharactersRequiredForHmacSha256!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        AccessTokenExpirationMinutes = 30,
        RefreshTokenExpirationDays = 7
    };

    [Fact]
    public void Constructor_ShouldThrow_WhenSecretTooShort()
    {
        var invalidOptions = Options.Create(new JwtOptions { Secret = "short_key" });
        var timeProvider = Substitute.For<TimeProvider>();

        var act = () => new TokenService(invalidOptions, timeProvider);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GenerateAccessToken_ShouldCreateValidJwtWithClaims()
    {
        var timeProvider = Substitute.For<TimeProvider>();
        var fixedTime = new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
        timeProvider.GetUtcNow().Returns(fixedTime);

        var sut = new TokenService(Options.Create(_options), timeProvider);
        var user = ApplicationUser.Create("planner", "planner@snp.com.vn", "Nguyen", "Van A", "YardPlanner", "hash").Value!;

        var tokenString = sut.GenerateAccessToken(user);

        tokenString.Should().NotBeNullOrWhiteSpace();

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.CanReadToken(tokenString).Should().BeTrue();

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = false
        };

        var principal = tokenHandler.ValidateToken(tokenString, validationParams, out var validatedToken);
        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(user.Id.ToString());
        principal.FindFirst(ClaimTypes.Name)!.Value.Should().Be(user.UserName);
        principal.FindFirst(ClaimTypes.Role)!.Value.Should().Be("YardPlanner");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnValidBase64String()
    {
        var timeProvider = Substitute.For<TimeProvider>();
        var sut = new TokenService(Options.Create(_options), timeProvider);

        var token1 = sut.GenerateRefreshToken();
        var token2 = sut.GenerateRefreshToken();

        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);

        var bytes = Convert.FromBase64String(token1);
        bytes.Length.Should().Be(64);
    }
}
