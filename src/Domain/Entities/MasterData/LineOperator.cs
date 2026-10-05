using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Domain.Entities.MasterData;

public sealed class LineOperator : AuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private LineOperator() { }

    public static Result<LineOperator> Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<LineOperator>(
                Error.Validation("LineOperator.EmptyCode", "Line operator code cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<LineOperator>(
                Error.Validation("LineOperator.EmptyName", "Line operator name cannot be empty."));
        }

        return Result.Success(new LineOperator
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim()
        });
    }
}
