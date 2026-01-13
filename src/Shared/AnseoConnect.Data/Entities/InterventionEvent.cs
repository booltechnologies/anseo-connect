using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Append-only log of intervention actions (stage enter, letter sent, escalation).
/// </summary>
public sealed class InterventionEvent : SchoolEntity
{
    public Guid EventId { get; set; }
    public Guid InstanceId { get; set; }
    public Guid StageId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? ArtifactId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

