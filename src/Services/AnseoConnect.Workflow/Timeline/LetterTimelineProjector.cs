using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Timeline;

/// <summary>
/// Projects LetterArtifact entities into timeline events.
/// </summary>
public sealed class LetterTimelineProjector : TimelineProjectorBase
{
    private readonly AnseoConnectDbContext _dbContext;
    
    public LetterTimelineProjector(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public override string SourceEntityType => "LetterArtifact";
    public override string Category => "INTERVENTION";
    
    public override async Task<IReadOnlyList<TimelineEvent>> ProjectAsync(object sourceEntity, CancellationToken cancellationToken = default)
    {
        if (sourceEntity is not LetterArtifact letter)
        {
            return Array.Empty<TimelineEvent>();
        }
        
        // Get instance to find student and case
        var instance = await _dbContext.StudentInterventionInstances
            .AsNoTracking()
            .Where(i => i.InstanceId == letter.InstanceId)
            .Select(i => new { i.StudentId, i.CaseId })
            .FirstOrDefaultAsync(cancellationToken);
            
        if (instance == null)
        {
            return Array.Empty<TimelineEvent>();
        }
        
        var evt = CreateEvent(
            instance.StudentId,
            instance.CaseId,
            "LETTER_GENERATED",
            letter.GeneratedAtUtc,
            title: $"Intervention letter generated ({letter.LanguageCode})",
            summary: $"Letter artifact generated for guardian",
            metadata: new { letter.TemplateId, letter.TemplateVersion, letter.LanguageCode, letter.GuardianId, letter.ContentHash }
        );
        SetSourceEntityId(evt, letter.ArtifactId);
        
        return new[] { evt };
    }
}
