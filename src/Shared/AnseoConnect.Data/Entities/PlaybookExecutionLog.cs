namespace AnseoConnect.Data.Entities;

/// <summary>
/// Audit log of playbook step execution attempts.
/// </summary>
public sealed class PlaybookExecutionLog : SchoolEntity
{
    public Guid LogId { get; set; }
    public Guid RunId { get; set; }
    public Guid StepId { get; set; }
    public string Channel { get; set; } = "SMS";
    public Guid? OutboxMessageId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = "SCHEDULED"; // SCHEDULED, SENT, SKIPPED, FAILED
    public string? SkipReason { get; set; }
    public DateTimeOffset ScheduledForUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExecutedAtUtc { get; set; }
}
