using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Outbound message record for tracking communications.
/// </summary>
public sealed class Message : SchoolEntity
{
    public Guid MessageId { get; set; }
    public Guid? ThreadId { get; set; }
    public Guid? CaseId { get; set; }
    public Guid StudentId { get; set; }
    public Guid GuardianId { get; set; }

    public string Direction { get; set; } = "OUTBOUND"; // OUTBOUND, INBOUND
    public string Channel { get; set; } = "SMS"; // SMS, EMAIL, WHATSAPP, VOICE_AUTODIAL
    public string MessageType { get; set; } = ""; // SERVICE_ATTENDANCE, etc.
    public string Status { get; set; } = "PENDING"; // PENDING, SENT, DELIVERED, FAILED, BLOCKED
    public string? ProviderMessageId { get; set; } // External provider ID (e.g., Twilio SID)
    public string? Provider { get; set; } // TWILIO, ZOHO, ACS
    public string Body { get; set; } = "";
    public string? TemplateId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAtUtc { get; set; }
    public DateTimeOffset? DeliveredAtUtc { get; set; }
    public bool IsRead { get; set; }

    public ConversationThread? Thread { get; set; }
    public Guardian? Guardian { get; set; }
    public ICollection<MessageLocalizedText> LocalizedTexts { get; set; } = new List<MessageLocalizedText>();
    public ICollection<MessageDeliveryAttempt> DeliveryAttempts { get; set; } = new List<MessageDeliveryAttempt>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
