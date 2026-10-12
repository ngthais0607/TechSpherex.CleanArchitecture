using FluentValidation.TestHelper;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Commands.CreateDeliveryOrder;
using SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Dtos;
using SnpDepotManagement.Domain.Enums;

namespace SnpDepotManagement.Application.UnitTests.Operations;

public class CreateDeliveryOrderValidatorTests
{
    private readonly CreateDeliveryOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new CreateDeliveryOrderCommand(
            "DO-123456",
            "MAEU",
            "0101234567",
            "Cong Ty XNK A",
            DateTime.UtcNow.AddDays(7),
            "VESSEL-01",
            new List<CreateDeliveryOrderItemInput>
            {
                new(ContainerSize.Ft20, ContainerType.Dry, 5)
            });

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenOrderNumberIsEmpty_ShouldHaveValidationError(string orderNumber)
    {
        var command = new CreateDeliveryOrderCommand(
            orderNumber,
            "MAEU",
            "0101234567",
            "Cong Ty XNK A",
            DateTime.UtcNow.AddDays(7),
            "VESSEL-01",
            new List<CreateDeliveryOrderItemInput>
            {
                new(ContainerSize.Ft20, ContainerType.Dry, 5)
            });

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderNumber);
    }

    [Fact]
    public void Validate_WhenItemsListIsEmpty_ShouldHaveValidationError()
    {
        var command = new CreateDeliveryOrderCommand(
            "DO-123456",
            "MAEU",
            "0101234567",
            "Cong Ty XNK A",
            DateTime.UtcNow.AddDays(7),
            "VESSEL-01",
            new List<CreateDeliveryOrderItemInput>());

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_WhenItemQuantityIsZeroOrNegative_ShouldHaveValidationError()
    {
        var command = new CreateDeliveryOrderCommand(
            "DO-123456",
            "MAEU",
            "0101234567",
            "Cong Ty XNK A",
            DateTime.UtcNow.AddDays(7),
            "VESSEL-01",
            new List<CreateDeliveryOrderItemInput>
            {
                new(ContainerSize.Ft20, ContainerType.Dry, 0)
            });

        var result = _validator.TestValidate(command);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenCustomerTaxCodeIsEmpty_ShouldHaveValidationError(string taxCode)
    {
        var command = new CreateDeliveryOrderCommand(
            "DO-123456",
            "MAEU",
            taxCode,
            "Cong Ty XNK A",
            DateTime.UtcNow.AddDays(7),
            "VESSEL-01",
            new List<CreateDeliveryOrderItemInput> { new(ContainerSize.Ft20, ContainerType.Dry, 5) });

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerTaxCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenCustomerNameIsEmpty_ShouldHaveValidationError(string customerName)
    {
        var command = new CreateDeliveryOrderCommand(
            "DO-123456",
            "MAEU",
            "0101234567",
            customerName,
            DateTime.UtcNow.AddDays(7),
            "VESSEL-01",
            new List<CreateDeliveryOrderItemInput> { new(ContainerSize.Ft20, ContainerType.Dry, 5) });

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenOutboundVesselIsEmpty_ShouldHaveValidationError(string vessel)
    {
        var command = new CreateDeliveryOrderCommand(
            "DO-123456",
            "MAEU",
            "0101234567",
            "Cong Ty XNK A",
            DateTime.UtcNow.AddDays(7),
            vessel,
            new List<CreateDeliveryOrderItemInput> { new(ContainerSize.Ft20, ContainerType.Dry, 5) });

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OutboundVessel);
    }
}

