namespace AnseoConnect.Data.Entities;

/// <summary>
/// Atomic telemetry event used for automation ROI calculations.
/// </summary>
public sealed class TelemetryEvent : SchoolEntity
{
    public Guid TelemetryEventId { get; set; }
    public string EventType { get; set; } = string.Empty; // PLAYBOOK_STARTED, STEP_SENT, PLAYBOOK_STOPPED, ATTENDANCE_IMPROVED
    public Guid? PlaybookRunId { get; set; }
    public Guid? StudentId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
