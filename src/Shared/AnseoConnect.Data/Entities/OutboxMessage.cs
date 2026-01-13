using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Outbox message for reliable, idempotent dispatch to external providers.
/// </summary>
public sealed class OutboxMessage : ITenantScoped
{
    public Guid OutboxMessageId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? SchoolId { get; set; }
    public string Type { get; set; } = string.Empty; // e.g., SEND_MESSAGE
    public string PayloadJson { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString();
    public string Status { get; set; } = "PENDING"; // PENDING, PROCESSING, COMPLETED, FAILED
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptUtc { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
