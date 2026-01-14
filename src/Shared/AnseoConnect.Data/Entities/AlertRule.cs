using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Configurable alert rule for monitoring system health and operations.
/// </summary>
public sealed class AlertRule : ITenantScoped
{
    public Guid AlertRuleId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = ""; // SIS, Outbox, Deliverability, etc.
    public string ConditionJson { get; set; } = ""; // Threshold conditions as JSON
    public string Severity { get; set; } = ""; // Info, Warning, Critical
    public bool IsEnabled { get; set; } = true;
    public string NotificationChannelsJson { get; set; } = "[]"; // Email addresses, webhook URLs as JSON array
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
