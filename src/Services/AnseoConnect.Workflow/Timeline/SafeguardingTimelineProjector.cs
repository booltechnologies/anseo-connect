using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Timeline;

/// <summary>
/// Projects SafeguardingAlert entities into timeline events.
/// </summary>
public sealed class SafeguardingTimelineProjector : TimelineProjectorBase
{
    private readonly AnseoConnectDbContext _dbContext;
    
    public SafeguardingTimelineProjector(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public override string SourceEntityType => "SafeguardingAlert";
    public override string Category => "SAFEGUARDING";
    
    public override async Task<IReadOnlyList<TimelineEvent>> ProjectAsync(object sourceEntity, CancellationToken cancellationToken = default)
    {
        if (sourceEntity is not SafeguardingAlert alert)
        {
            return Array.Empty<TimelineEvent>();
        }
        
        // Get case to find student ID
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .Where(c => c.CaseId == alert.CaseId)
            .Select(c => new { c.StudentId })
            .FirstOrDefaultAsync(cancellationToken);
            
        if (caseEntity == null)
        {
            return Array.Empty<TimelineEvent>();
        }
        
        var evt = CreateEvent(
            caseEntity.StudentId,
            alert.CaseId,
            "SAFEGUARDING_ALERT",
            alert.CreatedAtUtc,
            title: $"Safeguarding alert ({alert.Severity})",
            summary: $"Safeguarding alert raised: {alert.Severity} severity",
            metadata: new { alert.Severity, alert.RequiresHumanReview, alert.ChecklistId },
            visibilityScope: "SAFEGUARDING" // Restricted visibility
        );
        SetSourceEntityId(evt, alert.AlertId);
        
        // Project review event if reviewed
        var events = new List<TimelineEvent> { evt };
        
        if (alert.ReviewedAtUtc.HasValue)
        {
            var reviewEvt = CreateEvent(
                caseEntity.StudentId,
                alert.CaseId,
                "SAFEGUARDING_ALERT_REVIEWED",
                alert.ReviewedAtUtc.Value,
                actorId: alert.ReviewedBy,
                title: $"Safeguarding alert reviewed",
                summary: alert.ReviewNotes,
                metadata: new { alert.ReviewedBy, alert.ReviewNotes },
                visibilityScope: "SAFEGUARDING"
            );
            SetSourceEntityId(reviewEvt, alert.AlertId);
            events.Add(reviewEvt);
        }
        
        return events;
    }
}
