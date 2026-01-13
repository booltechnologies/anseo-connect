using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Definition of an automated multi-touch playbook triggered by an intervention stage.
/// </summary>
public sealed class PlaybookDefinition : ITenantScoped
{
    public Guid PlaybookId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? SchoolId { get; set; } // null = tenant-wide
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TriggerStageType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string StopConditionsJson { get; set; } = "[]";
    public string EscalationConditionsJson { get; set; } = "[]";
    public int EscalationAfterDays { get; set; } = 7;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
