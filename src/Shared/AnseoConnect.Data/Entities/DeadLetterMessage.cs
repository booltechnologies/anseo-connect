using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Dead letter entry for exhausted outbox messages.
/// </summary>
public sealed class DeadLetterMessage : ITenantScoped
{
    public Guid DeadLetterId { get; set; }
    public Guid TenantId { get; set; }
    public Guid OriginalOutboxId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public DateTimeOffset FailedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReplayedAtUtc { get; set; }
    public Guid? ReplayedByUserId { get; set; }
}
