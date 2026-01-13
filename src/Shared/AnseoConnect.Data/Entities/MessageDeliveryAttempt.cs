using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Records a delivery attempt for a message with provider-specific metadata.
/// </summary>
public sealed class MessageDeliveryAttempt : SchoolEntity
{
    public Guid AttemptId { get; set; }
    public Guid MessageId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public string? ProviderMessageId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset AttemptedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? RawResponseJson { get; set; }

    public Message? Message { get; set; }
}
