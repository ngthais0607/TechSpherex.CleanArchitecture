using FluentValidation;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;

public sealed class GateOutCommandValidator : AbstractValidator<GateOutCommand>
{
    public GateOutCommandValidator()
    {
        RuleFor(x => x.ContainerId)
            .Must(id => id.HasValue && id.Value != Guid.Empty)
            .WithMessage("Container ID is required.")
            .When(x => string.IsNullOrWhiteSpace(x.ContainerNumber));

        RuleFor(x => x.ContainerNumber)
            .NotEmpty().WithMessage("Container number is required.")
            .When(x => !x.ContainerId.HasValue);

        RuleFor(x => x.DeliveryOrderId)
            .Must(id => id.HasValue && id.Value != Guid.Empty)
            .WithMessage("Delivery Order ID is required.")
            .When(x => string.IsNullOrWhiteSpace(x.OrderNumber));

        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("Order number is required.")
            .When(x => !x.DeliveryOrderId.HasValue);

        RuleFor(x => x.OutVehicleNumber)
            .NotEmpty().WithMessage("Outbound vehicle plate number is required.")
            .When(x => string.IsNullOrWhiteSpace(x.VehicleNumber));

        RuleFor(x => x.VehicleNumber)
            .NotEmpty().WithMessage("Biển số xe là bắt buộc.")
            .When(x => string.IsNullOrWhiteSpace(x.OutVehicleNumber));

        RuleFor(x => x.EffectiveVehicleNumber)
            .MaximumLength(20).WithMessage("Vehicle number cannot exceed 20 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.EffectiveVehicleNumber));
    }
}
