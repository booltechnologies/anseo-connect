namespace AnseoConnect.Data.Entities;

/// <summary>
/// Represents a single sync operation run for a school.
/// </summary>
public sealed class SyncRun : SchoolEntity
{
    public Guid SyncRunId { get; set; }

    /// <summary>
    /// The SIS provider identifier (e.g., "WONDE").
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// The type of sync operation (e.g., "Roster", "Contacts", "Attendance").
    /// </summary>
    public string SyncType { get; set; } = string.Empty;

    /// <summary>
    /// Start time of the sync operation.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Completion time of the sync operation.
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>
    /// Status of the sync: RUNNING, SUCCEEDED, FAILED, CANCELLED
    /// </summary>
    public string Status { get; set; } = "RUNNING";

    /// <summary>
    /// Whether this was a full sync (ignoring watermarks).
    /// </summary>
    public bool WasFullSync { get; set; }

    /// <summary>
    /// Watermark timestamp used for delta sync (entities updated after this date).
    /// </summary>
    public DateTimeOffset? SyncWatermark { get; set; }

    /// <summary>
    /// Optional date parameter for attendance syncs.
    /// </summary>
    public DateOnly? AttendanceDate { get; set; }

    /// <summary>
    /// Summary notes about the sync run.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Navigation property to metrics for this sync run.
    /// </summary>
    public ICollection<SyncMetric> Metrics { get; set; } = new List<SyncMetric>();

    /// <summary>
    /// Navigation property to errors encountered during this sync run.
    /// </summary>
    public ICollection<SyncError> Errors { get; set; } = new List<SyncError>();
}
