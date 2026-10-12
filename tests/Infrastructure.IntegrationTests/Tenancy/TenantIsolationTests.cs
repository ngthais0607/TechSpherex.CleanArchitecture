using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;
using SnpDepotManagement.Infrastructure.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Infrastructure.IntegrationTests.Tenancy;

[Collection("PostgreSqlCollection")]
public sealed class TenantIsolationTests
{
    private readonly PostgreSqlFixture _fixture;

    public TenantIsolationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task QueryFilter_ShouldIsolateDataBetweenTenants()
    {
        var tenantAlpha = "DEPOT-ALPHA";
        var tenantBeta = "DEPOT-BETA";

        var numberAlpha = ContainerTestHelper.CreateValidContainerNumber("MSKU", 123456);
        var numberBeta = ContainerTestHelper.CreateValidContainerNumber("CMAU", 654321);

        var containerAlpha = Container.Create(
            numberAlpha,
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            2200m,
            30480m,
            null,
            "MSK",
            tenantAlpha).Value!;

        var containerBeta = Container.Create(
            numberBeta,
            "42G1",
            ContainerSize.Ft40,
            ContainerType.Dry,
            3700m,
            32500m,
            null,
            "CMA",
            tenantBeta).Value!;

        await using (var contextAlphaSetup = _fixture.CreateContext(tenantAlpha))
        {
            contextAlphaSetup.Containers.Add(containerAlpha);
            await contextAlphaSetup.SaveChangesAsync();
        }

        await using (var contextBetaSetup = _fixture.CreateContext(tenantBeta))
        {
            contextBetaSetup.Containers.Add(containerBeta);
            await contextBetaSetup.SaveChangesAsync();
        }

        // Querying from Alpha should only see Alpha
        await using (var contextAlpha = _fixture.CreateContext(tenantAlpha))
        {
            var alphaContainers = await contextAlpha.Containers.ToListAsync();
            alphaContainers.Should().ContainSingle(c => c.ContainerNumber.Value == numberAlpha.Value);
            alphaContainers.Should().NotContain(c => c.ContainerNumber.Value == numberBeta.Value);
        }

        // Querying from Beta should only see Beta
        await using (var contextBeta = _fixture.CreateContext(tenantBeta))
        {
            var betaContainers = await contextBeta.Containers.ToListAsync();
            betaContainers.Should().ContainSingle(c => c.ContainerNumber.Value == numberBeta.Value);
            betaContainers.Should().NotContain(c => c.ContainerNumber.Value == numberAlpha.Value);
        }
    }

    [Fact]
    public async Task SameContainerNumber_AllowedAcrossDifferentTenants_DueToTenantUniqueConstraint()
    {
        var tenantOne = "DEPOT-NORTH";
        var tenantTwo = "DEPOT-SOUTH";
        var sharedNumber = ContainerTestHelper.CreateValidContainerNumber("TGHU", 789012);

        var container1 = Container.Create(
            sharedNumber,
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            2200m,
            30480m,
            null,
            "TGH",
            tenantOne).Value!;

        var container2 = Container.Create(
            sharedNumber,
            "22G1",
            ContainerSize.Ft20,
            ContainerType.Dry,
            2200m,
            30480m,
            null,
            "TGH",
            tenantTwo).Value!;

        await using var context1 = _fixture.CreateContext(tenantOne);
        await using var context2 = _fixture.CreateContext(tenantTwo);

        context1.Containers.Add(container1);
        await context1.SaveChangesAsync();

        context2.Containers.Add(container2);
        var act = () => context2.SaveChangesAsync();

        // Both should succeed because unique index is (TenantId, ContainerNumber)
        await act.Should().NotThrowAsync();
    }
}
