using FluentValidation;
using SnpDepotManagement.Api.Endpoints;

namespace SnpDepotManagement.Api.Contracts;

public sealed class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
{
    public UpdateTransactionRequestValidator()
    {
        RuleFor(x => x.VehicleNumber).MaximumLength(20);
        RuleFor(x => x.DriverName).MaximumLength(100);
        RuleFor(x => x.Note).MaximumLength(200);
    }
}

public sealed class CancelTransactionRequestValidator : AbstractValidator<CancelTransactionRequest>
{
    public CancelTransactionRequestValidator() => RuleFor(x => x.Reason).MaximumLength(200);
}
