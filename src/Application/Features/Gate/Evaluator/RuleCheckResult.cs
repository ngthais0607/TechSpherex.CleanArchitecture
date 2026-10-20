using System.Text.Json.Serialization;

namespace SnpDepotManagement.Application.Features.Gate.Evaluator;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleState
{
    Pass,
    Fail,
    Warning
}

public sealed record RuleCheckResult(
    string RuleCode,
    RuleState State,
    string Label,
    string? Detail);
