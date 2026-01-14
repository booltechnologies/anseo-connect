namespace AnseoConnect.Data.Entities;

/// <summary>
/// Records health and outcomes of ingestion runs per school.
/// </summary>
public sealed class IngestionSyncLog : SchoolEntity
{
    public Guid IngestionSyncLogId { get; set; }

    public string Source { get; set; } = "WONDE";
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string Status { get; set; } = "RUNNING"; // RUNNING, SUCCEEDED, FAILED

    public int RecordsProcessed { get; set; }
    public int ErrorCount { get; set; }
    public int MismatchCount { get; set; }

    /// <summary>
    /// Alert threshold for error rate (percentage).
    /// </summary>
    public decimal? ErrorRateThreshold { get; set; }

    /// <summary>
    /// Alert threshold for mismatch count.
    /// </summary>
    public int? MismatchThreshold { get; set; }

    /// <summary>
    /// Details about mismatches encountered (JSON format).
    /// </summary>
    public string? MismatchDetailsJson { get; set; }

    public string? Notes { get; set; }
}
