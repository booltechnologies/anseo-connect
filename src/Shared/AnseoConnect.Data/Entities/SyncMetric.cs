namespace AnseoConnect.Data.Entities;

/// <summary>
/// Metrics for a specific entity type within a sync run.
/// </summary>
public sealed class SyncMetric : SchoolEntity
{
    public Guid SyncMetricId { get; set; }

    /// <summary>
    /// Foreign key to the sync run.
    /// </summary>
    public Guid SyncRunId { get; set; }

    /// <summary>
    /// The entity type being synced (e.g., "Student", "Guardian", "AttendanceMark").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Number of records inserted.
    /// </summary>
    public int InsertedCount { get; set; }

    /// <summary>
    /// Number of records updated.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Number of records skipped (e.g., duplicates, invalid data).
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Number of errors encountered for this entity type.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Navigation property to the sync run.
    /// </summary>
    public SyncRun? SyncRun { get; set; }
}
