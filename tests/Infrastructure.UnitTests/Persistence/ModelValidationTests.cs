using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Identity;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Infrastructure.Persistence;
using Xunit;

namespace SnpDepotManagement.Infrastructure.UnitTests.Persistence;

public sealed class ModelValidationTests
{
    private readonly AppDbContext _context;

    public ModelValidationTests()
    {
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.GetCurrentTenantId().Returns("DEPOT-TEST");
        var timeProvider = Substitute.For<TimeProvider>();
        var currentUser = Substitute.For<ICurrentUser>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options, tenantProvider, timeProvider, currentUser);
    }

    [Fact]
    public void Model_ShouldContainAllExpectedEntityTypes()
    {
        var model = _context.Model;
        var entityTypes = model.GetEntityTypes().Select(e => e.ClrType).ToList();

        entityTypes.Should().Contain(typeof(ApplicationUser));
        entityTypes.Should().Contain(typeof(Role));
        entityTypes.Should().Contain(typeof(UserRole));
        entityTypes.Should().Contain(typeof(Customer));
        entityTypes.Should().Contain(typeof(LineOperator));
        entityTypes.Should().Contain(typeof(Voyage));
        entityTypes.Should().Contain(typeof(Vehicle));
        entityTypes.Should().Contain(typeof(Container));
        entityTypes.Should().Contain(typeof(YardBlock));
        entityTypes.Should().Contain(typeof(YardSlot));
        entityTypes.Should().Contain(typeof(DeliveryOrder));
        entityTypes.Should().Contain(typeof(DeliveryOrderItem));
        entityTypes.Should().Contain(typeof(ContainerLifecycle));
    }

    [Fact]
    public void Model_ShouldHaveExpectedIndexes()
    {
        var model = _context.Model;

        // Container unique index
        var containerType = model.FindEntityType(typeof(Container));
        containerType.Should().NotBeNull();
        var containerIndexes = containerType!.GetIndexes().ToList();
        containerIndexes.Should().Contain(i => i.IsUnique && i.Properties.Any(p => p.Name == "TenantId") && i.Properties.Any(p => p.Name == "ContainerNumber"));

        // DeliveryOrder unique index
        var orderType = model.FindEntityType(typeof(DeliveryOrder));
        orderType.Should().NotBeNull();
        var orderIndexes = orderType!.GetIndexes().ToList();
        orderIndexes.Should().Contain(i => i.IsUnique && i.Properties.Any(p => p.Name == "TenantId") && i.Properties.Any(p => p.Name == "OrderNumber"));

        // ContainerLifecycle index
        var lifecycleType = model.FindEntityType(typeof(ContainerLifecycle));
        lifecycleType.Should().NotBeNull();
        var lifecycleIndexes = lifecycleType!.GetIndexes().ToList();
        lifecycleIndexes.Should().Contain(i => i.IsUnique && i.Properties.Any(p => p.Name == "TenantId") && i.Properties.Any(p => p.Name == "ContainerId"));
    }

    [Fact]
    public void YardSlot_ShouldHavePositionAsOwnedType()
    {
        var slotType = _context.Model.FindEntityType(typeof(YardSlot));
        slotType.Should().NotBeNull();

        var positionNav = slotType!.FindNavigation(nameof(YardSlot.Position));
        positionNav.Should().NotBeNull();
        positionNav!.TargetEntityType.IsOwned().Should().BeTrue();
    }
}
