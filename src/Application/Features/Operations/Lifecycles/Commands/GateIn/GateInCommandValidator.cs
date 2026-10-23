using FluentValidation;

namespace SnpDepotManagement.Application.Features.Operations.Lifecycles.Commands.GateIn;

public sealed class GateInCommandValidator : AbstractValidator<GateInCommand>
{
    public GateInCommandValidator()
    {
        RuleFor(x => x.DriverName).MaximumLength(100);

        RuleFor(x => x.ContainerId)
            .Must(id => id.HasValue && id.Value != Guid.Empty)
            .WithMessage("Container ID is required.")
            .When(x => string.IsNullOrWhiteSpace(x.ContainerNumber));

        RuleFor(x => x.ContainerNumber)
            .NotEmpty().WithMessage("Container number is required.")
            .When(x => !x.ContainerId.HasValue);

        RuleFor(x => x.InVehicleNumber)
            .NotEmpty().WithMessage("Inbound vehicle plate number is required.")
            .When(x => string.IsNullOrWhiteSpace(x.VehicleNumber));

        RuleFor(x => x.VehicleNumber)
            .NotEmpty().WithMessage("Biển số xe là bắt buộc.")
            .When(x => string.IsNullOrWhiteSpace(x.InVehicleNumber));

        RuleFor(x => x.SlotId)
            .Must(id => id.HasValue && id.Value != Guid.Empty)
            .WithMessage("Slot ID is required.")
            .When(x => string.IsNullOrWhiteSpace(x.BlockCode) && string.IsNullOrWhiteSpace(x.VirtualBlockCode));

        RuleFor(x => x.EffectiveVehicleNumber)
            .MaximumLength(20).WithMessage("Vehicle number cannot exceed 20 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.EffectiveVehicleNumber));
    }
}
