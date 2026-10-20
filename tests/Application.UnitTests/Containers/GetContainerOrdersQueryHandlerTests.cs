using SnpDepotManagement.Application.Features.Containers.Queries.GetContainerOrders;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.UnitTests.Containers;

public class GetContainerOrdersQueryHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly GetContainerOrdersQueryHandler _handler;

    public GetContainerOrdersQueryHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new GetContainerOrdersQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenContainerHasBookings_ShouldReturnCurrentOrderAndHistory()
    {
        var utcNow = DateTime.UtcNow;
        var container = CreateContainer("MAE", "100001", ContainerSize.Ft20, "MAEU");
        _context.Containers.Add(container);

        var pastOrder = DeliveryOrder.Create("ORD-PAST-01", "MAEU", "0101234567", "Old Customer", utcNow.AddDays(-5), "OLD VESSEL").Value!;
        var currentOrder = DeliveryOrder.Create("ORD-CURR-01", "MAEU", "0101234567", "Current Customer", utcNow.AddDays(5), "NEW VESSEL").Value!;
        _context.DeliveryOrders.AddRange(pastOrder, currentOrder);

        var pastBooking = DeliveryOrderBooking.Reserve(pastOrder.Id, Guid.NewGuid(), container.Id, "", utcNow.AddDays(-9));
        pastBooking.Ship(utcNow.AddDays(-6));

        var currentBooking = DeliveryOrderBooking.Reserve(currentOrder.Id, Guid.NewGuid(), container.Id, "", utcNow);

        _context.Bookings.AddRange(pastBooking, currentBooking);
        await _context.SaveChangesAsync();

        var query = new GetContainerOrdersQuery(container.ContainerNumber.Value);
        var result = await _handler.Handle(query);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!;

        dto.CurrentOrder.Should().NotBeNull();
        dto.CurrentOrder!.OrderNumber.Should().Be("ORD-CURR-01");
        dto.CurrentOrder.CustomerName.Should().Be("Current Customer");

        dto.History.Should().HaveCount(1);
        dto.History[0].OrderNumber.Should().Be("ORD-PAST-01");
        dto.History[0].Status.Should().Be("Shipped");

        dto.Summary.ShipmentCount.Should().Be(1);
        dto.Summary.CustomerCount.Should().Be(2);
        dto.Summary.LastCustomer.Should().Be("Old Customer");
    }

    [Fact]
    public async Task Handle_WhenContainerNotFound_ShouldReturnNotFoundError()
    {
        var query = new GetContainerOrdersQuery("CSQU3054383");
        var result = await _handler.Handle(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.NotFound");
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
