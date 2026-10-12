using FluentValidation;

namespace SnpDepotManagement.Application.Features.Yard.Commands.RemoveContainer;

public sealed class RemoveContainerCommandValidator : AbstractValidator<RemoveContainerCommand>
{
    public RemoveContainerCommandValidator()
    {
        RuleFor(x => x.BlockId)
            .NotEmpty().WithMessage("Block ID is required.");

        RuleFor(x => x.Bay)
            .GreaterThan(0).WithMessage("Bay must be greater than 0.");

        RuleFor(x => x.Row)
            .GreaterThan(0).WithMessage("Row must be greater than 0.");

        RuleFor(x => x.Tier)
            .GreaterThan(0).WithMessage("Tier must be greater than 0.");
    }
}
