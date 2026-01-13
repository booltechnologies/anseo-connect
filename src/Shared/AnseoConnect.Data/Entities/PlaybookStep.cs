using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Individual message step within a playbook sequence.
/// </summary>
public sealed class PlaybookStep : ITenantScoped
{
    public Guid StepId { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlaybookId { get; set; }
    public int Order { get; set; }
    public int OffsetDays { get; set; }
    public string Channel { get; set; } = "SMS"; // SMS, EMAIL, WHATSAPP, IN_APP
    public string? TemplateKey { get; set; }
    public string? FallbackChannel { get; set; }
    public bool SkipIfPreviousReplied { get; set; }
}
