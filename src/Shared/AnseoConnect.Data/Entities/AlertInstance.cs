using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Instance of a triggered alert rule.
/// </summary>
public sealed class AlertInstance : ITenantScoped
{
    public Guid AlertInstanceId { get; set; }
    public Guid TenantId { get; set; }
    public Guid AlertRuleId { get; set; }
    public AlertRule AlertRule { get; set; } = null!;
    public DateTimeOffset TriggeredAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public DateTimeOffset? AcknowledgedAtUtc { get; set; }
    public string? AcknowledgedBy { get; set; } // User ID
    public string Status { get; set; } = ""; // Active, Acknowledged, Resolved
    public string DetailsJson { get; set; } = "{}"; // Alert-specific details
}
