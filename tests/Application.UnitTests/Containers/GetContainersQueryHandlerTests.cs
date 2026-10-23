using SnpDepotManagement.Application.Features.Containers.Queries.GetContainers;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Entities.Yard;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.UnitTests.Containers;

public class GetContainersQueryHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly GetContainersQueryHandler _handler;

    public GetContainersQueryHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new GetContainersQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenFiltersAreApplied_ShouldReturnFilteredPagedResult()
    {
        var c1 = CreateContainer("MAE", "100001", ContainerSize.Ft20, "MAEU");
        var c2 = CreateContainer("MAE", "100002", ContainerSize.Ft40, "MAEU");
        var c3 = CreateContainer("ONE", "100003", ContainerSize.Ft20, "ONE");

        _context.Containers.AddRange(c1, c2, c3);
        await _context.SaveChangesAsync();

        var query = new GetContainersQuery(OwnerCode: "MAEU", Size: ContainerSize.Ft20, PageIndex: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().ContainerNumber.Should().Be(c1.ContainerNumber.Value);
    }

    [Fact]
    public async Task Handle_WhenSearchMatchesContainerNumber_ShouldReturnMatchedContainers()
    {
        var c1 = CreateContainer("CSQ", "305438", ContainerSize.Ft20, "CSQU");
        var c2 = CreateContainer("MAE", "100001", ContainerSize.Ft20, "MAEU");

        _context.Containers.AddRange(c1, c2);
        await _context.SaveChangesAsync();

        var query = new GetContainersQuery(Search: "305438", PageIndex: 1, PageSize: 10);

        var result = await _handler.Handle(query);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.First().ContainerNumber.Should().Be(c1.ContainerNumber.Value);
    }

    [Fact]
    public async Task Handle_WhenContainersHaveDifferentStatuses_ShouldComputeDisplayStatusCorrectly()
    {
        var utcNow = DateTime.UtcNow;
        var block = YardBlock.CreateStandard("A1", "Khu A1", 2, 2, 2);
        var slot = block.Slots.First();
        _context.YardBlocks.Add(block);

        // 1. InYard container
        var cInYard = CreateContainer("INY", "100001", ContainerSize.Ft20, "INYU");
        var lcInYard = ContainerLifecycle.GateIn(cInYard.Id, "INYU", Classification.A, ContainerCondition.Normal, "51C-11111", slot.Id, utcNow);

        // 2. Reserved container
        var cReserved = CreateContainer("RES", "200001", ContainerSize.Ft20, "RESU");
        var lcReserved = ContainerLifecycle.GateIn(cReserved.Id, "RESU", Classification.A, ContainerCondition.Normal, "51C-22222", slot.Id, utcNow);
        var order = DeliveryOrder.Create("ORD-RES-01", "RESU", "0101234567", "CUST", utcNow.AddDays(2), "VESSEL").Value!;
        _context.DeliveryOrders.Add(order);
        var booking = DeliveryOrderBooking.Reserve(order.Id, Guid.NewGuid(), cReserved.Id, "", utcNow);
        _context.Bookings.Add(booking);

        // 3. OutOfYard container
        var cOut = CreateContainer("OUT", "300001", ContainerSize.Ft20, "OUTU");
        var lcOut = ContainerLifecycle.GateIn(cOut.Id, "OUTU", Classification.A, ContainerCondition.Normal, "51C-33333", slot.Id, utcNow.AddDays(-2));
        lcOut.GateOut("51C-44444", order.Id, utcNow.AddDays(-1));

        // 4. NeverIn container
        var cNever = CreateContainer("NEV", "400001", ContainerSize.Ft20, "NEVU");

        _context.Containers.AddRange(cInYard, cReserved, cOut, cNever);
        _context.ContainerLifecycles.AddRange(lcInYard, lcReserved, lcOut);
        await _context.SaveChangesAsync();

        var query = new GetContainersQuery(PageIndex: 1, PageSize: 10);
        var result = await _handler.Handle(query);

        result.IsSuccess.Should().BeTrue();
        var items = result.Value!.Items;

        var dtoInYard = items.First(x => x.Id == cInYard.Id);
        dtoInYard.DisplayStatus.Should().Be("InYard");
        dtoInYard.IsInYard.Should().BeTrue();
        dtoInYard.CurrentSlot.Should().NotBeNull();
        dtoInYard.CurrentSlot!.BlockCode.Should().Be("A1");

        var dtoReserved = items.First(x => x.Id == cReserved.Id);
        dtoReserved.DisplayStatus.Should().Be("Reserved");
        dtoReserved.CurrentBooking.Should().NotBeNull();
        dtoReserved.CurrentBooking!.OrderNumber.Should().Be("ORD-RES-01");

        var dtoOut = items.First(x => x.Id == cOut.Id);
        dtoOut.DisplayStatus.Should().Be("OutOfYard");
        dtoOut.IsInYard.Should().BeFalse();

        var dtoNever = items.First(x => x.Id == cNever.Id);
        dtoNever.DisplayStatus.Should().Be("NeverIn");
        dtoNever.IsInYard.Should().BeFalse();
    }

    private static Container CreateContainer(string ownerPrefix, string serialNumber, ContainerSize size, string ownerCode)
    {
        string raw = $"{ownerPrefix}U{serialNumber}";
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = raw[i];
            int val = char.IsLetter(c) ? (c - 'A' + 10) : (c - '0');
            if (char.IsLetter(c))
            {
                if (val >= 11) val++;
                if (val >= 22) val++;
                if (val >= 33) val++;
            }
            sum += val * (1 << i);
        }
        int rem = sum % 11;
        int checkDigit = rem == 10 ? 0 : rem;

        var number = ContainerNumber.Create($"{raw}{checkDigit}").Value!;
        return Container.Create(number, "22G1", size, ContainerType.Dry, 2200, 30480, null, ownerCode).Value!;
    }
}
