using FluentValidation;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;

public sealed class GateInCommandValidator : AbstractValidator<GateInCommand>
{
    public GateInCommandValidator()
    {
        RuleFor(x => x.ContainerId)
            .NotEmpty().WithMessage("Container ID is required.");

        RuleFor(x => x.InVehicleNumber)
            .NotEmpty().WithMessage("Inbound vehicle plate number is required.")
            .MaximumLength(20).WithMessage("Vehicle number cannot exceed 20 characters.");

        RuleFor(x => x.SlotId)
            .NotEmpty().WithMessage("Slot ID is required.");
    }
}
