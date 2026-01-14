using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Timeline;

/// <summary>
/// Projects CaseTimelineEvent entities into timeline events.
/// </summary>
public sealed class CaseEventTimelineProjector : TimelineProjectorBase
{
    private readonly AnseoConnectDbContext _dbContext;
    
    public CaseEventTimelineProjector(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public override string SourceEntityType => "CaseTimelineEvent";
    public override string Category => "CASE";
    
    public override async Task<IReadOnlyList<TimelineEvent>> ProjectAsync(object sourceEntity, CancellationToken cancellationToken = default)
    {
        if (sourceEntity is not CaseTimelineEvent caseEvent)
        {
            return Array.Empty<TimelineEvent>();
        }
        
        // Get case to find student ID
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .Where(c => c.CaseId == caseEvent.CaseId)
            .Select(c => new { c.StudentId })
            .FirstOrDefaultAsync(cancellationToken);
            
        if (caseEntity == null)
        {
            return Array.Empty<TimelineEvent>();
        }
        
        var evt = CreateEvent(
            caseEntity.StudentId,
            caseEvent.CaseId,
            caseEvent.EventType,
            caseEvent.CreatedAtUtc,
            actorId: caseEvent.CreatedBy,
            title: caseEvent.EventType.Replace("_", " "),
            summary: caseEvent.EventData,
            metadata: caseEvent.EventData != null ? new { EventData = caseEvent.EventData } : null
        );
        SetSourceEntityId(evt, caseEvent.EventId);
        
        return new[] { evt };
    }
}
