using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Timeline;

/// <summary>
/// Projects InterventionMeeting entities into timeline events.
/// </summary>
public sealed class MeetingTimelineProjector : TimelineProjectorBase
{
    private readonly AnseoConnectDbContext _dbContext;
    
    public MeetingTimelineProjector(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public override string SourceEntityType => "InterventionMeeting";
    public override string Category => "MEETING";
    
    public override async Task<IReadOnlyList<TimelineEvent>> ProjectAsync(object sourceEntity, CancellationToken cancellationToken = default)
    {
        if (sourceEntity is not InterventionMeeting meeting)
        {
            return Array.Empty<TimelineEvent>();
        }
        
        // Get instance to find student and case
        var instance = await _dbContext.StudentInterventionInstances
            .AsNoTracking()
            .Where(i => i.InstanceId == meeting.InstanceId)
            .Select(i => new { i.StudentId, i.CaseId })
            .FirstOrDefaultAsync(cancellationToken);
            
        if (instance == null)
        {
            return Array.Empty<TimelineEvent>();
        }
        
        var events = new List<TimelineEvent>();
        
        // Project meeting scheduled event
        var scheduledEvt = CreateEvent(
            instance.StudentId,
            instance.CaseId,
            "MEETING_SCHEDULED",
            meeting.ScheduledAtUtc,
            actorId: meeting.CreatedByUserId?.ToString(),
            title: "Meeting scheduled",
            summary: $"Intervention meeting scheduled for {meeting.ScheduledAtUtc:yyyy-MM-dd}",
            metadata: new { meeting.Status, meeting.AttendeesJson }
        );
        SetSourceEntityId(scheduledEvt, meeting.MeetingId);
        events.Add(scheduledEvt);
        
        // Project meeting held event if held
        if (meeting.Status == "HELD" && meeting.HeldAtUtc.HasValue)
        {
            var heldEvt = CreateEvent(
                instance.StudentId,
                instance.CaseId,
                "MEETING_HELD",
                meeting.HeldAtUtc.Value,
                actorId: meeting.CreatedByUserId?.ToString(),
                title: "Meeting held",
                summary: meeting.OutcomeNotes ?? "Meeting held",
                metadata: new { meeting.Status, meeting.OutcomeCode, meeting.OutcomeNotes, meeting.AttendeesJson }
            );
            SetSourceEntityId(heldEvt, meeting.MeetingId);
            events.Add(heldEvt);
        }
        
        return events;
    }
}
