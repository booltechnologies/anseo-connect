namespace AnseoConnect.Contracts.SIS;

/// <summary>
/// Options for sync operations.
/// </summary>
public sealed class SyncOptions
{
    /// <summary>
    /// If true, performs a full sync ignoring watermarks. Otherwise performs incremental sync.
    /// </summary>
    public bool ForceFullSync { get; set; }

    /// <summary>
    /// Timestamp to use for delta sync (only entities updated after this date).
    /// </summary>
    public DateTimeOffset? UpdatedAfter { get; set; }

    /// <summary>
    /// Whether to archive raw payloads for this sync run.
    /// </summary>
    public bool ArchivePayloads { get; set; } = true;

    /// <summary>
    /// Whether to store detailed metrics for this sync run.
    /// </summary>
    public bool StoreMetrics { get; set; } = true;
}
