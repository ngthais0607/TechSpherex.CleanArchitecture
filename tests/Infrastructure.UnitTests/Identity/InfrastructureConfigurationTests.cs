using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnpDepotManagement.Infrastructure;
using Xunit;

namespace SnpDepotManagement.Infrastructure.UnitTests.Identity;

public sealed class InfrastructureConfigurationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankDatabaseConfigurationFailsImmediately(string connection)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "TestOnlySecretWithAtLeastThirtyTwoCharacters!", ["Jwt:Issuer"] = "Tests", ["Jwt:Audience"] = "Tests",
            ["ConnectionStrings:DefaultConnection"] = connection
        }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(configuration));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("REPLACE_WITH_CRYPTOGRAPHIC_BASE64_SECRET_AT_LEAST_32_BYTES")]
    public void InvalidJwtSecretMustNotFallBackToDevelopmentKey(string? secret)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test",
                ["Jwt:Secret"] = secret, ["Jwt:Issuer"] = "Test", ["Jwt:Audience"] = "Test"
            }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(configuration));
    }
}
