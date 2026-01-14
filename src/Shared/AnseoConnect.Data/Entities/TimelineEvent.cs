using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Unified timeline event for a student, aggregating events from attendance, communications, interventions, meetings, tasks, and evidence.
/// </summary>
public sealed class TimelineEvent : SchoolEntity
{
    public Guid EventId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? CaseId { get; set; }
    
    public string EventType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // ATTENDANCE, COMMS, INTERVENTION, MEETING, TASK, EVIDENCE, TIER, SAFEGUARDING, CASE
    public string SourceEntityType { get; set; } = string.Empty;
    public Guid SourceEntityId { get; set; }
    
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string? ActorId { get; set; }
    public string? ActorName { get; set; }
    
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? MetadataJson { get; set; }
    
    public string VisibilityScope { get; set; } = "STANDARD"; // STANDARD, SAFEGUARDING, ADMIN_ONLY
    public string? SearchableText { get; set; } // Denormalized for full-text search
    
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    
    public Student? Student { get; set; }
    public Case? Case { get; set; }
}
