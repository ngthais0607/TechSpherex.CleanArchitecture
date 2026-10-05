using NetArchTest.Rules;
using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Architecture.Tests;

public sealed class DomainArchitectureTests
{
    [Fact]
    public void Domain_ShouldNotHaveDependencyOnOtherLayers()
    {
        var domainAssembly = typeof(BaseEntity).Assembly;

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "SnpDepotManagement.Application",
                "SnpDepotManagement.Infrastructure",
                "SnpDepotManagement.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
