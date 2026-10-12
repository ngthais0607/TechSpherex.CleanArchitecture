using FluentValidation;

namespace SnpDepotManagement.Application.Features.Yard.Commands.RelocateContainer;

public sealed class RelocateContainerCommandValidator : AbstractValidator<RelocateContainerCommand>
{
    public RelocateContainerCommandValidator()
    {
        RuleFor(x => x.BlockId)
            .NotEmpty().WithMessage("Block ID is required.");

        RuleFor(x => x.SourceBay)
            .GreaterThan(0).WithMessage("Source Bay must be greater than 0.");

        RuleFor(x => x.SourceRow)
            .GreaterThan(0).WithMessage("Source Row must be greater than 0.");

        RuleFor(x => x.SourceTier)
            .GreaterThan(0).WithMessage("Source Tier must be greater than 0.");

        RuleFor(x => x.TargetBay)
            .GreaterThan(0).WithMessage("Target Bay must be greater than 0.");

        RuleFor(x => x.TargetRow)
            .GreaterThan(0).WithMessage("Target Row must be greater than 0.");

        RuleFor(x => x.TargetTier)
            .GreaterThan(0).WithMessage("Target Tier must be greater than 0.");

        RuleFor(x => x)
            .Must(x => !(x.SourceBay == x.TargetBay && x.SourceRow == x.TargetRow && x.SourceTier == x.TargetTier))
            .WithMessage("Source and target positions must be different.");
    }
}
