using FluentValidation;
using SnpDepotManagement.Domain.Entities.Yard;

namespace SnpDepotManagement.Application.Features.Yard.Commands.CreateYardBlock;

public sealed class CreateYardBlockCommandValidator : AbstractValidator<CreateYardBlockCommand>
{
    public CreateYardBlockCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Block code is required.")
            .MaximumLength(20).WithMessage("Block code cannot exceed 20 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Block name is required.")
            .MaximumLength(100).WithMessage("Block name cannot exceed 100 characters.");

        When(x => !x.IsVirtual, () =>
        {
            RuleFor(x => x.Bays)
                .GreaterThan(0).WithMessage("Number of bays must be greater than zero for standard block.")
                .LessThanOrEqualTo(YardBlockLimits.MaxBays);

            RuleFor(x => x.Rows)
                .GreaterThan(0).WithMessage("Number of rows must be greater than zero for standard block.")
                .LessThanOrEqualTo(YardBlockLimits.MaxRows);

            RuleFor(x => x.Tiers)
                .GreaterThan(0).WithMessage("Number of tiers must be greater than zero for standard block.")
                .LessThanOrEqualTo(YardBlockLimits.MaxTiers);

            RuleFor(x => x).Must(x => YardBlockLimits.IsWithinLimits(x.Bays, x.Rows, x.Tiers))
                .WithMessage(YardBlockLimits.InvalidSize.Description).OverridePropertyName("Dimensions");
        });
    }
}
