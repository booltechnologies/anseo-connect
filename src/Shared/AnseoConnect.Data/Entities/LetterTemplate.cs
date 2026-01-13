using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Versioned letter templates with governance controls.
/// </summary>
public sealed class LetterTemplate : ITenantScoped
{
    public Guid TemplateId { get; set; }
    public Guid TenantId { get; set; }

    public string TemplateKey { get; set; } = string.Empty; // e.g., LETTER_1_ATTENDANCE
    public int Version { get; set; } = 1;
    public string Status { get; set; } = "DRAFT"; // DRAFT, APPROVED, RETIRED
    public string BodyHtml { get; set; } = string.Empty;
    public string? MergeFieldSchemaJson { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public string LockScope { get; set; } = "DISTRICT_ONLY";
}

