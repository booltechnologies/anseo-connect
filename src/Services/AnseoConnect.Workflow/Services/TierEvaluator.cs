using System.Text.Json;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Evaluates MTSS tier criteria (entry, exit, escalation) for students.
/// </summary>
public sealed class TierEvaluator
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<TierEvaluator> _logger;

    public TierEvaluator(
        AnseoConnectDbContext dbContext,
        ILogger<TierEvaluator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates if a student meets entry criteria for a tier.
    /// </summary>
    public async Task<bool> MeetsEntryCriteriaAsync(Guid studentId, MtssTierDefinition tier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tier.EntryCriteriaJson) || tier.EntryCriteriaJson == "{}")
        {
            return false;
        }

        var criteria = ParseCriteria(tier.EntryCriteriaJson);
        if (criteria.Count == 0)
        {
            return false;
        }

        // Get latest attendance summary
        var latestSummary = await _dbContext.AttendanceDailySummaries
            .AsNoTracking()
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSummary == null)
        {
            return false;
        }

        return EvaluateCriteria(latestSummary, criteria);
    }

    /// <summary>
    /// Evaluates if a student meets exit criteria (can move down a tier).
    /// </summary>
    public async Task<bool> MeetsExitCriteriaAsync(Guid studentId, TierAssignment current, CancellationToken cancellationToken = default)
    {
        var tier = await _dbContext.MtssTierDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TierDefinitionId == current.TierDefinitionId, cancellationToken);

        if (tier == null || string.IsNullOrWhiteSpace(tier.ExitCriteriaJson) || tier.ExitCriteriaJson == "{}")
        {
            return false;
        }

        var criteria = ParseCriteria(tier.ExitCriteriaJson);
        if (criteria.Count == 0)
        {
            return false;
        }

        // Get latest attendance summary
        var latestSummary = await _dbContext.AttendanceDailySummaries
            .AsNoTracking()
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSummary == null)
        {
            return false;
        }

        // For exit criteria, we check if attendance has improved
        // Compare current attendance to baseline at tier assignment
        var baselineSummary = await _dbContext.AttendanceDailySummaries
            .AsNoTracking()
            .Where(s => s.StudentId == studentId && s.Date <= DateOnly.FromDateTime(current.AssignedAtUtc.Date))
            .OrderByDescending(s => s.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (baselineSummary == null)
        {
            return false;
        }

        // Check if attendance improved
        var improvement = latestSummary.AttendancePercent - baselineSummary.AttendancePercent;
        return EvaluateCriteria(latestSummary, criteria) && improvement > 0;
    }

    /// <summary>
    /// Evaluates if a student should escalate to a higher tier.
    /// </summary>
    public async Task<bool> ShouldEscalateAsync(Guid studentId, TierAssignment current, CancellationToken cancellationToken = default)
    {
        var tier = await _dbContext.MtssTierDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TierDefinitionId == current.TierDefinitionId, cancellationToken);

        if (tier == null || string.IsNullOrWhiteSpace(tier.EscalationCriteriaJson) || tier.EscalationCriteriaJson == "{}")
        {
            return false;
        }

        var criteria = ParseCriteria(tier.EscalationCriteriaJson);
        if (criteria.Count == 0)
        {
            return false;
        }

        // Get latest attendance summary
        var latestSummary = await _dbContext.AttendanceDailySummaries
            .AsNoTracking()
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSummary == null)
        {
            return false;
        }

        // Check if enough time has passed in current tier
        var daysInTier = (DateTimeOffset.UtcNow - current.AssignedAtUtc).TotalDays;
        var requiresMinDays = criteria.Any(c => c.PreviousTierDaysMin.HasValue);
        if (requiresMinDays && criteria.First(c => c.PreviousTierDaysMin.HasValue).PreviousTierDaysMin > daysInTier)
        {
            return false;
        }

        return EvaluateCriteria(latestSummary, criteria);
    }

    /// <summary>
    /// Builds a human-readable rationale explanation from evaluation data.
    /// </summary>
    public string BuildRationale(TierEvaluationResult result)
    {
        var parts = new List<string>();

        if (result.AttendancePercent.HasValue)
        {
            parts.Add($"Attendance: {result.AttendancePercent.Value:F1}%");
        }

        if (result.AbsenceCount.HasValue)
        {
            parts.Add($"Total absences: {result.AbsenceCount.Value}");
        }

        if (result.ConsecutiveAbsences.HasValue)
        {
            parts.Add($"Consecutive absences: {result.ConsecutiveAbsences.Value}");
        }

        if (result.TriggeredConditions.Count > 0)
        {
            parts.Add($"Criteria met: {string.Join(", ", result.TriggeredConditions)}");
        }

        if (result.DaysInCurrentTier.HasValue)
        {
            parts.Add($"Days in current tier: {result.DaysInCurrentTier.Value}");
        }

        return string.Join("; ", parts);
    }

    private static List<TierCriteria> ParseCriteria(string criteriaJson)
    {
        if (string.IsNullOrWhiteSpace(criteriaJson) || criteriaJson == "{}")
        {
            return new List<TierCriteria>();
        }

        try
        {
            var criteria = JsonSerializer.Deserialize<List<TierCriteria>>(criteriaJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return criteria ?? new List<TierCriteria>();
        }
        catch
        {
            return new List<TierCriteria>();
        }
    }

    private static bool EvaluateCriteria(AttendanceDailySummary summary, IReadOnlyList<TierCriteria> criteria)
    {
        foreach (var criterion in criteria)
        {
            var matches = true;

            if (criterion.AttendancePercentBelow.HasValue)
            {
                matches = matches && summary.AttendancePercent <= criterion.AttendancePercentBelow.Value;
            }

            if (criterion.AbsenceCountAbove.HasValue)
            {
                matches = matches && summary.TotalAbsenceDaysYTD >= criterion.AbsenceCountAbove.Value;
            }

            if (criterion.ConsecutiveAbsencesAbove.HasValue)
            {
                matches = matches && summary.ConsecutiveAbsenceDays >= criterion.ConsecutiveAbsencesAbove.Value;
            }

            if (matches)
            {
                return true; // At least one criterion must match
            }
        }

        return false;
    }
}

/// <summary>
/// Tier evaluation criteria schema.
/// </summary>
public sealed class TierCriteria
{
    public decimal? AttendancePercentBelow { get; set; }
    public int? AbsenceCountAbove { get; set; }
    public int? ConsecutiveAbsencesAbove { get; set; }
    public int? PreviousTierDaysMin { get; set; }
    public List<string>? RequiresStageCompletion { get; set; }
    public bool? ExcludeIfExemption { get; set; }
}

/// <summary>
/// Result of tier evaluation with detailed metrics.
/// </summary>
public sealed record TierEvaluationResult
{
    public Guid StudentId { get; init; }
    public Guid? TierDefinitionId { get; init; }
    public bool MeetsCriteria { get; init; }
    public List<string> TriggeredConditions { get; init; } = new();
    public decimal? AttendancePercent { get; init; }
    public int? AbsenceCount { get; init; }
    public int? ConsecutiveAbsences { get; init; }
    public int? DaysInCurrentTier { get; init; }
}
