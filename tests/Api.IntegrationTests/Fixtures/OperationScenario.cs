using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Api.IntegrationTests.Fixtures;

internal sealed record OperationScenario(YardBlock Block, Container First, Container Second, Container Third, DeliveryOrder Order)
{
    private static int _serial = 400000;

    public static Container NewContainer()
    {
        var value = $"MSKU{Interlocked.Increment(ref _serial):D6}";
        var sum = 0;
        for (var i = 0; i < value.Length; i++)
        {
            var n = char.IsDigit(value[i]) ? value[i] - '0' : value[i] - 'A' + 10;
            if (char.IsLetter(value[i])) { if (n >= 11) n++; if (n >= 22) n++; if (n >= 33) n++; }
            sum += n * (1 << i);
        }
        var number = ContainerNumber.Create(value + (sum % 11 % 10)).Value!;
        return Container.Create(number, "22G1", ContainerSize.Ft20, ContainerType.Dry, 2000, 30000, null, "MSK", "DEPOT-A").Value!;
    }

    public static async Task<OperationScenario> CreateAsync(ApiFactory factory, bool placeFirst = true, bool placeSecond = false)
    {
        var ct = Xunit.TestContext.Current.CancellationToken;
        await using var db = factory.Context("DEPOT-A");
        var block = YardBlock.CreateStandard("B" + Guid.NewGuid().ToString("N")[..6], "HTTP test", 5, 1, 2, "DEPOT-A");
        var first = NewContainer(); var second = NewContainer(); var third = NewContainer();
        db.YardBlocks.Add(block); db.Containers.AddRange(first, second, third);
        var order = DeliveryOrder.Create("D" + Guid.NewGuid().ToString("N")[..8], "MSK", "0101234567", "Customer", DateTime.UtcNow.AddDays(7), "Vessel", "DEPOT-A").Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);
        db.DeliveryOrders.Add(order);
        foreach (var (container, bay, placed) in new[] { (first, 1, placeFirst), (second, 3, placeSecond) })
        {
            var slot = block.Slots.First(s => s.Bay == bay && s.Tier == 1);
            var lifecycle = ContainerLifecycle.GateIn(container.Id, "MSK", Classification.A, ContainerCondition.Normal, "TRUCK", slot.Id, DateTime.UtcNow, "DEPOT-A");
            if (placed) slot.Occupy(container.Id); else lifecycle.SetCurrentSlot(null);
            db.ContainerLifecycles.Add(lifecycle);
        }
        await db.SaveChangesAsync(ct);
        return new(block, first, second, third, order);
    }
}
