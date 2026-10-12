using FluentValidation;

namespace SnpDepotManagement.Application.Features.Operations.DeliveryOrders.Commands.CreateDeliveryOrder;

public sealed class CreateDeliveryOrderCommandValidator : AbstractValidator<CreateDeliveryOrderCommand>
{
    public CreateDeliveryOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("Order number is required.")
            .MaximumLength(50).WithMessage("Order number cannot exceed 50 characters.");

        RuleFor(x => x.LineOperator)
            .NotEmpty().WithMessage("Line operator is required.")
            .MaximumLength(10).WithMessage("Line operator cannot exceed 10 characters.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required in a delivery order.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Item quantity must be greater than zero.");
        });
    }
}
