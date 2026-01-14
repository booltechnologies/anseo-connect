namespace AnseoConnect.Data.Entities;

/// <summary>
/// Tracks sync state and watermarks per entity type for a school.
/// </summary>
public sealed class SchoolSyncState : SchoolEntity
{
    public Guid SchoolSyncStateId { get; set; }

    /// <summary>
    /// SIS provider identifier (e.g., "WONDE").
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Entity type (e.g., "Student", "Guardian", "Attendance", "Class").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Last successful sync watermark (timestamp of last successful sync).
    /// </summary>
    public DateTimeOffset? LastSyncWatermarkUtc { get; set; }

    /// <summary>
    /// Last time this entity type was successfully synced.
    /// </summary>
    public DateTimeOffset? LastSuccessfulSyncUtc { get; set; }

    /// <summary>
    /// Number of consecutive sync failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Last error message (if any).
    /// </summary>
    public string? LastError { get; set; }
}
