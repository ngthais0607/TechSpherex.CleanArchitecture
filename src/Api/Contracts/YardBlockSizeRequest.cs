using FluentValidation;
using SnpDepotManagement.Domain.Entities.Yard;

namespace SnpDepotManagement.Api.Contracts;

public sealed record YardBlockSizeRequest(int NewBays, int NewRows, int NewTiers);

public sealed class YardBlockSizeRequestValidator : AbstractValidator<YardBlockSizeRequest>
{
    public YardBlockSizeRequestValidator()
    {
        RuleFor(x => x.NewBays).InclusiveBetween(0, YardBlockLimits.MaxBays);
        RuleFor(x => x.NewRows).InclusiveBetween(0, YardBlockLimits.MaxRows);
        RuleFor(x => x.NewTiers).InclusiveBetween(0, YardBlockLimits.MaxTiers);
        RuleFor(x => x).Must(x => YardBlockLimits.IsWithinLimits(x.NewBays, x.NewRows, x.NewTiers))
            .WithMessage(YardBlockLimits.InvalidSize.Description).OverridePropertyName("Dimensions");
    }
}
