using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Domain.UnitTests.Operations;

public sealed class DeliveryOrderTests
{
    private readonly DeliveryOrder _order;

    public DeliveryOrderTests()
    {
        _order = DeliveryOrder.Create(
            orderNumber: "DO-2026-001",
            lineOperator: "MSC",
            customerTaxCode: "0123456789",
            customerName: "Logistics Partner A",
            expiryDate: DateTime.UtcNow.AddDays(7),
            outboundVessel: "MSC OSCAR").Value!;

        _order.AddItem(ContainerSize.Ft20, ContainerType.Dry, quantity: 5);
    }

    [Fact]
    public void Allocate_ShouldDecreaseRemainingQuantity()
    {
        var item = _order.Items.First();
        item.RemainingQuantity.Should().Be(5);
        item.DeliveredQuantity.Should().Be(0);

        var result = item.Allocate(2);

        result.IsSuccess.Should().BeTrue();
        item.DeliveredQuantity.Should().Be(2);
        item.RemainingQuantity.Should().Be(3);
    }

    [Fact]
    public void Allocate_WhenExceedingRemainingQuantity_ShouldReturnError()
    {
        var item = _order.Items.First();

        var result = item.Allocate(6); // Only 5 available

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeliveryOrderItem.QuantityExceeded");
        item.DeliveredQuantity.Should().Be(0);
        item.RemainingQuantity.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Allocate_WhenQuantityZeroOrNegative_ShouldReturnError(int quantity)
    {
        var item = _order.Items.First();

        var result = item.Allocate(quantity);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeliveryOrderItem.InvalidQuantity");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WhenCustomerTaxCodeIsEmpty_ShouldReturnFailure(string? taxCode)
    {
        var result = DeliveryOrder.Create("DO-01", "MSK", taxCode!, "Customer A", DateTime.UtcNow.AddDays(7), "VESSEL-01");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeliveryOrder.EmptyCustomerTaxCode");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WhenCustomerNameIsEmpty_ShouldReturnFailure(string? customerName)
    {
        var result = DeliveryOrder.Create("DO-01", "MSK", "0101234567", customerName!, DateTime.UtcNow.AddDays(7), "VESSEL-01");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeliveryOrder.EmptyCustomerName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WhenOutboundVesselIsEmpty_ShouldReturnFailure(string? vessel)
    {
        var result = DeliveryOrder.Create("DO-01", "MSK", "0101234567", "Customer A", DateTime.UtcNow.AddDays(7), vessel!);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeliveryOrder.EmptyOutboundVessel");
    }
}

