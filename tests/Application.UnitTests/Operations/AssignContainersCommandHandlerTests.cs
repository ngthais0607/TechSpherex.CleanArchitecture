using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Features.Operations.Bookings.Commands.AssignContainers;
using SnpDepotManagement.Application.Features.Operations.Bookings.Commands.ReleaseBooking;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Containers;
using SnpDepotManagement.Domain.Entities.MasterData;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.ValueObjects;

namespace SnpDepotManagement.Application.UnitTests.Operations;

public class AssignContainersCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly AssignContainersToOrderCommandHandler _assignHandler;
    private readonly ReleaseBookingCommandHandler _releaseHandler;
    private readonly TimeProvider _timeProvider;
    private readonly DateTimeOffset _utcNow = new(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);

    public AssignContainersCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _timeProvider = new FixedTimeProvider(_utcNow);
        _assignHandler = new AssignContainersToOrderCommandHandler(_context, _timeProvider);
        _releaseHandler = new ReleaseBookingCommandHandler(_context, _timeProvider);
    }

    private static Container CreateValidContainer(string ownerPrefix, string serialNumber, ContainerSize size)
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
        string isoType = size == ContainerSize.Ft40 ? "42G1" : "22G1";
        return Container.Create(number, isoType, size, ContainerType.Dry, 2200, 30480, null, ownerPrefix).Value!;
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ShouldReturnNotFound()
    {
        var command = new AssignContainersToOrderCommand("DO-NONEXISTENT", new[] { "MSKU1234567" });
        var result = await _assignHandler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("DeliveryOrder.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenOrderExpired_ShouldReturnFailure()
    {
        var order = DeliveryOrder.Create("DO-EXP", "MSK", "0123456789", "Test Corp", _utcNow.UtcDateTime.AddDays(-1), "VESSEL1").Value!;
        _context.DeliveryOrders.Add(order);
        await _context.SaveChangesAsync();

        var command = new AssignContainersToOrderCommand("DO-EXP", new[] { "MSKU1234567" });
        var result = await _assignHandler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("OrderRule.DeliveryOrderExpired", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenContainerNotInYard_ShouldReturnConflict()
    {
        var order = DeliveryOrder.Create("DO-YARD", "MSK", "0123456789", "Test Corp", _utcNow.UtcDateTime.AddDays(5), "VESSEL1").Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1);
        _context.DeliveryOrders.Add(order);

        var container = CreateValidContainer("MSK", "100001", ContainerSize.Ft20);
        _context.Containers.Add(container);
        await _context.SaveChangesAsync();

        var command = new AssignContainersToOrderCommand("DO-YARD", new[] { container.ContainerNumber.Value });
        var result = await _assignHandler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Lifecycle.NotInYard", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenValidContainers_ShouldCreateBookingsAndReturnSuccess()
    {
        // Line operator
        var lineOp = LineOperator.Create("MSK", "Maersk").Value!;
        _context.LineOperators.Add(lineOp);

        var order = DeliveryOrder.Create("DO-VALID", "MSK", "0123456789", "Test Corp", _utcNow.UtcDateTime.AddDays(5), "VESSEL1").Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 2);
        _context.DeliveryOrders.Add(order);

        var c1 = CreateValidContainer("MSK", "100001", ContainerSize.Ft20);
        var c2 = CreateValidContainer("MSK", "100002", ContainerSize.Ft20);
        _context.Containers.AddRange(c1, c2);

        var l1 = ContainerLifecycle.GateIn(c1.Id, "MSK", Classification.A, ContainerCondition.Normal, "29C-12345", Guid.NewGuid(), _utcNow.UtcDateTime);
        var l2 = ContainerLifecycle.GateIn(c2.Id, "MSK", Classification.A, ContainerCondition.Normal, "29C-12346", Guid.NewGuid(), _utcNow.UtcDateTime);
        _context.ContainerLifecycles.AddRange(l1, l2);
        await _context.SaveChangesAsync();

        var command = new AssignContainersToOrderCommand("DO-VALID", new[] { c1.ContainerNumber.Value, c2.ContainerNumber.Value });
        var result = await _assignHandler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.AssignedCount);
        Assert.Equal(2, result.Value.BookingIds.Count);

        var bookings = await _context.Bookings.Where(b => b.OrderId == order.Id).ToListAsync();
        Assert.Equal(2, bookings.Count);
        Assert.All(bookings, b => Assert.Equal(BookingStatus.Reserved, b.Status));
    }

    [Fact]
    public async Task Handle_WhenQuotaExceeded_ShouldReturnConflict()
    {
        var lineOp = LineOperator.Create("MSK", "Maersk").Value!;
        _context.LineOperators.Add(lineOp);

        var order = DeliveryOrder.Create("DO-QUOTA", "MSK", "0123456789", "Test Corp", _utcNow.UtcDateTime.AddDays(5), "VESSEL1").Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 1); // Quota is only 1
        _context.DeliveryOrders.Add(order);

        var c1 = CreateValidContainer("MSK", "100001", ContainerSize.Ft20);
        var c2 = CreateValidContainer("MSK", "100002", ContainerSize.Ft20);
        _context.Containers.AddRange(c1, c2);

        var l1 = ContainerLifecycle.GateIn(c1.Id, "MSK", Classification.A, ContainerCondition.Normal, "29C-12345", Guid.NewGuid(), _utcNow.UtcDateTime);
        var l2 = ContainerLifecycle.GateIn(c2.Id, "MSK", Classification.A, ContainerCondition.Normal, "29C-12346", Guid.NewGuid(), _utcNow.UtcDateTime);
        _context.ContainerLifecycles.AddRange(l1, l2);
        await _context.SaveChangesAsync();

        var command = new AssignContainersToOrderCommand("DO-QUOTA", new[] { c1.ContainerNumber.Value, c2.ContainerNumber.Value });
        var result = await _assignHandler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Booking.QuotaExceeded", result.Error.Code);
    }

    [Fact]
    public async Task ReleaseBooking_WhenReserved_ShouldCancelBooking()
    {
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var containerId = Guid.NewGuid();
        var booking = DeliveryOrderBooking.Reserve(orderId, itemId, containerId, "tenant1", _utcNow.UtcDateTime);
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _releaseHandler.Handle(new ReleaseBookingCommand(booking.Id));
        Assert.True(result.IsSuccess);

        var updated = await _context.Bookings.FindAsync(booking.Id);
        Assert.NotNull(updated);
        Assert.Equal(BookingStatus.Cancelled, updated.Status);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
