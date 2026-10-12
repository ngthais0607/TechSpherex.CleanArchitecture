using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Infrastructure.IntegrationTests.Fixtures;
using Xunit;

namespace SnpDepotManagement.Infrastructure.IntegrationTests.Operations;

[Collection("PostgreSqlCollection")]
public sealed class YardSlotConcurrencyTests
{
    private readonly PostgreSqlFixture _fixture;

    public YardSlotConcurrencyTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TwoConcurrentTransactions_OccupySameSlot_ShouldAllowOnlyOneAndThrowConcurrencyExceptionOnSecond()
    {
        var tenantId = "CONCURRENCY-DEPOT";
        var block = YardBlock.CreateStandard("BLK", "Block Concurrency", 1, 1, 1, tenantId);

        // Setup slot in DB
        await using (var setupContext = _fixture.CreateContext(tenantId))
        {
            setupContext.YardBlocks.Add(block);
            await setupContext.SaveChangesAsync();
        }

        var slotId = block.Slots.First().Id;
        var containerA = Guid.NewGuid();
        var containerB = Guid.NewGuid();

        // 2 independent DbContexts loaded the slot at the same time
        await using var contextA = _fixture.CreateContext(tenantId);
        await using var contextB = _fixture.CreateContext(tenantId);

        var slotInContextA = await contextA.YardSlots.FirstAsync(s => s.Id == slotId);
        var slotInContextB = await contextB.YardSlots.FirstAsync(s => s.Id == slotId);

        // Context A occupies and saves
        slotInContextA.Occupy(containerA);
        await contextA.SaveChangesAsync();

        // Context B attempts to occupy and save after A has already modified xmin
        slotInContextB.Occupy(containerB);

        var act = () => contextB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
