using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Timeline;

/// <summary>
/// Projects InterventionEvent entities into timeline events.
/// </summary>
public sealed class InterventionTimelineProjector : TimelineProjectorBase
{
    private readonly AnseoConnectDbContext _dbContext;
    
    public InterventionTimelineProjector(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public override string SourceEntityType => "InterventionEvent";
    public override string Category => "INTERVENTION";
    
    public override async Task<IReadOnlyList<TimelineEvent>> ProjectAsync(object sourceEntity, CancellationToken cancellationToken = default)
    {
        if (sourceEntity is not InterventionEvent interventionEvent)
        {
            return Array.Empty<TimelineEvent>();
        }
        
        // Get instance to find student and case
        var instance = await _dbContext.StudentInterventionInstances
            .AsNoTracking()
            .Where(i => i.InstanceId == interventionEvent.InstanceId)
            .Select(i => new { i.StudentId, i.CaseId })
            .FirstOrDefaultAsync(cancellationToken);
            
        if (instance == null)
        {
            return Array.Empty<TimelineEvent>();
        }
        
        var title = interventionEvent.EventType switch
        {
            "STAGE_ENTERED" => "Intervention stage entered",
            "LETTER_SENT" => "Intervention letter sent",
            "STAGE_ESCALATED" => "Intervention stage escalated",
            "STAGE_COMPLETED" => "Intervention stage completed",
            _ => interventionEvent.EventType.Replace("_", " ")
        };
        
        var evt = CreateEvent(
            instance.StudentId,
            instance.CaseId,
            interventionEvent.EventType,
            interventionEvent.OccurredAtUtc,
            title: title,
            summary: interventionEvent.EventType,
            metadata: new { interventionEvent.InstanceId, interventionEvent.StageId, interventionEvent.ArtifactId }
        );
        SetSourceEntityId(evt, interventionEvent.EventId);
        
        return new[] { evt };
    }
}
