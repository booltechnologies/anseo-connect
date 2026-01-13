using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Defines scheduled reports and parameters.
/// </summary>
public sealed class ReportDefinition : ITenantScoped
{
    public Guid DefinitionId { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = "ATTENDANCE_ANALYSIS";
    public string ScheduleCron { get; set; } = "0 0 1 */4 *";
    public bool IsActive { get; set; } = true;
    public string? ParametersJson { get; set; }
}

