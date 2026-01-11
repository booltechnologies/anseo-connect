using System.Text.Json;

namespace AnseoConnect.PolicyRuntime;

/// <summary>
/// Interface for evaluating safeguarding triggers from policy packs.
/// </summary>
public interface ISafeguardingEvaluator
{
    /// <summary>
    /// Evaluates safeguarding triggers based on metrics.
    /// </summary>
    /// <param name="policyPackRoot">The root JSON element of the policy pack</param>
    /// <param name="metrics">Dictionary of metrics to evaluate (e.g., guardianNoReplyDays, consecutiveAbsenceDays)</param>
    /// <returns>Result indicating if alert should be created, with severity and checklistId</returns>
    SafeguardingEvaluationResult Evaluate(JsonElement policyPackRoot, Dictionary<string, object> metrics);
}

public sealed record SafeguardingEvaluationResult
{
    public bool CreateAlert { get; set; }
    public string? Severity { get; set; }
    public string? ChecklistId { get; set; }
}
