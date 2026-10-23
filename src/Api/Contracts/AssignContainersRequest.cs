using FluentValidation;

namespace SnpDepotManagement.Api.Contracts;

public sealed record AssignContainersRequest(IReadOnlyList<string> ContainerNumbers);

public sealed class AssignContainersRequestValidator : AbstractValidator<AssignContainersRequest>
{
    public AssignContainersRequestValidator()
    {
        RuleFor(x => x.ContainerNumbers).NotEmpty()
            .Must(numbers => numbers is null || numbers.Count <= 200)
            .WithMessage("A request can assign at most 200 containers.");
        RuleForEach(x => x.ContainerNumbers).NotEmpty();
    }
}
