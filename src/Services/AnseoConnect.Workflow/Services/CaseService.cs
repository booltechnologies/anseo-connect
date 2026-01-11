using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Service for managing attendance cases and timeline events.
/// </summary>
public sealed class CaseService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<CaseService> _logger;
    private readonly ITenantContext _tenantContext;

    public CaseService(
        AnseoConnectDbContext dbContext,
        ILogger<CaseService> logger,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _logger = logger;
        _tenantContext = tenantContext;
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

        caseEntity.Tier = 2;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await AddTimelineEventAsync(caseId, "TIER_2_ESCALATED", checklistId, null, cancellationToken);

        _logger.LogInformation("Escalated case {CaseId} to Tier 2", caseId);

        return true;
    }
}
