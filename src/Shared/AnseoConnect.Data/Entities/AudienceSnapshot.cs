using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Materialized list of recipients for a segment at send time.
/// </summary>
public sealed class AudienceSnapshot : ITenantScoped
{
    public Guid SnapshotId { get; set; }
    public Guid TenantId { get; set; }
    public Guid SegmentId { get; set; }
    public string RecipientIdsJson { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
