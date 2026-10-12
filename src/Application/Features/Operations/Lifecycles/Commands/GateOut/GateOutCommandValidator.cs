using FluentValidation;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateOut;

public sealed class GateOutCommandValidator : AbstractValidator<GateOutCommand>
{
    public GateOutCommandValidator()
    {
        RuleFor(x => x.ContainerId)
            .NotEmpty().WithMessage("Container ID is required.");

        RuleFor(x => x.DeliveryOrderId)
            .NotEmpty().WithMessage("Delivery Order ID is required.");

        RuleFor(x => x.OutVehicleNumber)
            .NotEmpty().WithMessage("Outbound vehicle plate number is required.")
            .MaximumLength(20).WithMessage("Vehicle number cannot exceed 20 characters.");
    }
}
