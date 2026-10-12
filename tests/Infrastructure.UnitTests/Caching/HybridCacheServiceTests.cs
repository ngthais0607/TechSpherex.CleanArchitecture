using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Infrastructure.Caching;
using Xunit;

namespace SnpDepotManagement.Infrastructure.UnitTests.Caching;

public sealed class HybridCacheServiceTests
{
    private readonly ITenantProvider _tenantProvider;
    private readonly HybridCacheService _sut;

    public HybridCacheServiceTests()
    {
        _tenantProvider = Substitute.For<ITenantProvider>();
        _tenantProvider.GetCurrentTenantId().Returns("DEPOT-A");

        var services = new ServiceCollection();
#pragma warning disable EXTEXP0018
        services.AddHybridCache();
#pragma warning restore EXTEXP0018
        var sp = services.BuildServiceProvider();
        var hybridCache = sp.GetRequiredService<HybridCache>();

        _sut = new HybridCacheService(hybridCache, _tenantProvider, redis: null);
    }

    [Fact]
    public async Task SetAsync_And_GetAsync_ShouldStoreAndRetrieveValue()
    {
        var key = "container:TEMU1234567";
        var expectedValue = new TestCacheItem { Id = Guid.NewGuid(), Name = "Dry Container" };

        await _sut.SetAsync(key, expectedValue);
        var actual = await _sut.GetAsync<TestCacheItem>(key);

        actual.Should().NotBeNull();
        actual!.Id.Should().Be(expectedValue.Id);
        actual.Name.Should().Be(expectedValue.Name);
    }

    [Fact]
    public async Task RemoveAsync_ShouldInvalidateCachedValue()
    {
        var key = "slot:SLOT-01";
        await _sut.SetAsync(key, "SlotOccupied");

        var before = await _sut.GetAsync<string>(key);
        before.Should().Be("SlotOccupied");

        await _sut.RemoveAsync(key);
        var after = await _sut.GetAsync<string>(key);

        after.Should().BeNull();
    }

    [Fact]
    public async Task RemoveByPrefixAsync_ShouldInvalidateTaggedEntries()
    {
        var prefix = "orders";
        var key = "orders:DO-12345";
        await _sut.SetAsync(key, "Pending");

        await _sut.RemoveByPrefixAsync(prefix);
        var after = await _sut.GetAsync<string>(key);

        // In HybridCache, RemoveByTagAsync invalidates entries associated with the tag
        // Since prefix is used as tag, it should be removed or handled gracefully
        after.Should().BeNull();
    }

    private sealed class TestCacheItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
