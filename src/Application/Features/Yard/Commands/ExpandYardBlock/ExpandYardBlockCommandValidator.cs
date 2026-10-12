using FluentValidation;

namespace SnpDepotManagement.Application.Features.Yard.Commands.ExpandYardBlock;

public sealed class ExpandYardBlockCommandValidator : AbstractValidator<ExpandYardBlockCommand>
{
    public ExpandYardBlockCommandValidator()
    {
        RuleFor(x => x.BlockId)
            .NotEmpty().WithMessage("Block ID is required.");

        RuleFor(x => x.NewBays)
            .GreaterThanOrEqualTo(0).WithMessage("New bays must be greater than or equal to zero.");

        RuleFor(x => x.NewRows)
            .GreaterThanOrEqualTo(0).WithMessage("New rows must be greater than or equal to zero.");

        RuleFor(x => x.NewTiers)
            .GreaterThanOrEqualTo(0).WithMessage("New tiers must be greater than or equal to zero.");
    }
}
