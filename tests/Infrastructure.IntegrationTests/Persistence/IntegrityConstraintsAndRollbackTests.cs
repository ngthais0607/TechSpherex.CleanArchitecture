using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;
using SnpDepotManagement.Infrastructure.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Infrastructure.IntegrationTests.Persistence;

[Collection("PostgreSqlCollection")]
public sealed class IntegrityConstraintsAndRollbackTests
{
    private readonly PostgreSqlFixture _fixture;

    public IntegrityConstraintsAndRollbackTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DuplicateContainerNumber_InSameTenant_ShouldThrowUniqueConstraintViolation()
    {
        var tenant = "DEPOT-INTEGRITY";
        var number = ContainerTestHelper.CreateValidContainerNumber("TEMU", 987654);

        var container1 = Container.Create(
            number, "22G1", ContainerSize.Ft20, ContainerType.Dry,
            2200m, 30480m, null, "TEM", tenant).Value!;

        var container2 = Container.Create(
            number, "22G1", ContainerSize.Ft20, ContainerType.Dry,
            2200m, 30480m, null, "TEM", tenant).Value!;

        await using var context = _fixture.CreateContext(tenant);
        context.Containers.Add(container1);
        await context.SaveChangesAsync();

        context.Containers.Add(container2);
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task DeleteContainer_ReferencedByLifecycle_ShouldThrowDeleteRestrictViolation()
    {
        var tenant = "DEPOT-RESTRICT";
        var number = ContainerTestHelper.CreateValidContainerNumber("OOLU", 123456);
        var container = Container.Create(
            number, "22G1", ContainerSize.Ft20, ContainerType.Dry,
            2200m, 30480m, null, "OOL", tenant).Value!;

        await using (var setupContext = _fixture.CreateContext(tenant))
        {
            setupContext.Containers.Add(container);
            await setupContext.SaveChangesAsync();

            var lifecycle = ContainerLifecycle.GateIn(
                container.Id,
                "ONE",
                Classification.A,
                ContainerCondition.Normal,
                "51C-12345",
                Guid.NewGuid(),
                DateTime.UtcNow,
                tenant);

            setupContext.ContainerLifecycles.Add(lifecycle);
            await setupContext.SaveChangesAsync();
        }

        await using (var deleteContext = _fixture.CreateContext(tenant))
        {
            var trackedContainer = await deleteContext.Containers.FirstAsync(c => c.Id == container.Id);
            deleteContext.Containers.Remove(trackedContainer);

            var act = () => deleteContext.SaveChangesAsync();

            // Should fail due to Restrict foreign key from container_lifecycles
            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }

    [Fact]
    public async Task TransactionRollback_ShouldDiscardAllPendingChanges()
    {
        var tenant = "DEPOT-ROLLBACK";
        var initialCount = 0;

        await using (var checkContext = _fixture.CreateContext(tenant))
        {
            initialCount = await checkContext.Containers.CountAsync();
        }

        await using (var transContext = _fixture.CreateContext(tenant))
        await using (var transaction = await transContext.Database.BeginTransactionAsync())
        {
            var number = ContainerTestHelper.CreateValidContainerNumber("SUDU", 654321);
            var container = Container.Create(
                number, "22G1", ContainerSize.Ft20, ContainerType.Dry,
                2200m, 30480m, null, "SUD", tenant).Value!;

            transContext.Containers.Add(container);
            await transContext.SaveChangesAsync();

            // Explicitly roll back transaction
            await transaction.RollbackAsync();
        }

        await using (var verifyContext = _fixture.CreateContext(tenant))
        {
            var currentCount = await verifyContext.Containers.CountAsync();
            currentCount.Should().Be(initialCount);
        }
    }
}
