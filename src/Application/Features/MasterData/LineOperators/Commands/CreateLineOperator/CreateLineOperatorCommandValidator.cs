using FluentValidation;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.CreateLineOperator;

public sealed class CreateLineOperatorCommandValidator : AbstractValidator<CreateLineOperatorCommand>
{
    public CreateLineOperatorCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Line operator code is required.")
            .MaximumLength(20).WithMessage("Line operator code cannot exceed 20 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Line operator name is required.")
            .MaximumLength(100).WithMessage("Line operator name cannot exceed 100 characters.");
    }
}
