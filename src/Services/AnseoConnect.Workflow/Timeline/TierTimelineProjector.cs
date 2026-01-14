using AnseoConnect.Data.Entities;

namespace AnseoConnect.Workflow.Timeline;

/// <summary>
/// Projects TierAssignmentHistory entities into timeline events.
/// </summary>
public sealed class TierTimelineProjector : TimelineProjectorBase
{
    public override string SourceEntityType => "TierAssignmentHistory";
    public override string Category => "TIER";
    
    public override Task<IReadOnlyList<TimelineEvent>> ProjectAsync(object sourceEntity, CancellationToken cancellationToken = default)
    {
        if (sourceEntity is not TierAssignmentHistory history)
        {
            return Task.FromResult<IReadOnlyList<TimelineEvent>>(Array.Empty<TimelineEvent>());
        }
        
        var title = history.ChangeType switch
        {
            "ENTRY" => $"Assigned to Tier {history.ToTier}",
            "ESCALATION" => $"Escalated from Tier {history.FromTier} to Tier {history.ToTier}",
            "DE_ESCALATION" => $"Moved from Tier {history.FromTier} to Tier {history.ToTier}",
            "EXIT" => $"Removed from Tier {history.FromTier}",
            _ => $"Tier changed from {history.FromTier} to {history.ToTier}"
        };
        
        var evt = CreateEvent(
            history.StudentId,
            history.CaseId,
            $"TIER_{history.ChangeType}",
            history.ChangedAtUtc,
            actorId: history.ChangedByUserId?.ToString(),
            title: title,
            summary: history.ChangeReason,
            metadata: new { history.FromTier, history.ToTier, history.ChangeType, history.ChangeReason }
        );
        SetSourceEntityId(evt, history.HistoryId);
        
        return Task.FromResult<IReadOnlyList<TimelineEvent>>(new[] { evt });
    }
}
