using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SnpDepotManagement.Application.Abstractions.Identity;
using SnpDepotManagement.Application.Abstractions.Tenancy;
using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Infrastructure.Persistence.Seed;

public static class DemoDatasetWriter
{
    public static async Task WriteAsync(AppDbContext db, string tenantId, DateTime baselineUtc, CancellationToken ct)
    {
        var transaction = db.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Demo dataset must be written inside a transaction.");
        var clock = new SeedTimeProvider(baselineUtc.AddDays(-30));
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(db.Database.GetDbConnection()).UseSnakeCaseNamingConvention().Options;
        await using var historical = new AppDbContext(options, new SeedTenant(tenantId), clock, new SeedActor());
        await historical.Database.UseTransactionAsync(transaction.GetDbTransaction(), ct);
        await WriteDataAsync(historical, tenantId, baselineUtc, clock, ct);
    }

    private static async Task WriteDataAsync(AppDbContext db, string tenant, DateTime now, SeedTimeProvider clock, CancellationToken ct)
    {
        foreach (var (code, name, owners) in new[]
        {
            ("MSC", "Mediterranean Shipping Company", new[] { "MSC", "MED" }),
            ("MAEU", "Maersk Line", new[] { "MAE", "MSK" })
        })
        {
            var op = await db.LineOperators.SingleOrDefaultAsync(item => item.Code == code, ct);
            if (op is null)
            {
                op = Require(LineOperator.Create(code, name));
                db.LineOperators.Add(op);
            }
            else if (!op.IsActive)
                throw new InvalidOperationException($"Demo operator {code} is inactive. Use a separate development database.");
            foreach (var owner in owners)
                db.LineOperatorOwnerCodes.Add(LineOperatorOwnerCode.Create(op.Id, owner, tenant));
        }
        var customers = new[]
        {
            Require(Customer.Create("0102030405", "Samsung Electronics Vietnam", tenant)),
            Require(Customer.Create("0109876543", "VinFast Trading & Production", tenant)),
            Require(Customer.Create("0105556667", "Hoa Phat Steel Corporation", tenant))
        };
        db.Customers.AddRange(customers);
        var blockA = YardBlock.CreateStandard("A", "Standard Block A", 10, 6, 4, tenant);
        var blockB = YardBlock.CreateStandard("B", "Standard Block B", 10, 6, 4, tenant);
        var virtualBlock = YardBlock.CreateVirtual("VB-01", "Khu chờ phân vị trí 1", tenant);
        db.YardBlocks.AddRange(blockA, blockB, virtualBlock);

        var orders = new Dictionary<string, DeliveryOrder>
        {
            ["DO-2026-001"] = Order("DO-2026-001", "MSC", customers[0], 15, ContainerSize.Ft20, ContainerType.Dry, 5),
            ["DO-2026-002"] = Order("DO-2026-002", "MAEU", customers[1], 10, ContainerSize.Ft40, ContainerType.Dry, 4),
            ["DO-2026-003"] = Order("DO-2026-003", "MSC", customers[0], 2, ContainerSize.Ft20, ContainerType.Reefer, 3),
            ["DO-2026-004"] = Order("DO-2026-004", "MAEU", customers[2], -5, ContainerSize.Ft20, ContainerType.Dry, 4),
            ["DO-2026-005"] = Order("DO-2026-005", "MSC", customers[1], 5, ContainerSize.Ft20, ContainerType.Dry, 3)
        };
        db.DeliveryOrders.AddRange(orders.Values);
        var containers = DemoDataset.Containers.ToDictionary(spec => spec.Index, spec => Require(Container.Create(
            Require(ContainerNumber.Create(spec.Number)),
            spec.Size == ContainerSize.Ft40 ? "42G1" : spec.Type == ContainerType.Reefer ? "22R1" : "22G1",
            spec.Size, spec.Type, 2200m, 30480m, new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Classification.A, ContainerCondition.Normal, tenant)));
        db.Containers.AddRange(containers.Values);
        await db.SaveChangesAsync(ct);

        var physicalSlots = blockA.Slots.Where(slot => slot.Tier == 1).OrderBy(slot => slot.Bay).ThenBy(slot => slot.Row).ToArray();
        var shippedSlots = blockB.Slots.Where(slot => slot.Tier == 1).OrderBy(slot => slot.Bay).ThenBy(slot => slot.Row).ToArray();
        var lifecycles = new Dictionary<int, ContainerLifecycle>();
        var historicalSlots = new Dictionary<int, YardSlot>();
        var physicalIndex = 0;
        var shippedIndex = 0;
        foreach (var spec in DemoDataset.Containers.Where(spec => spec.Scenario != DemoContainerScenario.NeverIn))
        {
            var container = containers[spec.Index];
            var inDate = now.AddDays(-20).AddMinutes(spec.Index * 2);
            clock.SetUtcNow(inDate);
            var vehicle = $"29C-{10000 + spec.Index}";
            var lifecycle = ContainerLifecycle.GateIn(container.Id, spec.LineOperator, container.Classification,
                container.Condition, vehicle, null, inDate, tenant, VehicleType.Truck, virtualBlock.Id);
            lifecycles.Add(spec.Index, lifecycle);
            db.ContainerLifecycles.Add(lifecycle);
            db.GateTransactions.Add(GateTransaction.CreateGateIn(container.Id, vehicle, VehicleType.Truck,
                "Nguyen Van A", null, virtualBlock.Code, tenant));
            Movement(container, lifecycle, ContainerMovementType.GateIn, inDate, null,
                new MovementPosition(virtualBlock.Id, virtualBlock.Code, 0, 0, 0), vehicle);
            await db.SaveChangesAsync(ct);
            if (spec.Scenario is DemoContainerScenario.Physical or DemoContainerScenario.Shipped)
            {
                var slot = spec.Scenario == DemoContainerScenario.Physical
                    ? physicalSlots[physicalIndex++] : shippedSlots[shippedIndex++];
                historicalSlots.Add(spec.Index, slot);
                slot.Occupy(container.Id);
                lifecycle.SetCurrentSlot(slot.Id);
                clock.SetUtcNow(inDate.AddMinutes(1));
                Movement(container, lifecycle, ContainerMovementType.Place, inDate.AddMinutes(1), null,
                    new MovementPosition(slot.BlockId, spec.Scenario == DemoContainerScenario.Physical ? blockA.Code : blockB.Code,
                        slot.Bay, slot.Row, slot.Tier));
                await db.SaveChangesAsync(ct);
            }
        }

        foreach (var spec in DemoDataset.Containers.Where(spec => spec.OrderNumber is not null))
        {
            var order = orders[spec.OrderNumber!];
            var reservedAt = now.AddDays(spec.OrderNumber == "DO-2026-004" ? -10 : -5);
            var booking = DeliveryOrderBooking.Reserve(order.Id, order.Items.Single().Id,
                containers[spec.Index].Id, tenant, reservedAt);
            db.Bookings.Add(booking);
            if (spec.Scenario == DemoContainerScenario.Shipped)
            {
                var outDate = now.AddDays(-1).AddMinutes(spec.Index);
                clock.SetUtcNow(outDate);
                var lifecycle = lifecycles[spec.Index];
                var slot = historicalSlots[spec.Index];
                var vehicle = $"51D-{30000 + spec.Index}";
                Check(lifecycle.GateOut(vehicle, order.Id, outDate, VehicleType.Truck));
                slot.Release();
                booking.Ship(outDate);
                Check(order.Items.Single().Allocate());
                db.GateTransactions.Add(GateTransaction.CreateGateOut(containers[spec.Index].Id, order.Id,
                    vehicle, VehicleType.Truck, "Tran Van B", slot.Id, tenant));
                Movement(containers[spec.Index], lifecycle, ContainerMovementType.GateOut, outDate,
                    new MovementPosition(slot.BlockId, blockB.Code, slot.Bay, slot.Row, slot.Tier), null, vehicle, order.Id);
                await db.SaveChangesAsync(ct);
            }
        }
        clock.SetUtcNow(now);
        orders["DO-2026-005"].MarkCompleted();
        await db.SaveChangesAsync(ct);

        DeliveryOrder Order(string number, string op, Customer customer, int days, ContainerSize size, ContainerType type, int quantity)
        {
            var order = Require(DeliveryOrder.Create(number, op, customer.TaxCode, customer.Name,
                now.AddDays(days), $"{op} DEMO VESSEL", tenant));
            Check(order.AddItem(size, type, quantity));
            return order;
        }

        void Movement(Container container, ContainerLifecycle lifecycle, ContainerMovementType kind, DateTime at,
            MovementPosition? source, MovementPosition? destination, string? vehicle = null, Guid? orderId = null)
        {
            db.ContainerMovements.Add(Require(ContainerMovement.Create(container.Id, lifecycle.Id, kind, at,
                null, source, destination, tenant, vehicle, vehicle is null ? null : VehicleType.Truck, orderId)));
        }
    }

    private static T Require<T>(Result<T> result) => result.IsSuccess && result.Value is { } value
        ? value : throw new InvalidOperationException($"Invalid demo dataset: {result.Error.Code}: {result.Error.Description}");
    private static void Check(Result result)
    {
        if (result.IsFailure) throw new InvalidOperationException($"Invalid demo dataset: {result.Error.Code}: {result.Error.Description}");
    }

    private sealed class SeedTimeProvider(DateTime initialUtc) : TimeProvider
    {
        private DateTime _utcNow = initialUtc;
        public void SetUtcNow(DateTime utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow);
    }
    private sealed class SeedTenant(string tenantId) : ITenantProvider
    {
        public string? GetCurrentTenantId() => tenantId;
        public TenantInfo? GetCurrentTenant() => new(tenantId, tenantId);
    }
    private sealed class SeedActor : ICurrentUser
    {
        public Guid? UserId => null;
        public string? TenantId => null;
        public string? Role => null;
        public bool IsAuthenticated => false;
    }
}
