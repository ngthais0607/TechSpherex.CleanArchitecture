using FluentValidation;

namespace SnpDepotManagement.Application.Features.MasterData.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.TaxCode)
            .NotEmpty().WithMessage("Tax code is required.")
            .Must(code => code != null && (code.Trim().Length == 10 || code.Trim().Length == 13))
            .WithMessage("Customer tax code must be 10 or 13 digits.")
            .Must(code => code != null && code.Trim().All(char.IsDigit))
            .WithMessage("Customer tax code must consist of digits only.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(150).WithMessage("Customer name cannot exceed 150 characters.");
    }
}
