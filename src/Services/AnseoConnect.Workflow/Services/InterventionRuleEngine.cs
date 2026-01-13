using System.Text.Json;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Evaluates intervention rule sets to produce eligible students and supports simulation.
/// </summary>
public sealed class InterventionRuleEngine
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<InterventionRuleEngine> _logger;

    public InterventionRuleEngine(
        AnseoConnectDbContext dbContext,
        ILogger<InterventionRuleEngine> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<EligibleStudent>> EvaluateAsync(Guid schoolId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var ruleSets = await _dbContext.InterventionRuleSets
            .AsNoTracking()
            .Where(r => r.SchoolId == schoolId && r.IsActive)
            .ToListAsync(cancellationToken);

        if (ruleSets.Count == 0)
        {
            _logger.LogInformation("No active intervention rule sets for school {SchoolId}", schoolId);
            return new List<EligibleStudent>();
        }

        var summaries = await _dbContext.AttendanceDailySummaries
            .AsNoTracking()
            .Where(s => s.SchoolId == schoolId && s.Date == date)
            .ToListAsync(cancellationToken);

        var results = new List<EligibleStudent>();

        foreach (var ruleSet in ruleSets)
        {
            var conditions = ParseConditions(ruleSet.RulesJson);
            foreach (var summary in summaries)
            {
                var triggered = EvaluateConditions(summary, conditions);
                if (triggered.Count > 0)
                {
                    results.Add(new EligibleStudent(
                        summary.StudentId,
                        ruleSet.RuleSetId,
                        triggered));
                }
            }
        }

        return results;
    }

    public async Task<SimulationResult> SimulateAsync(Guid studentId, InterventionRuleSet ruleSet, DateOnly date, CancellationToken cancellationToken = default)
    {
        var summary = await _dbContext.AttendanceDailySummaries
            .AsNoTracking()
            .Where(s => s.StudentId == studentId && s.Date == date)
            .FirstOrDefaultAsync(cancellationToken);

        if (summary == null)
        {
            return new SimulationResult(studentId, ruleSet.RuleSetId, false, new List<string>());
        }

        var conditions = ParseConditions(ruleSet.RulesJson);
        var triggered = EvaluateConditions(summary, conditions);

        return new SimulationResult(studentId, ruleSet.RuleSetId, triggered.Count > 0, triggered);
    }

    private static List<RuleCondition> ParseConditions(string rulesJson)
    {
        if (string.IsNullOrWhiteSpace(rulesJson))
        {
            return new List<RuleCondition>();
        }

        try
        {
            var conditions = JsonSerializer.Deserialize<List<RuleCondition>>(rulesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return conditions ?? new List<RuleCondition>();
        }
        catch
        {
            return new List<RuleCondition>();
        }
    }

    private static List<string> EvaluateConditions(AttendanceDailySummary summary, IReadOnlyList<RuleCondition> conditions)
    {
        var triggered = new List<string>();

        foreach (var condition in conditions)
        {
            switch (condition.Type?.ToUpperInvariant())
            {
                case "ATTENDANCEPERCENTTHRESHOLD":
                    if (condition.ThresholdPercentage.HasValue &&
                        summary.AttendancePercent <= condition.ThresholdPercentage.Value)
                    {
                        triggered.Add(condition.Type);
                    }
                    break;
                case "ABSENCECOUNTTHRESHOLD":
                    if (condition.ThresholdCount.HasValue &&
                        summary.TotalAbsenceDaysYTD >= condition.ThresholdCount.Value)
                    {
                        triggered.Add(condition.Type);
                    }
                    break;
                case "CONSECUTIVEABSENCEDAYS":
                    if (condition.ConsecutiveDays.HasValue &&
                        summary.ConsecutiveAbsenceDays >= condition.ConsecutiveDays.Value)
                    {
                        triggered.Add(condition.Type);
                    }
                    break;
                default:
                    break;
            }
        }

        return triggered;
    }
}

public sealed record RuleCondition
{
    public string Type { get; init; } = string.Empty;
    public int? ThresholdCount { get; init; }
    public decimal? ThresholdPercentage { get; init; }
    public int? ConsecutiveDays { get; init; }
}

public sealed record EligibleStudent(Guid StudentId, Guid RuleSetId, IReadOnlyList<string> TriggeredConditions);

public sealed record SimulationResult(Guid StudentId, Guid RuleSetId, bool IsEligible, IReadOnlyList<string> TriggeredConditions);

