using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Queries.GetDeliveryOrders;
using SnpDepotManagement.Application.UnitTests.Common;
using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.UnitTests.Operations;

public class GetDeliveryOrdersQueryHandlerTests
{
    private readonly TestAppDbContext _context;
    private readonly GetDeliveryOrdersQueryHandler _handler;

    public GetDeliveryOrdersQueryHandlerTests()
    {
        _context = TestAppDbContext.Create();
        _handler = new GetDeliveryOrdersQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenFilterByLineOperator_ShouldReturnMatchingOrders()
    {
        // Arrange
        var o1 = DeliveryOrder.Create("DO-01", "MAEU", "0101234567", "A", DateTime.UtcNow.AddDays(7), "V1").Value!;
        o1.AddItem(ContainerSize.Ft20, ContainerType.Dry, 2);

        var o2 = DeliveryOrder.Create("DO-02", "ONE", "0101234567", "A", DateTime.UtcNow.AddDays(7), "V2").Value!;
        o2.AddItem(ContainerSize.Ft40, ContainerType.Dry, 1);

        _context.DeliveryOrders.AddRange(o1, o2);
        await _context.SaveChangesAsync();

        var query = new GetDeliveryOrdersQuery(LineOperator: "MAEU", PageIndex: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().OrderNumber.Should().Be("DO-01");
        result.Value.Items.First().LineOperator.Should().Be("MAEU");
    }
}
