using SnpDepotManagement.Domain.Entities.Operations;
using SnpDepotManagement.Domain.Enums;
using SnpDepotManagement.Domain.Rules;

namespace SnpDepotManagement.Domain.UnitTests.Rules;

public sealed class DeliveryOrderHasRemainingQuantityRuleTests
{
    private readonly DeliveryOrder _order;

    public DeliveryOrderHasRemainingQuantityRuleTests()
    {
        _order = DeliveryOrder.Create(
            "DO-001",
            "MSC",
            "0123456789",
            "Partner A",
            DateTime.UtcNow.AddDays(5),
            "VESSEL-1").Value!;

        _order.AddItem(ContainerSize.Ft20, ContainerType.Dry, quantity: 2);
    }

    [Fact]
    public void IsBroken_WhenRemainingQuantityExists_ShouldBeFalse()
    {
        // Còn số lượng đúng size và type
        var rule = new DeliveryOrderHasRemainingQuantityRule(_order, ContainerSize.Ft20, ContainerType.Dry);

        rule.IsBroken().Should().BeFalse();
    }

    [Fact]
    public void IsBroken_WhenQuantityIsExhausted_ShouldBeTrue()
    {
        // Hết số lượng: Allocate toàn bộ 2 cont
        var item = _order.Items.First();
        item.Allocate(2);

        var rule = new DeliveryOrderHasRemainingQuantityRule(_order, ContainerSize.Ft20, ContainerType.Dry);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("OrderRule.NoRemainingQuantity");
    }

    [Fact]
    public void IsBroken_WhenContainerTypeOrSizeNotInOrder_ShouldBeTrue()
    {
        // Sai loại: DO chỉ có Ft20 Dry, kiểm tra Ft40 Reefer
        var rule = new DeliveryOrderHasRemainingQuantityRule(_order, ContainerSize.Ft40, ContainerType.Reefer);

        rule.IsBroken().Should().BeTrue();
        rule.Error.Code.Should().Be("OrderRule.NoRemainingQuantity");
    }
}
