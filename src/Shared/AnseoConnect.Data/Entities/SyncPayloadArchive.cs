namespace AnseoConnect.Data.Entities;

/// <summary>
/// Archives raw payloads from SIS sync operations for audit and debugging purposes.
/// Subject to retention policies for GDPR compliance.
/// </summary>
public sealed class SyncPayloadArchive : SchoolEntity
{
    public Guid ArchiveId { get; set; }

    /// <summary>
    /// Foreign key to the sync run.
    /// </summary>
    public Guid SyncRunId { get; set; }

    /// <summary>
    /// The entity type (e.g., "Student", "Contact", "Attendance").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// External ID from the SIS provider.
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// The raw JSON payload from the SIS API.
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the payload was captured.
    /// </summary>
    public DateTimeOffset CapturedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when this archive record should be deleted (retention policy).
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}
