using FluentValidation;

namespace SnpDepotManagement.Application.Features.MasterData.Voyages.Commands.CreateVoyage;

public sealed class CreateVoyageCommandValidator : AbstractValidator<CreateVoyageCommand>
{
    public CreateVoyageCommandValidator()
    {
        RuleFor(x => x.VesselName)
            .NotEmpty().WithMessage("Vessel name is required.")
            .MaximumLength(100).WithMessage("Vessel name cannot exceed 100 characters.");

        RuleFor(x => x.VoyageNumber)
            .NotEmpty().WithMessage("Voyage number is required.")
            .MaximumLength(50).WithMessage("Voyage number cannot exceed 50 characters.");

        RuleFor(x => x.Etd)
            .NotEmpty().WithMessage("Estimated departure time (ETD) is required.");
    }
}
