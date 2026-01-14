namespace AnseoConnect.Contracts.SIS;

/// <summary>
/// Result of a sync operation.
/// </summary>
public sealed class SyncRunResult
{
    /// <summary>
    /// The sync run ID created for this operation.
    /// </summary>
    public Guid SyncRunId { get; set; }

    /// <summary>
    /// Whether the sync completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if sync failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of records inserted.
    /// </summary>
    public int InsertedCount { get; set; }

    /// <summary>
    /// Number of records updated.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Number of records skipped.
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Number of errors encountered.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Start time of the sync operation.
    /// </summary>
    public DateTimeOffset StartTimeUtc { get; set; }

    /// <summary>
    /// End time of the sync operation.
    /// </summary>
    public DateTimeOffset EndTimeUtc { get; set; }

    /// <summary>
    /// Duration of the sync operation.
    /// </summary>
    public TimeSpan Duration => EndTimeUtc - StartTimeUtc;
}
