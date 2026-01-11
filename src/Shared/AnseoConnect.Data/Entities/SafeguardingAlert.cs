using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Safeguarding alert raised from policy triggers.
/// </summary>
public sealed class SafeguardingAlert : SchoolEntity
{
    public Guid AlertId { get; set; }
    public Guid CaseId { get; set; }

    public string Severity { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH, CRITICAL
    public string? ChecklistId { get; set; } // Policy pack checklist identifier
    public bool RequiresHumanReview { get; set; } = true;
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Case? Case { get; set; }
}
