using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnpDepotManagement.Infrastructure;
using Xunit;

namespace SnpDepotManagement.Infrastructure.UnitTests.Identity;

public sealed class InfrastructureConfigurationTests
{
    [Fact]
    public void MissingJwtSecretMustNotFallBackToDevelopmentKey()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test" }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(configuration));
    }
}
