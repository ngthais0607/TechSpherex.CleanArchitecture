using FluentValidation;

namespace SnpDepotManagement.Api.Contracts;

public sealed record YardBlockSizeRequest(int NewBays, int NewRows, int NewTiers);

public sealed class YardBlockSizeRequestValidator : AbstractValidator<YardBlockSizeRequest>
{
    public YardBlockSizeRequestValidator()
    {
        RuleFor(x => x.NewBays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NewRows).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NewTiers).GreaterThanOrEqualTo(0);
    }
}
