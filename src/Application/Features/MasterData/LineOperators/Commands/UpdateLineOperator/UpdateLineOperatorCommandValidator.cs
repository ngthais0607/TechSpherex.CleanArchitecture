using FluentValidation;

namespace SnpDepotManagement.Application.Features.MasterData.LineOperators.Commands.UpdateLineOperator;

public sealed class UpdateLineOperatorCommandValidator : AbstractValidator<UpdateLineOperatorCommand>
{
    public UpdateLineOperatorCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Line operator ID is required.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Line operator name is required.")
            .MaximumLength(255).WithMessage("Line operator name cannot exceed 255 characters.");
    }
}
