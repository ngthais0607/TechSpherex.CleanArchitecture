using FluentValidation;

namespace SnpDepotManagement.Application.Features.Yard.Commands.PlaceContainer;

public sealed class PlaceContainerCommandValidator : AbstractValidator<PlaceContainerCommand>
{
    public PlaceContainerCommandValidator()
    {
        RuleFor(x => x.BlockId)
            .NotEmpty().WithMessage("Block ID is required.");

        RuleFor(x => x.ContainerId)
            .NotEmpty().WithMessage("Container ID is required.");

        RuleFor(x => x.Bay)
            .GreaterThan(0).WithMessage("Bay must be greater than 0.");

        RuleFor(x => x.Row)
            .GreaterThan(0).WithMessage("Row must be greater than 0.");

        RuleFor(x => x.Tier)
            .GreaterThan(0).WithMessage("Tier must be greater than 0.");
    }
}
