using AnseoConnect.Data.Entities;

namespace AnseoConnect.Workflow.Timeline;

/// <summary>
/// Projects EvidencePack entities into timeline events.
/// </summary>
public sealed class EvidenceTimelineProjector : TimelineProjectorBase
{
    public override string SourceEntityType => "EvidencePack";
    public override string Category => "EVIDENCE";
    
    public override Task<IReadOnlyList<TimelineEvent>> ProjectAsync(object sourceEntity, CancellationToken cancellationToken = default)
    {
        if (sourceEntity is not EvidencePack evidencePack)
        {
            return Task.FromResult<IReadOnlyList<TimelineEvent>>(Array.Empty<TimelineEvent>());
        }
        
        var evt = CreateEvent(
            evidencePack.StudentId,
            evidencePack.CaseId,
            "EVIDENCE_PACK_GENERATED",
            evidencePack.GeneratedAtUtc,
            actorId: evidencePack.GeneratedByUserId.ToString(),
            title: $"Evidence pack generated ({evidencePack.Format})",
            summary: $"Evidence pack generated for {evidencePack.GenerationPurpose} covering {evidencePack.DateRangeStart:yyyy-MM-dd} to {evidencePack.DateRangeEnd:yyyy-MM-dd}",
            metadata: new 
            { 
                evidencePack.Format, 
                evidencePack.GenerationPurpose,
                evidencePack.DateRangeStart,
                evidencePack.DateRangeEnd,
                evidencePack.ContentHash
            },
            visibilityScope: "ADMIN_ONLY" // Evidence packs are sensitive
        );
        SetSourceEntityId(evt, evidencePack.EvidencePackId);
        
        return Task.FromResult<IReadOnlyList<TimelineEvent>>(new[] { evt });
    }
}
