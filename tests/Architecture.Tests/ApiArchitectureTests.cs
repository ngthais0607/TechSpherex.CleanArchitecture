using FluentAssertions;
using NetArchTest.Rules;
using SnpDepotManagement.Api.Endpoints;
using Xunit;

namespace SnpDepotManagement.Architecture.Tests;

public sealed class ApiArchitectureTests
{
    [Fact]
    public void EndpointsMustNotAccessEntityFrameworkOrPersistence()
    {
        var result = Types.InAssembly(typeof(ContainerEndpoints).Assembly)
            .That().ResideInNamespace("SnpDepotManagement.Api.Endpoints")
            .ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "SnpDepotManagement.Infrastructure.Persistence")
            .GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
}
