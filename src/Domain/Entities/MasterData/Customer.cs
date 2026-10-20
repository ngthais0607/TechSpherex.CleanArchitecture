using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.MasterData;

public sealed class Customer : AuditableEntity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string TaxCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private Customer() { }

    public static Result<Customer> Create(string? taxCode, string? name, string tenantId = "")
    {
        if (string.IsNullOrWhiteSpace(taxCode))
        {
            return Result.Failure<Customer>(
                Error.Validation("Customer.EmptyTaxCode", "Mã số thuế không được để trống."));
        }

        var normalizedTaxCode = taxCode.Trim();

        if (normalizedTaxCode.Length != 10 && normalizedTaxCode.Length != 13)
        {
            return Result.Failure<Customer>(
                Error.Validation("Customer.InvalidTaxCodeLength", "Mã số thuế phải gồm 10 hoặc 13 chữ số."));
        }

        if (!normalizedTaxCode.All(char.IsDigit))
        {
            return Result.Failure<Customer>(
                Error.Validation("Customer.InvalidTaxCodeFormat", "Mã số thuế chỉ được chứa các chữ số."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Customer>(
                Error.Validation("Customer.EmptyName", "Tên khách hàng không được để trống."));
        }

        return Result.Success(new Customer
        {
            TenantId = tenantId,
            TaxCode = normalizedTaxCode,
            Name = name.Trim(),
            IsActive = true
        });
    }

    public Result Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(
                Error.Validation("Customer.EmptyName", "Tên khách hàng không được để trống."));
        }

        Name = name.Trim();
        return Result.Success();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
