using FluentValidation;

namespace SnpDepotManagement.Application.Features.MasterData.Vehicles.Commands.CreateVehicle;

public sealed class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(x => x.PlateNumber)
            .NotEmpty().WithMessage("Vehicle plate number is required.")
            .MaximumLength(20).WithMessage("Plate number cannot exceed 20 characters.");
    }
}
