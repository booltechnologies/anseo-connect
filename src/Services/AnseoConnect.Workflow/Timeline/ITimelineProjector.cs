using AnseoConnect.Data.Entities;

namespace AnseoConnect.Workflow.Timeline;

/// <summary>
/// Interface for projecting events from source entities into TimelineEvent records.
/// </summary>
public interface ITimelineProjector
{
    /// <summary>
    /// Gets the source entity type name (e.g., "Message", "InterventionEvent", "WorkTask").
    /// </summary>
    string SourceEntityType { get; }
    
    /// <summary>
    /// Gets the category for events from this projector (e.g., "COMMS", "INTERVENTION", "TASK").
    /// </summary>
    string Category { get; }
    
    /// <summary>
    /// Projects an entity into timeline events. Returns empty list if no events should be created.
    /// </summary>
    Task<IReadOnlyList<TimelineEvent>> ProjectAsync(object sourceEntity, CancellationToken cancellationToken = default);
}
