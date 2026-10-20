using Microsoft.EntityFrameworkCore;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Commands.CreateDeliveryOrder;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Dtos;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.UnitTests.Operations;

public class CreateDeliveryOrderCommandHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly CreateDeliveryOrderCommandHandler _handler;

    public CreateDeliveryOrderCommandHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new CreateDeliveryOrderCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_ShouldCreateOrderWithItems()
    {
        // Arrange
        _context.LineOperators.Add(Domain.Entities.MasterData.LineOperator.Create("MAEU", "Maersk Line").Value!);
        _context.Customers.Add(Domain.Entities.MasterData.Customer.Create("0101234567", "Db Customer Name").Value!);
        await _context.SaveChangesAsync();

        var command = new CreateDeliveryOrderCommand(
            "DO-999888",
            "MAEU",
            "0101234567",
            "Ignored Input Name",
            DateTime.UtcNow.AddDays(14),
            "EVER GIVEN",
            new List<CreateDeliveryOrderItemInput>
            {
                new(ContainerSize.Ft20, ContainerType.Dry, 10),
                new(ContainerSize.Ft40, ContainerType.Dry, 5)
            });

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var orderInDb = await _context.DeliveryOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == result.Value);

        orderInDb.Should().NotBeNull();
        orderInDb!.OrderNumber.Should().Be("DO-999888");
        orderInDb.LineOperator.Should().Be("MAEU");
        orderInDb.CustomerName.Should().Be("Db Customer Name");
        orderInDb.IsCompleted.Should().BeFalse();
        orderInDb.Items.Should().HaveCount(2);
        orderInDb.Items.Sum(i => i.RequiredQuantity).Should().Be(15);
    }

    [Fact]
    public async Task Handle_WhenOrderNumberAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        _context.LineOperators.Add(Domain.Entities.MasterData.LineOperator.Create("MAEU", "Maersk Line").Value!);
        _context.LineOperators.Add(Domain.Entities.MasterData.LineOperator.Create("ONE", "Ocean Network Express").Value!);
        _context.Customers.Add(Domain.Entities.MasterData.Customer.Create("0101234567", "Cong Ty A").Value!);
        _context.Customers.Add(Domain.Entities.MasterData.Customer.Create("0109999999", "Cong Ty B").Value!);
        await _context.SaveChangesAsync();

        var command1 = new CreateDeliveryOrderCommand(
            "DO-DUP001",
            "MAEU",
            "0101234567",
            "Cong Ty A",
            DateTime.UtcNow.AddDays(7),
            "VESSEL-1",
            new List<CreateDeliveryOrderItemInput> { new(ContainerSize.Ft20, ContainerType.Dry, 2) });

        var res1 = await _handler.Handle(command1);
        res1.IsSuccess.Should().BeTrue();

        var command2 = new CreateDeliveryOrderCommand(
            "do-dup001", // case-insensitive duplicate
            "ONE",
            "0109999999",
            "Cong Ty B",
            DateTime.UtcNow.AddDays(7),
            "VESSEL-2",
            new List<CreateDeliveryOrderItemInput> { new(ContainerSize.Ft20, ContainerType.Dry, 1) });

        // Act
        var result = await _handler.Handle(command2);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeliveryOrder.AlreadyExists");
    }

    [Fact]
    public async Task Handle_WhenLineOperatorUnknown_ShouldReturnError()
    {
        _context.Customers.Add(Domain.Entities.MasterData.Customer.Create("0101234567", "Customer").Value!);
        await _context.SaveChangesAsync();

        var command = new CreateDeliveryOrderCommand(
            "DO-UNKNOWN-LINE",
            "UNKNOWN",
            "0101234567",
            "Customer",
            DateTime.UtcNow.AddDays(7),
            "VESSEL",
            new List<CreateDeliveryOrderItemInput> { new(ContainerSize.Ft20, ContainerType.Dry, 1) });

        var result = await _handler.Handle(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderRule.UnknownLineOperator");
    }

    [Fact]
    public async Task Handle_WhenCustomerUnknown_ShouldReturnError()
    {
        _context.LineOperators.Add(Domain.Entities.MasterData.LineOperator.Create("MAEU", "Maersk").Value!);
        await _context.SaveChangesAsync();

        var command = new CreateDeliveryOrderCommand(
            "DO-UNKNOWN-CUST",
            "MAEU",
            "9999999999",
            "Unknown",
            DateTime.UtcNow.AddDays(7),
            "VESSEL",
            new List<CreateDeliveryOrderItemInput> { new(ContainerSize.Ft20, ContainerType.Dry, 1) });

        var result = await _handler.Handle(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderRule.UnknownCustomer");
    }

    [Fact]
    public async Task Handle_WhenExpiryInPast_ShouldReturnError()
    {
        _context.LineOperators.Add(Domain.Entities.MasterData.LineOperator.Create("MAEU", "Maersk").Value!);
        _context.Customers.Add(Domain.Entities.MasterData.Customer.Create("0101234567", "Customer").Value!);
        await _context.SaveChangesAsync();

        var command = new CreateDeliveryOrderCommand(
            "DO-PAST-EXP",
            "MAEU",
            "0101234567",
            "Customer",
            DateTime.UtcNow.AddDays(-2),
            "VESSEL",
            new List<CreateDeliveryOrderItemInput> { new(ContainerSize.Ft20, ContainerType.Dry, 1) });

        var result = await _handler.Handle(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderRule.ExpiryInPast");
    }

    [Fact]
    public async Task Handle_WhenDuplicateLines_ShouldReturnError()
    {
        _context.LineOperators.Add(Domain.Entities.MasterData.LineOperator.Create("MAEU", "Maersk").Value!);
        _context.Customers.Add(Domain.Entities.MasterData.Customer.Create("0101234567", "Customer").Value!);
        await _context.SaveChangesAsync();

        var command = new CreateDeliveryOrderCommand(
            "DO-DUP-LINE",
            "MAEU",
            "0101234567",
            "Customer",
            DateTime.UtcNow.AddDays(7),
            "VESSEL",
            new List<CreateDeliveryOrderItemInput>
            {
                new(ContainerSize.Ft20, ContainerType.Dry, 1),
                new(ContainerSize.Ft20, ContainerType.Dry, 2)
            });

        var result = await _handler.Handle(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderRule.DuplicateLine");
    }
}
