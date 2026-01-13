using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Library of available interventions mapped to MTSS tiers.
/// </summary>
public sealed class MtssIntervention : ITenantScoped
{
    public Guid InterventionId { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // COMMUNICATION, MEETING, SUPPORT_PLAN, REFERRAL
    public string ApplicableTiersJson { get; set; } = "[]"; // [1,2] or [2,3] - stored as JSON array
    public string EvidenceRequirementsJson { get; set; } = "[]";
    public bool RequiresParentConsent { get; set; }
    public int? TypicalDurationDays { get; set; }
    public bool IsActive { get; set; } = true;
}
