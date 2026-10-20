using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Infrastructure.Persistence.Seed;

public interface IDemoDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class DemoDataSeeder : IDemoDataSeeder
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<DemoDataSeeder> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var enabled = string.Equals(_configuration["Seed:DemoData"], "true", StringComparison.OrdinalIgnoreCase);
        if (!enabled)
        {
            return;
        }

        var anyBlocks = await _context.YardBlocks.AnyAsync(cancellationToken);
        if (anyBlocks)
        {
            _logger.LogInformation("Demo data already seeded, skipping.");
            return;
        }

        var tenantId = _configuration["Admin:TenantId"] ?? "DEPOT-A";
        var now = DateTime.UtcNow;

        _logger.LogInformation("Starting Demo Data Seeding for tenant {TenantId}...", tenantId);

        // 1. LineOperators & OwnerCodes
        var lineOpMsc = LineOperator.Create("MSC", "Mediterranean Shipping Company").Value!;
        var lineOpMaeu = LineOperator.Create("MAEU", "Maersk Line").Value!;
        _context.LineOperators.AddRange(lineOpMsc, lineOpMaeu);

        var ownerCodes = new List<LineOperatorOwnerCode>
        {
            LineOperatorOwnerCode.Create(lineOpMsc.Id, "MSC", tenantId),
            LineOperatorOwnerCode.Create(lineOpMsc.Id, "MED", tenantId),
            LineOperatorOwnerCode.Create(lineOpMaeu.Id, "MAE", tenantId),
            LineOperatorOwnerCode.Create(lineOpMaeu.Id, "MSK", tenantId)
        };
        _context.LineOperatorOwnerCodes.AddRange(ownerCodes);

        // 2. Customers
        var cust1 = Customer.Create("0102030405", "Samsung Electronics Vietnam", tenantId).Value!;
        var cust2 = Customer.Create("0109876543", "VinFast Trading & Production", tenantId).Value!;
        var cust3 = Customer.Create("0105556667", "Hoa Phat Steel Corporation", tenantId).Value!;
        _context.Customers.AddRange(cust1, cust2, cust3);

        // 3. YardBlocks
        var blockA = YardBlock.CreateStandard("A", "Standard Block A", 10, 6, 4, tenantId);
        var blockB = YardBlock.CreateStandard("B", "Standard Block B", 10, 6, 4, tenantId);
        var blockVb = YardBlock.CreateVirtual("VB-01", "Khu chờ phân vị trí 1", tenantId);
        _context.YardBlocks.AddRange(blockA, blockB, blockVb);

        // 4. DeliveryOrders
        var do1 = DeliveryOrder.Create("DO-2026-001", "MSC", cust1.TaxCode, cust1.Name, now.AddDays(15), "MSC OSCAR", tenantId).Value!;
        do1.AddItem(ContainerSize.Ft20, ContainerType.Dry, 5);

        var do2 = DeliveryOrder.Create("DO-2026-002", "MAEU", cust2.TaxCode, cust2.Name, now.AddDays(10), "MAERSK MC-KINNEY MOLLER", tenantId).Value!;
        do2.AddItem(ContainerSize.Ft40, ContainerType.Dry, 4);

        var do3 = DeliveryOrder.Create("DO-2026-003", "MSC", cust1.TaxCode, cust1.Name, now.AddDays(2), "MSC GULSUN", tenantId).Value!;
        do3.AddItem(ContainerSize.Ft20, ContainerType.Reefer, 3);

        var do4 = DeliveryOrder.Create("DO-2026-004", "MAEU", cust3.TaxCode, cust3.Name, now.AddDays(-5), "MADRID MAERSK", tenantId).Value!;
        do4.AddItem(ContainerSize.Ft20, ContainerType.Dry, 4);

        var do5 = DeliveryOrder.Create("DO-2026-005", "MSC", cust2.TaxCode, cust2.Name, now.AddDays(5), "MSC ISABELLA", tenantId).Value!;
        do5.AddItem(ContainerSize.Ft20, ContainerType.Dry, 3);
        var do5Item = do5.Items.First();
        do5Item.Allocate(3);
        do5.MarkCompleted();

        _context.DeliveryOrders.AddRange(do1, do2, do3, do4, do5);
        await _context.SaveChangesAsync(cancellationToken);

        // 5. 50 Containers
        var containers = new List<Container>();
        var lifecycles = new List<ContainerLifecycle>();
        var bookings = new List<DeliveryOrderBooking>();
        var gateTxs = new List<GateTransaction>();

        var blockASlots = blockA.Slots.OrderBy(s => s.Bay).ThenBy(s => s.Row).ThenBy(s => s.Tier).ToList();

        for (int i = 0; i < 50; i++)
        {
            var isMsc = i < 25;
            var owner = isMsc ? (i % 2 == 0 ? "MSC" : "MED") : (i % 2 == 0 ? "MAE" : "MSK");
            var numStr = ContainerNumberGenerator.Generate(owner, "U");
            var contNum = ContainerNumber.Create(numStr).Value!;

            var is40Ft = i >= 35; // 15 cont 40ft (35..49), 35 cont 20ft (0..34)
            var size = is40Ft ? ContainerSize.Ft40 : ContainerSize.Ft20;
            var isReefer = i is >= 20 and < 25;
            var type = isReefer ? ContainerType.Reefer : ContainerType.Dry;
            var iso = is40Ft ? "42G1" : (isReefer ? "22R1" : "22G1");

            var cont = Container.Create(
                contNum,
                iso,
                size,
                type,
                2200m,
                30480m,
                new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Classification.A,
                ContainerCondition.Normal,
                tenantId).Value!;

            containers.Add(cont);
        }

        _context.Containers.AddRange(containers);
        await _context.SaveChangesAsync(cancellationToken);

        // Phân loại:
        // 0..19 (20 cont): InYard
        //   - 0..14 (15 cont): Nằm ở Slot của Block A
        //   - 15..19 (5 cont): Nằm ở Block ảo VB-01
        // 20..29 (10 cont): GatedOut (đã xuất)
        // 30..49 (20 cont): NeverIn (chưa vào)

        for (int i = 0; i < 20; i++)
        {
            var cont = containers[i];
            var lineOp = i < 25 ? "MSC" : "MAEU";

            if (i < 15)
            {
                var slot = blockASlots[i];
                slot.Occupy(cont.Id);
                var lc = ContainerLifecycle.GateIn(
                    cont.Id, lineOp, cont.Classification, cont.Condition,
                    $"29C-{10000 + i}", slot.Id, now.AddDays(-i), tenantId, VehicleType.Truck, null);
                lifecycles.Add(lc);

                gateTxs.Add(GateTransaction.CreateGateIn(
                    cont.Id, $"29C-{10000 + i}", VehicleType.Truck,
                    "Nguyen Van A", slot.Id, null, tenantId));
            }
            else
            {
                var lc = ContainerLifecycle.GateIn(
                    cont.Id, lineOp, cont.Classification, cont.Condition,
                    $"29C-{10000 + i}", null, now.AddDays(-i), tenantId, VehicleType.Truck, blockVb.Id);
                lifecycles.Add(lc);

                gateTxs.Add(GateTransaction.CreateGateIn(
                    cont.Id, $"29C-{10000 + i}", VehicleType.Truck,
                    "Nguyen Van A", null, blockVb.Code, tenantId));
            }
        }

        // 10 GatedOut (containers 20..29)
        for (int i = 20; i < 30; i++)
        {
            var cont = containers[i];
            var lineOp = i < 25 ? "MSC" : "MAEU";

            var lc = ContainerLifecycle.GateIn(
                cont.Id, lineOp, cont.Classification, cont.Condition,
                $"51D-{20000 + i}", null, now.AddDays(-20), tenantId, VehicleType.Truck, null);

            lc.GateOut($"51D-{30000 + i}", do5.Id, now.AddDays(-1), VehicleType.Truck);
            lifecycles.Add(lc);

            gateTxs.Add(GateTransaction.CreateGateOut(
                cont.Id, do5.Id, $"51D-{30000 + i}", VehicleType.Truck,
                "Tran Van B", null, tenantId));
        }

        _context.ContainerLifecycles.AddRange(lifecycles);
        _context.GateTransactions.AddRange(gateTxs);

        // Bookings
        // DO 1 (MSC 20ft Dry): 3 Reserved (cont 0, 1, 2)
        var do1Item = do1.Items.First();
        bookings.Add(DeliveryOrderBooking.Reserve(do1.Id, do1Item.Id, containers[0].Id, tenantId, now));
        bookings.Add(DeliveryOrderBooking.Reserve(do1.Id, do1Item.Id, containers[1].Id, tenantId, now));
        bookings.Add(DeliveryOrderBooking.Reserve(do1.Id, do1Item.Id, containers[2].Id, tenantId, now));

        // DO 2 (MAEU 40ft Dry): 2 Reserved (cont 35, 36)
        var do2Item = do2.Items.First();
        bookings.Add(DeliveryOrderBooking.Reserve(do2.Id, do2Item.Id, containers[35].Id, tenantId, now));
        bookings.Add(DeliveryOrderBooking.Reserve(do2.Id, do2Item.Id, containers[36].Id, tenantId, now));

        // DO 3 (MSC 20ft Reefer): 2 Reserved (cont 20, 21)
        var do3Item = do3.Items.First();
        bookings.Add(DeliveryOrderBooking.Reserve(do3.Id, do3Item.Id, containers[20].Id, tenantId, now));
        bookings.Add(DeliveryOrderBooking.Reserve(do3.Id, do3Item.Id, containers[21].Id, tenantId, now));

        // DO 4 (MAEU 20ft Dry - Expired): 3 Reserved (cont 25, 26, 27)
        var do4Item = do4.Items.First();
        bookings.Add(DeliveryOrderBooking.Reserve(do4.Id, do4Item.Id, containers[25].Id, tenantId, now.AddDays(-10)));
        bookings.Add(DeliveryOrderBooking.Reserve(do4.Id, do4Item.Id, containers[26].Id, tenantId, now.AddDays(-10)));
        bookings.Add(DeliveryOrderBooking.Reserve(do4.Id, do4Item.Id, containers[27].Id, tenantId, now.AddDays(-10)));

        // DO 5 (MSC 20ft Dry - Shipped): 3 Shipped (cont 22, 23, 24)
        for (int i = 22; i <= 24; i++)
        {
            var b = DeliveryOrderBooking.Reserve(do5.Id, do5Item.Id, containers[i].Id, tenantId, now.AddDays(-5));
            b.Ship(now.AddDays(-1));
            bookings.Add(b);
        }

        _context.Bookings.AddRange(bookings);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully completed Demo Data Seeding: 2 LineOperators, 3 Customers, 3 YardBlocks, 50 Containers, 5 DeliveryOrders.");
    }
}
