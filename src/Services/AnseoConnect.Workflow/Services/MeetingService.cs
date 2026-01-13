using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Manages intervention meetings and outcome-driven automation.
/// </summary>
public sealed class MeetingService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly CaseService _caseService;
    private readonly ILogger<MeetingService> _logger;

    public MeetingService(
        AnseoConnectDbContext dbContext,
        CaseService caseService,
        ILogger<MeetingService> logger)
    {
        _dbContext = dbContext;
        _caseService = caseService;
        _logger = logger;
    }

    public async Task<InterventionMeeting> ScheduleAsync(
        Guid instanceId,
        Guid stageId,
        DateTimeOffset scheduledAtUtc,
        Guid? createdByUserId,
        string? attendeesJson,
        CancellationToken cancellationToken = default)
    {
        var instance = await _dbContext.StudentInterventionInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InstanceId == instanceId, cancellationToken)
            ?? throw new InvalidOperationException($"Intervention instance {instanceId} not found.");

        var meeting = new InterventionMeeting
        {
            MeetingId = Guid.NewGuid(),
            TenantId = instance.TenantId,
            SchoolId = instance.SchoolId,
            InstanceId = instanceId,
            StageId = stageId,
            ScheduledAtUtc = scheduledAtUtc,
            Status = "SCHEDULED",
            AttendeesJson = attendeesJson,
            CreatedByUserId = createdByUserId
        };

        _dbContext.InterventionMeetings.Add(meeting);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Scheduled meeting {MeetingId} for instance {InstanceId}", meeting.MeetingId, instanceId);
        return meeting;
    }

    public async Task<InterventionMeeting> RecordOutcomeAsync(
        Guid meetingId,
        string status,
        string? outcomeCode,
        string? outcomeNotes,
        string? notesJson,
        CancellationToken cancellationToken = default)
    {
        var meeting = await _dbContext.InterventionMeetings
            .FirstOrDefaultAsync(m => m.MeetingId == meetingId, cancellationToken)
            ?? throw new InvalidOperationException($"Meeting {meetingId} not found.");

        meeting.Status = status;
        meeting.HeldAtUtc = DateTimeOffset.UtcNow;
        meeting.OutcomeCode = outcomeCode;
        meeting.OutcomeNotes = outcomeNotes;
        meeting.NotesJson = notesJson;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await ApplyOutcomeAutomationAsync(meeting, cancellationToken);

        _logger.LogInformation("Recorded outcome {Outcome} for meeting {MeetingId}", outcomeCode, meetingId);
        return meeting;
    }

    private async Task ApplyOutcomeAutomationAsync(InterventionMeeting meeting, CancellationToken cancellationToken)
    {
        var instance = await _dbContext.StudentInterventionInstances
            .FirstOrDefaultAsync(i => i.InstanceId == meeting.InstanceId, cancellationToken);

        if (instance == null)
        {
            return;
        }

        if (string.Equals(meeting.OutcomeCode, "SUPPORT_PLAN", StringComparison.OrdinalIgnoreCase))
        {
            var task = new WorkTask
            {
                WorkTaskId = Guid.NewGuid(),
                TenantId = instance.TenantId,
                SchoolId = instance.SchoolId,
                CaseId = instance.CaseId == Guid.Empty ? null : instance.CaseId,
                Title = "Follow-up after meeting",
                Status = "OPEN",
                DueAtUtc = DateTimeOffset.UtcNow.AddDays(7),
                Notes = meeting.OutcomeNotes
            };
            _dbContext.WorkTasks.Add(task);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (string.Equals(meeting.OutcomeCode, "ESCALATE", StringComparison.OrdinalIgnoreCase) && instance.CaseId != Guid.Empty)
        {
            await _caseService.EscalateToTier3Async(instance.CaseId, meeting.OutcomeNotes ?? "Escalated from meeting", null, cancellationToken);
        }
    }
}

