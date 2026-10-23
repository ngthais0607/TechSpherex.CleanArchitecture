using FluentValidation;
using SnpDepotManagement.Api.Endpoints;

namespace SnpDepotManagement.Api.Contracts;

public sealed class PlaceFromVirtualBlockRequestValidator : AbstractValidator<PlaceFromVirtualBlockRequest>
{
    public PlaceFromVirtualBlockRequestValidator()
    {
        RuleFor(x => x.TargetBlockCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Bay).GreaterThan(0);
        RuleFor(x => x.Row).GreaterThan(0);
        RuleFor(x => x.Tier).GreaterThan(0);
    }
}
