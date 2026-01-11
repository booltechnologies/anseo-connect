using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Timeline event for a case.
/// </summary>
public sealed class CaseTimelineEvent : SchoolEntity
{
    public Guid EventId { get; set; }
    public Guid CaseId { get; set; }

    public string EventType { get; set; } = ""; // ABSENCE_DETECTED, MESSAGE_SENT, MESSAGE_REPLY, CASE_ESCALATED, etc.
    public string? EventData { get; set; } // JSON data
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; } // User ID or system identifier

    public Case? Case { get; set; }
}
