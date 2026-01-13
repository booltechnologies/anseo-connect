using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Communication template (SMS/Email/WhatsApp) with variables.
/// </summary>
public sealed class MessageTemplate : ITenantScoped
{
    public Guid MessageTemplateId { get; set; }
    public Guid TenantId { get; set; }

    public string TemplateKey { get; set; } = string.Empty; // e.g., ABSENCE_FIRST_CONTACT
    public string Channel { get; set; } = "SMS"; // SMS, EMAIL, WHATSAPP, VOICE_AUTODIAL
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? ToneConstraints { get; set; } // JSON array of constraints
    public int? MaxLength { get; set; }
    public int Version { get; set; } = 1;
    public string Status { get; set; } = "DRAFT"; // DRAFT, PENDING_APPROVAL, APPROVED, RETIRED
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public string? MergeFieldSchemaJson { get; set; }
    public string LockScope { get; set; } = "DISTRICT_ONLY"; // DISTRICT_ONLY, SCHOOL_OVERRIDE_ALLOWED
    public Guid? ParentTemplateId { get; set; }
}
