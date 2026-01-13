using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Tracks engagement events for a message/guardian.
/// </summary>
public sealed class EngagementEvent : ITenantScoped
{
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }
    public Guid MessageId { get; set; }
    public Guid GuardianId { get; set; }
    public string EventType { get; set; } = "DELIVERED"; // DELIVERED, OPENED, CLICKED, REPLIED, FAILED, BOUNCED
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? MetadataJson { get; set; }
}
