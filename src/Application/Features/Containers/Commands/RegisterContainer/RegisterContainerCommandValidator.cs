using FluentValidation;

namespace SnpDepotManagement.Application.Features.Containers.Commands.RegisterContainer;

public sealed class RegisterContainerCommandValidator : AbstractValidator<RegisterContainerCommand>
{
    public RegisterContainerCommandValidator()
    {
        RuleFor(x => x.ContainerNumber)
            .NotEmpty().WithMessage("Container number is required.")
            .Length(11).WithMessage("Container number must be exactly 11 characters.");

        RuleFor(x => x.TareWeight)
            .GreaterThan(0).WithMessage("Tare weight must be greater than zero.");

        RuleFor(x => x.MaxGrossWeight)
            .GreaterThan(0).WithMessage("Max gross weight must be greater than zero.")
            .GreaterThan(x => x.TareWeight).WithMessage("Max gross weight must be greater than tare weight.");
    }
}
