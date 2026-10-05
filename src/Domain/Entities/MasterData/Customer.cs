using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.MasterData;

public sealed class Customer : AuditableEntity
{
    public string TaxCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private Customer() { }

    public static Result<Customer> Create(string? taxCode, string? name)
    {
        if (string.IsNullOrWhiteSpace(taxCode))
        {
            return Result.Failure<Customer>(
                Error.Validation("Customer.EmptyTaxCode", "Customer tax code cannot be empty."));
        }

        var normalizedTaxCode = taxCode.Trim();

        if (normalizedTaxCode.Length != 10 && normalizedTaxCode.Length != 13)
        {
            return Result.Failure<Customer>(
                Error.Validation("Customer.InvalidTaxCodeLength", "Customer tax code must be 10 or 13 digits."));
        }

        if (!normalizedTaxCode.All(char.IsDigit))
        {
            return Result.Failure<Customer>(
                Error.Validation("Customer.InvalidTaxCodeFormat", "Customer tax code must consist of digits only."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Customer>(
                Error.Validation("Customer.EmptyName", "Customer name cannot be empty."));
        }

        return Result.Success(new Customer
        {
            TaxCode = normalizedTaxCode,
            Name = name.Trim()
        });
    }
}
