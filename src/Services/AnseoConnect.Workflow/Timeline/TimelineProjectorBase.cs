using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using System.Text.Json;

namespace AnseoConnect.Workflow.Timeline;

/// <summary>
/// Base class for timeline projectors with common helper methods.
/// </summary>
public abstract class TimelineProjectorBase : ITimelineProjector
{
    public abstract string SourceEntityType { get; }
    public abstract string Category { get; }
    
    public abstract Task<IReadOnlyList<TimelineEvent>> ProjectAsync(object sourceEntity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a timeline event with common fields populated.
    /// </summary>
    protected TimelineEvent CreateEvent(
        Guid studentId,
        Guid? caseId,
        string eventType,
        DateTimeOffset occurredAtUtc,
        string? actorId = null,
        string? actorName = null,
        string? title = null,
        string? summary = null,
        object? metadata = null,
        string visibilityScope = "STANDARD")
    {
        var evt = new TimelineEvent
        {
            EventId = Guid.NewGuid(),
            StudentId = studentId,
            CaseId = caseId,
            EventType = eventType,
            Category = Category,
            SourceEntityType = SourceEntityType,
            OccurredAtUtc = occurredAtUtc,
            ActorId = actorId,
            ActorName = actorName,
            Title = title,
            Summary = summary,
            VisibilityScope = visibilityScope,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        
        if (metadata != null)
        {
            evt.MetadataJson = JsonSerializer.Serialize(metadata);
        }
        
        // Build searchable text from title, summary, and event type
        var searchParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(title))
            searchParts.Add(title);
        if (!string.IsNullOrWhiteSpace(summary))
            searchParts.Add(summary);
        if (!string.IsNullOrWhiteSpace(eventType))
            searchParts.Add(eventType);
        if (!string.IsNullOrWhiteSpace(actorName))
            searchParts.Add(actorName);
            
        evt.SearchableText = string.Join(" ", searchParts);
        
        return evt;
    }
    
    /// <summary>
    /// Sets the source entity ID on an event.
    /// </summary>
    protected void SetSourceEntityId(TimelineEvent evt, Guid sourceEntityId)
    {
        evt.SourceEntityId = sourceEntityId;
    }
}
