using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.MasterData;

public sealed class LineOperator : AuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private LineOperator() { }

    public static Result<LineOperator> Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<LineOperator>(
                Error.Validation("LineOperator.EmptyCode", "Mã hãng tàu không được để trống."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<LineOperator>(
                Error.Validation("LineOperator.EmptyName", "Tên hãng tàu không được để trống."));
        }

        return Result.Success(new LineOperator
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            IsActive = true
        });
    }

    public Result Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(
                Error.Validation("LineOperator.EmptyName", "Tên hãng tàu không được để trống."));
        }

        Name = name.Trim();
        return Result.Success();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
