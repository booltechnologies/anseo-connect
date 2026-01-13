using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Configurable intervention rules per tenant/school (jurisdiction aware).
/// </summary>
public sealed class InterventionRuleSet : SchoolEntity
{
    public Guid RuleSetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = "IE";
    public bool IsActive { get; set; } = true;
    public string RulesJson { get; set; } = string.Empty;
}

