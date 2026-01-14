namespace AnseoConnect.Data.Entities;

/// <summary>
/// Detailed error record for a sync operation.
/// </summary>
public sealed class SyncError : SchoolEntity
{
    public Guid SyncErrorId { get; set; }

    /// <summary>
    /// Foreign key to the sync run.
    /// </summary>
    public Guid SyncRunId { get; set; }

    /// <summary>
    /// The entity type that failed (e.g., "Student", "Guardian").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// External ID from the SIS provider (if available).
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Error message or exception details.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Optional raw payload that caused the error (for debugging).
    /// </summary>
    public string? RawPayloadJson { get; set; }

    /// <summary>
    /// Timestamp when the error occurred.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Navigation property to the sync run.
    /// </summary>
    public SyncRun? SyncRun { get; set; }
}
