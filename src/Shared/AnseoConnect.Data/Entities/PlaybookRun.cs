namespace AnseoConnect.Data.Entities;

/// <summary>
/// Tracks the execution of a playbook for a specific student/guardian.
/// </summary>
public sealed class PlaybookRun : SchoolEntity
{
    public Guid RunId { get; set; }
    public Guid PlaybookId { get; set; }
    public Guid InstanceId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? GuardianId { get; set; }
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, STOPPED, COMPLETED, ESCALATED
    public DateTimeOffset TriggeredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StoppedAtUtc { get; set; }
    public string? StopReason { get; set; }
    public int CurrentStepOrder { get; set; }
    public DateTimeOffset? NextStepScheduledAtUtc { get; set; }
}
