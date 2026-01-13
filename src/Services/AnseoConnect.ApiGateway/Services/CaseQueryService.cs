using AnseoConnect.Contracts.DTOs;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AnseoConnect.ApiGateway.Services;

/// <summary>
/// Service for querying cases and absences for staff endpoints.
/// Uses projection to DTOs and AsNoTracking for read-only queries.
/// </summary>
public sealed class CaseQueryService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<CaseQueryService> _logger;

    public CaseQueryService(AnseoConnectDbContext dbContext, ILogger<CaseQueryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets paginated list of open cases.
    /// </summary>
    public async Task<(List<CaseDto> Cases, int TotalCount)> GetOpenCasesAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Cases
            .AsNoTracking()
            .Where(c => c.Status == "OPEN")
            .Include(c => c.Student)
            .Include(c => c.TimelineEvents.OrderByDescending(e => e.CreatedAtUtc).Take(10))
            .OrderByDescending(c => c.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var cases = await query
            .Skip(skip)
            .Take(take)
            .Select(c => new CaseDto(
                CaseId: c.CaseId,
                StudentId: c.StudentId,
                StudentName: $"{c.Student!.FirstName} {c.Student.LastName}".Trim(),
                CaseType: c.CaseType,
                Tier: c.Tier,
                Status: c.Status,
                CreatedAtUtc: c.CreatedAtUtc,
                ResolvedAtUtc: c.ResolvedAtUtc,
                TimelineEvents: c.TimelineEvents
                    .OrderByDescending(e => e.CreatedAtUtc)
                    .Take(10)
                    .Select(e => new CaseTimelineEventDto(
                        EventId: e.EventId,
                        CaseId: e.CaseId,
                        EventType: e.EventType,
                        EventData: e.EventData,
                        CreatedAtUtc: e.CreatedAtUtc,
                        CreatedBy: e.CreatedBy
                    ))
                    .ToList()
            ))
            .ToListAsync(cancellationToken);

        return (cases, totalCount);
    }

    /// <summary>
    /// Gets case details with full timeline.
    /// </summary>
    public async Task<CaseDto?> GetCaseAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .Where(c => c.CaseId == caseId)
            .Include(c => c.Student)
            .Include(c => c.TimelineEvents.OrderByDescending(e => e.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        if (caseEntity == null)
        {
            return null;
        }

        return new CaseDto(
            CaseId: caseEntity.CaseId,
            StudentId: caseEntity.StudentId,
            StudentName: $"{caseEntity.Student!.FirstName} {caseEntity.Student.LastName}".Trim(),
            CaseType: caseEntity.CaseType,
            Tier: caseEntity.Tier,
            Status: caseEntity.Status,
            CreatedAtUtc: caseEntity.CreatedAtUtc,
            ResolvedAtUtc: caseEntity.ResolvedAtUtc,
            TimelineEvents: caseEntity.TimelineEvents
                .OrderByDescending(e => e.CreatedAtUtc)
                .Select(e => new CaseTimelineEventDto(
                    EventId: e.EventId,
                    CaseId: e.CaseId,
                    EventType: e.EventType,
                    EventData: e.EventData,
                    CreatedAtUtc: e.CreatedAtUtc,
                    CreatedBy: e.CreatedBy
                ))
                .ToList()
        );
    }

    /// <summary>
    /// Gets today's unexplained absences.
    /// </summary>
    public async Task<List<AbsenceDto>> GetTodayUnexplainedAbsencesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Get distinct unexplained absences for today
        // Note: Same student can have both AM and PM absences, so we group by StudentId+Date+Session
        var absencesQuery = _dbContext.AttendanceMarks
            .AsNoTracking()
            .Where(am => am.Date == today &&
                        (am.Status == "ABSENT" || am.Status == "UNKNOWN") &&
                        (am.ReasonCode == null || am.ReasonCode == ""))
            .Include(am => am.Student)
            .ToListAsync(cancellationToken);

        var allMarks = await absencesQuery;
        var absences = allMarks
            .GroupBy(am => new { am.StudentId, am.Date, am.Session })
            .Select(g =>
            {
                var first = g.First();
                var student = first.Student;
                return new AbsenceDto(
                    StudentId: g.Key.StudentId,
                    StudentName: student != null
                        ? $"{student.FirstName} {student.LastName}".Trim()
                        : "Unknown Student",
                    Date: g.Key.Date,
                    Session: g.Key.Session,
                    ReasonCode: first.ReasonCode
                );
            })
            .ToList();

        return absences;
    }

    /// <summary>
    /// Gets consent status for a guardian and channel.
    /// </summary>
    public async Task<ConsentStatusDto?> GetConsentStatusAsync(
        Guid guardianId,
        string channel,
        CancellationToken cancellationToken = default)
    {
        var consentState = await _dbContext.ConsentStates
            .AsNoTracking()
            .Where(c => c.GuardianId == guardianId && c.Channel == channel)
            .Include(c => c.Guardian)
            .FirstOrDefaultAsync(cancellationToken);

        if (consentState == null)
        {
            return new ConsentStatusDto(
                GuardianId: guardianId,
                GuardianName: "",
                Channel: channel,
                State: "UNKNOWN",
                LastUpdatedUtc: DateTimeOffset.UtcNow,
                Source: "SYSTEM"
            );
        }

        return new ConsentStatusDto(
            GuardianId: consentState.GuardianId,
            GuardianName: consentState.Guardian?.FullName ?? "",
            Channel: consentState.Channel,
            State: consentState.State,
            LastUpdatedUtc: consentState.LastUpdatedUtc,
            Source: consentState.Source
        );
    }

    /// <summary>
    /// Marks a checklist item complete for safeguarding alert or work task linked to the case.
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

        var completion = await _dbContext.ChecklistCompletions
            .FirstOrDefaultAsync(c =>
                c.CaseId == caseId &&
                c.ChecklistId == checklistId &&
                c.ItemId == itemId,
                cancellationToken);

        if (completion == null)
        {
            completion = new ChecklistCompletion
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
            _dbContext.ChecklistCompletions.Add(completion);
        }
        else
        {
            completion.CompletedAtUtc = DateTimeOffset.UtcNow;
            completion.CompletedByUserId = null;
            completion.Notes = notes;
            completion.AlertId = alert?.AlertId;
            completion.WorkTaskId = workTask?.WorkTaskId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Checklist item {ItemId} completed for case {CaseId} checklist {ChecklistId}", itemId, caseId, checklistId);
        return true;
    }
}
