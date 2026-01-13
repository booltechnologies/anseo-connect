using System.Text.Json;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Service for managing attendance cases and timeline events.
/// </summary>
public sealed class CaseService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<CaseService> _logger;
    private readonly ITenantContext _tenantContext;
    private readonly MtssTierService? _tierService;

    public CaseService(
        AnseoConnectDbContext dbContext,
        ILogger<CaseService> logger,
        ITenantContext tenantContext,
        MtssTierService? tierService = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _tenantContext = tenantContext;
        _tierService = tierService;
    }

    /// <summary>
    /// Gets or creates an open attendance case for a student.
    /// </summary>
    public async Task<Case> GetOrCreateAttendanceCaseAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var existingCase = await _dbContext.Cases
            .Where(c => c.StudentId == studentId &&
                       c.CaseType == "ATTENDANCE" &&
                       c.Status == "OPEN")
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingCase != null)
        {
            return existingCase;
        }

        // Create new case
        var newCase = new Case
        {
            StudentId = studentId,
            CaseType = "ATTENDANCE",
            Tier = 1,
            Status = "OPEN",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Cases.Add(newCase);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Add timeline event
        await AddTimelineEventAsync(newCase.CaseId, "CASE_CREATED", null, null, cancellationToken);

        _logger.LogInformation("Created new attendance case {CaseId} for student {StudentId}", newCase.CaseId, studentId);

        return newCase;
    }

    /// <summary>
    /// Adds a timeline event to a case.
    /// </summary>
    public async Task AddTimelineEventAsync(
        Guid caseId,
        string eventType,
        string? eventData,
        string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        var timelineEvent = new CaseTimelineEvent
        {
            CaseId = caseId,
            EventType = eventType,
            EventData = eventData,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };

        _dbContext.CaseTimelineEvents.Add(timelineEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added timeline event {EventType} to case {CaseId}", eventType, caseId);
    }

    /// <summary>
    /// Escalates a case to Tier 2 if conditions are met.
    /// </summary>
    public async Task<bool> EscalateToTier2Async(
        Guid caseId,
        string? checklistId = null,
        CancellationToken cancellationToken = default)
    {
        var caseEntity = await _dbContext.Cases
            .Where(c => c.CaseId == caseId)
            .FirstOrDefaultAsync(cancellationToken);

        if (caseEntity == null || caseEntity.Status != "OPEN" || caseEntity.Tier >= 2)
        {
            return false;
        }

        // Use MtssTierService if available
        if (_tierService != null)
        {
            var evaluation = await _tierService.EvaluateTierAsync(caseEntity.StudentId, caseId, cancellationToken);
            var rationaleJson = JsonSerializer.Serialize(new
            {
                evaluation.TriggeredConditions,
                evaluation.AttendancePercent,
                ChecklistId = checklistId
            });

            await _tierService.AssignTierAsync(
                caseId,
                2,
                "AUTO_ESCALATED",
                rationaleJson,
                null,
                cancellationToken);
        }
        else
        {
            caseEntity.Tier = 2;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await AddTimelineEventAsync(caseId, "TIER_2_ESCALATED", checklistId, null, cancellationToken);

        _logger.LogInformation("Escalated case {CaseId} to Tier 2", caseId);

        return true;
    }

    /// <summary>
    /// Escalates a case to Tier 3. Requires evidence pack (optional) and logs timeline.
    /// </summary>
    public async Task<bool> EscalateToTier3Async(
        Guid caseId,
        string reason,
        string? checklistId = null,
        CancellationToken cancellationToken = default)
    {
        var caseEntity = await _dbContext.Cases
            .Where(c => c.CaseId == caseId)
            .FirstOrDefaultAsync(cancellationToken);

        if (caseEntity == null || caseEntity.Status != "OPEN" || caseEntity.Tier >= 3)
        {
            return false;
        }

        // Use MtssTierService if available
        if (_tierService != null)
        {
            var evaluation = await _tierService.EvaluateTierAsync(caseEntity.StudentId, caseId, cancellationToken);
            var rationaleJson = JsonSerializer.Serialize(new
            {
                evaluation.TriggeredConditions,
                evaluation.AttendancePercent,
                Reason = reason,
                ChecklistId = checklistId
            });

            await _tierService.AssignTierAsync(
                caseId,
                3,
                "AUTO_ESCALATED",
                rationaleJson,
                null,
                cancellationToken);
        }
        else
        {
            caseEntity.Tier = 3;
            caseEntity.EscalatedAtUtc = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await AddTimelineEventAsync(caseId, "TIER_3_ESCALATED", reason, null, cancellationToken);

        _logger.LogInformation("Escalated case {CaseId} to Tier 3", caseId);
        return true;
    }

    /// <summary>
    /// Marks a checklist item complete for the latest safeguarding alert or work task on the case that matches the checklistId.
    /// </summary>
    public async Task<bool> CompleteChecklistItemAsync(
        Guid caseId,
        string checklistId,
        string itemId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var alert = await _dbContext.SafeguardingAlerts
            .Where(a => a.CaseId == caseId && a.ChecklistId == checklistId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var workTask = alert == null
            ? await _dbContext.WorkTasks
                .Where(t => t.CaseId == caseId && t.ChecklistId == checklistId && t.Status == "OPEN")
                .OrderByDescending(t => t.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        if (alert == null && workTask == null)
        {
            _logger.LogWarning("No checklist found for case {CaseId} checklist {ChecklistId}", caseId, checklistId);
            return false;
        }

        var existing = await _dbContext.ChecklistCompletions
            .FirstOrDefaultAsync(c =>
                c.CaseId == caseId &&
                c.ChecklistId == checklistId &&
                c.ItemId == itemId,
                cancellationToken);

        if (existing == null)
        {
            existing = new ChecklistCompletion
            {
                ChecklistCompletionId = Guid.NewGuid(),
                CaseId = caseId,
                ChecklistId = checklistId,
                ItemId = itemId,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                CompletedByUserId = null,
                Notes = notes,
                AlertId = alert?.AlertId,
                WorkTaskId = workTask?.WorkTaskId
            };
            _dbContext.ChecklistCompletions.Add(existing);
        }
        else
        {
            existing.CompletedAtUtc = DateTimeOffset.UtcNow;
            existing.CompletedByUserId = null;
            existing.Notes = notes;
            existing.AlertId = alert?.AlertId;
            existing.WorkTaskId = workTask?.WorkTaskId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddTimelineEventAsync(caseId, "CHECKLIST_ITEM_COMPLETED", $"{checklistId}:{itemId}", null, cancellationToken);

        _logger.LogInformation("Completed checklist item {ItemId} for case {CaseId} checklist {ChecklistId}", itemId, caseId, checklistId);
        return true;
    }
}
