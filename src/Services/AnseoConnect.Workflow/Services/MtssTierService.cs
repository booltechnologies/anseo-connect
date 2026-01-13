using System.Text.Json;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Service for managing MTSS tier assignments and evaluations.
/// </summary>
public sealed class MtssTierService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly TierEvaluator _tierEvaluator;
    private readonly ILogger<MtssTierService> _logger;
    private readonly ITenantContext _tenantContext;

    public MtssTierService(
        AnseoConnectDbContext dbContext,
        TierEvaluator tierEvaluator,
        ILogger<MtssTierService> logger,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tierEvaluator = tierEvaluator;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Evaluates tier placement for a student based on attendance data.
    /// </summary>
    public async Task<TierEvaluationResult> EvaluateTierAsync(Guid studentId, Guid caseId, CancellationToken cancellationToken = default)
    {
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CaseId == caseId, cancellationToken);

        if (caseEntity == null)
        {
            throw new InvalidOperationException($"Case {caseId} not found");
        }

        // Get active tier definitions for tenant
        var tierDefinitions = await _dbContext.MtssTierDefinitions
            .AsNoTracking()
            .Where(t => t.TenantId == _tenantContext.TenantId && t.IsActive)
            .OrderBy(t => t.TierNumber)
            .ToListAsync(cancellationToken);

        if (tierDefinitions.Count == 0)
        {
            _logger.LogWarning("No active tier definitions found for tenant {TenantId}", _tenantContext.TenantId);
            return new TierEvaluationResult
            {
                StudentId = studentId,
                MeetsCriteria = false,
                TriggeredConditions = new List<string> { "NO_TIER_DEFINITIONS" }
            };
        }

        // Get latest attendance summary
        var latestSummary = await _dbContext.AttendanceDailySummaries
            .AsNoTracking()
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSummary == null)
        {
            return new TierEvaluationResult
            {
                StudentId = studentId,
                MeetsCriteria = false,
                TriggeredConditions = new List<string> { "NO_ATTENDANCE_DATA" }
            };
        }

        // Evaluate each tier from highest to lowest (3 -> 2 -> 1)
        foreach (var tier in tierDefinitions.OrderByDescending(t => t.TierNumber))
        {
            var meetsCriteria = await _tierEvaluator.MeetsEntryCriteriaAsync(studentId, tier, cancellationToken);
            if (meetsCriteria)
            {
                var triggeredConditions = new List<string> { $"TIER_{tier.TierNumber}_CRITERIA_MET" };
                var rationale = _tierEvaluator.BuildRationale(new TierEvaluationResult
                {
                    StudentId = studentId,
                    TierDefinitionId = tier.TierDefinitionId,
                    MeetsCriteria = true,
                    TriggeredConditions = triggeredConditions,
                    AttendancePercent = latestSummary.AttendancePercent,
                    AbsenceCount = (int)latestSummary.TotalAbsenceDaysYTD,
                    ConsecutiveAbsences = latestSummary.ConsecutiveAbsenceDays
                });

                return new TierEvaluationResult
                {
                    StudentId = studentId,
                    TierDefinitionId = tier.TierDefinitionId,
                    MeetsCriteria = true,
                    TriggeredConditions = triggeredConditions,
                    AttendancePercent = latestSummary.AttendancePercent,
                    AbsenceCount = (int)latestSummary.TotalAbsenceDaysYTD,
                    ConsecutiveAbsences = latestSummary.ConsecutiveAbsenceDays
                };
            }
        }

        // Default to Tier 1 if no criteria met
        var tier1 = tierDefinitions.FirstOrDefault(t => t.TierNumber == 1);
        return new TierEvaluationResult
        {
            StudentId = studentId,
            TierDefinitionId = tier1?.TierDefinitionId,
            MeetsCriteria = false,
            TriggeredConditions = new List<string> { "DEFAULT_TIER_1" },
            AttendancePercent = latestSummary.AttendancePercent,
            AbsenceCount = (int)latestSummary.TotalAbsenceDaysYTD,
            ConsecutiveAbsences = latestSummary.ConsecutiveAbsenceDays
        };
    }

    /// <summary>
    /// Assigns or changes tier for a case with rationale.
    /// </summary>
    public async Task<TierAssignment> AssignTierAsync(
        Guid caseId,
        int tierNumber,
        string reason,
        string rationaleJson,
        Guid? assignedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var caseEntity = await _dbContext.Cases
            .FirstOrDefaultAsync(c => c.CaseId == caseId, cancellationToken);

        if (caseEntity == null)
        {
            throw new InvalidOperationException($"Case {caseId} not found");
        }

        // Get tier definition
        var tierDefinition = await _dbContext.MtssTierDefinitions
            .FirstOrDefaultAsync(t => t.TenantId == _tenantContext.TenantId && t.TierNumber == tierNumber && t.IsActive, cancellationToken);

        if (tierDefinition == null)
        {
            throw new InvalidOperationException($"Tier {tierNumber} definition not found for tenant");
        }

        // Get or create tier assignment
        var existingAssignment = await _dbContext.TierAssignments
            .FirstOrDefaultAsync(a => a.CaseId == caseId, cancellationToken);

        TierAssignment assignment;
        int? fromTier = null;

        if (existingAssignment != null)
        {
            fromTier = existingAssignment.TierNumber;
            existingAssignment.TierNumber = tierNumber;
            existingAssignment.TierDefinitionId = tierDefinition.TierDefinitionId;
            existingAssignment.AssignmentReason = reason;
            existingAssignment.RationaleJson = rationaleJson;
            existingAssignment.AssignedAtUtc = DateTimeOffset.UtcNow;
            existingAssignment.AssignedByUserId = assignedByUserId;
            existingAssignment.NextReviewAtUtc = DateTimeOffset.UtcNow.AddDays(tierDefinition.ReviewIntervalDays);
            assignment = existingAssignment;
        }
        else
        {
            assignment = new TierAssignment
            {
                AssignmentId = Guid.NewGuid(),
                StudentId = caseEntity.StudentId,
                CaseId = caseId,
                TierNumber = tierNumber,
                TierDefinitionId = tierDefinition.TierDefinitionId,
                AssignmentReason = reason,
                RationaleJson = rationaleJson,
                AssignedAtUtc = DateTimeOffset.UtcNow,
                AssignedByUserId = assignedByUserId,
                NextReviewAtUtc = DateTimeOffset.UtcNow.AddDays(tierDefinition.ReviewIntervalDays)
            };
            _dbContext.TierAssignments.Add(assignment);
        }

        // Update case tier
        caseEntity.Tier = tierNumber;

        // Create history record if tier changed
        if (fromTier.HasValue && fromTier.Value != tierNumber)
        {
            var changeType = tierNumber > fromTier.Value ? "ESCALATION" : "DE_ESCALATION";
            var history = new TierAssignmentHistory
            {
                HistoryId = Guid.NewGuid(),
                AssignmentId = assignment.AssignmentId,
                StudentId = caseEntity.StudentId,
                CaseId = caseId,
                FromTier = fromTier.Value,
                ToTier = tierNumber,
                ChangeType = changeType,
                ChangeReason = reason,
                RationaleJson = rationaleJson,
                ChangedByUserId = assignedByUserId,
                ChangedAtUtc = DateTimeOffset.UtcNow
            };
            _dbContext.TierAssignmentHistories.Add(history);
        }
        else if (!fromTier.HasValue)
        {
            // First assignment
            var history = new TierAssignmentHistory
            {
                HistoryId = Guid.NewGuid(),
                AssignmentId = assignment.AssignmentId,
                StudentId = caseEntity.StudentId,
                CaseId = caseId,
                FromTier = 0,
                ToTier = tierNumber,
                ChangeType = "ENTRY",
                ChangeReason = reason,
                RationaleJson = rationaleJson,
                ChangedByUserId = assignedByUserId,
                ChangedAtUtc = DateTimeOffset.UtcNow
            };
            _dbContext.TierAssignmentHistories.Add(history);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned tier {TierNumber} to case {CaseId} with reason {Reason}", tierNumber, caseId, reason);

        return assignment;
    }

    /// <summary>
    /// Gets tier history for a case.
    /// </summary>
    public async Task<List<TierAssignmentHistory>> GetHistoryAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TierAssignmentHistories
            .AsNoTracking()
            .Where(h => h.CaseId == caseId)
            .OrderBy(h => h.ChangedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets current tier assignment with rationale explanation.
    /// </summary>
    public async Task<TierAssignmentWithRationale?> GetCurrentTierAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var assignment = await _dbContext.TierAssignments
            .AsNoTracking()
            .Include(a => a.TierDefinition)
            .FirstOrDefaultAsync(a => a.CaseId == caseId, cancellationToken);

        if (assignment == null)
        {
            return null;
        }

        var rationale = _tierEvaluator.BuildRationale(new TierEvaluationResult
        {
            StudentId = assignment.StudentId,
            TierDefinitionId = assignment.TierDefinitionId,
            MeetsCriteria = true,
            TriggeredConditions = new List<string>()
        });

        return new TierAssignmentWithRationale
        {
            Assignment = assignment,
            Rationale = rationale
        };
    }

    /// <summary>
    /// Maps intervention stages to tier progression.
    /// </summary>
    public async Task SyncStageToTierMappingAsync(Guid instanceId, Guid stageId, CancellationToken cancellationToken = default)
    {
        var instance = await _dbContext.StudentInterventionInstances
            .Include(i => i.Case)
            .FirstOrDefaultAsync(i => i.InstanceId == instanceId, cancellationToken);

        if (instance == null || instance.Case == null)
        {
            return;
        }

        var stage = await _dbContext.InterventionStages
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StageId == stageId, cancellationToken);

        if (stage == null)
        {
            return;
        }

        // Map stage types to tier progression
        // Letter1 -> Tier 1, Letter2 -> Tier 2, Meeting -> Tier 2/3, Escalation -> Tier 3
        int? suggestedTier = stage.StageType?.ToUpperInvariant() switch
        {
            "LETTER_1" => 1,
            "LETTER_2" => 2,
            "MEETING" => 2,
            "CONFERENCE" => 2,
            "ESCALATION" => 3,
            _ => null
        };

        if (suggestedTier.HasValue && instance.Case.Tier < suggestedTier.Value)
        {
            var evaluation = await EvaluateTierAsync(instance.StudentId, instance.CaseId, cancellationToken);
            var rationaleJson = JsonSerializer.Serialize(new { evaluation.TriggeredConditions, evaluation.AttendancePercent });

            await AssignTierAsync(
                instance.CaseId,
                suggestedTier.Value,
                $"STAGE_{stage.StageType}_TRIGGERED",
                rationaleJson,
                null,
                cancellationToken);
        }
    }
}

/// <summary>
/// Tier assignment with human-readable rationale.
/// </summary>
public sealed record TierAssignmentWithRationale
{
    public TierAssignment Assignment { get; init; } = null!;
    public string Rationale { get; init; } = string.Empty;
}
