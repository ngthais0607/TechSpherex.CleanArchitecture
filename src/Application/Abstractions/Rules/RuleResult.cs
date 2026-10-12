using SnpDepotManagement.Domain.Common;

namespace SnpDepotManagement.Application.Abstractions.Rules;

public sealed record RuleResult
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    private RuleResult(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static RuleResult Success() => new(true, null);

    public static RuleResult Failure(Error error) => new(false, error);
}
