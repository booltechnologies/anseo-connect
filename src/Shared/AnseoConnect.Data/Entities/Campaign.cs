using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Represents a campaign using a segment snapshot and a template version.
/// </summary>
public sealed class Campaign : ITenantScoped
{
    public Guid CampaignId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid SegmentId { get; set; }
    public Guid SnapshotId { get; set; }
    public Guid TemplateVersionId { get; set; }
    public string Status { get; set; } = "DRAFT"; // DRAFT, SCHEDULED, SENDING, COMPLETED
    public DateTimeOffset? ScheduledAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
