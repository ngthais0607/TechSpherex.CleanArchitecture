using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Queries.GetDeliveryOrderByOrderNumber;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.UnitTests.Operations;

public class GetDeliveryOrderByOrderNumberQueryHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly GetDeliveryOrderByOrderNumberQueryHandler _handler;

    public GetDeliveryOrderByOrderNumberQueryHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new GetDeliveryOrderByOrderNumberQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenOrderExists_ShouldReturnMatchingDeliveryOrder()
    {
        // Arrange
        var order = DeliveryOrder.Create("DO-2026-001", "MSC", "0123456789", "Cong Ty A", DateTime.UtcNow.AddDays(7), "MSC OSCAR").Value!;
        order.AddItem(ContainerSize.Ft20, ContainerType.Dry, 3);
        _context.DeliveryOrders.Add(order);
        await _context.SaveChangesAsync();

        var query = new GetDeliveryOrderByOrderNumberQuery("do-2026-001");

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrderNumber.Should().Be("DO-2026-001");
        result.Value.LineOperator.Should().Be("MSC");
        result.Value.CustomerTaxCode.Should().Be("0123456789");
        result.Value.CustomerName.Should().Be("Cong Ty A");
        result.Value.OutboundVessel.Should().Be("MSC OSCAR");
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().RequiredQuantity.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ShouldReturnNotFoundError()
    {
        var query = new GetDeliveryOrderByOrderNumberQuery("NON-EXISTENT");

        var result = await _handler.Handle(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeliveryOrder.NotFound");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WhenOrderNumberEmpty_ShouldReturnValidationError(string? orderNumber)
    {
        var query = new GetDeliveryOrderByOrderNumberQuery(orderNumber!);

        var result = await _handler.Handle(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeliveryOrder.EmptyOrderNumber");
    }
}
