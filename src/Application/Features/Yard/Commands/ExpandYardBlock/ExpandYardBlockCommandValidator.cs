using FluentValidation;
using SnpDepotManagement.Domain.Entities.Yard;

namespace SnpDepotManagement.Application.Features.Yard.Commands.ExpandYardBlock;

public sealed class ExpandYardBlockCommandValidator : AbstractValidator<ExpandYardBlockCommand>
{
    public ExpandYardBlockCommandValidator()
    {
        RuleFor(x => x.BlockId)
            .NotEmpty().WithMessage("Block ID is required.");

        RuleFor(x => x.NewBays)
            .InclusiveBetween(0, YardBlockLimits.MaxBays);

        RuleFor(x => x.NewRows)
            .InclusiveBetween(0, YardBlockLimits.MaxRows);

        RuleFor(x => x.NewTiers)
            .InclusiveBetween(0, YardBlockLimits.MaxTiers);

        RuleFor(x => x).Must(x => YardBlockLimits.IsWithinLimits(x.NewBays, x.NewRows, x.NewTiers))
            .WithMessage(YardBlockLimits.InvalidSize.Description).OverridePropertyName("Dimensions");
    }
}
