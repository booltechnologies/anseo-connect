using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Configurable MTSS tier definitions per tenant.
/// </summary>
public sealed class MtssTierDefinition : ITenantScoped
{
    public Guid TierDefinitionId { get; set; }
    public Guid TenantId { get; set; }

    public int TierNumber { get; set; } // 1, 2, 3
    public string Name { get; set; } = string.Empty; // "Universal", "Targeted", "Intensive"
    public string Description { get; set; } = string.Empty;
    public string EntryCriteriaJson { get; set; } = "{}"; // conditions that place a student in this tier
    public string ExitCriteriaJson { get; set; } = "{}"; // conditions that move student down a tier
    public string EscalationCriteriaJson { get; set; } = "{}"; // conditions that move student up
    public int ReviewIntervalDays { get; set; } = 30; // how often to re-evaluate tier placement
    public string RequiredArtifactsJson { get; set; } = "[]"; // what must be documented at this tier
    public string RecommendedInterventionsJson { get; set; } = "[]"; // intervention IDs mapped to this tier
    public bool IsActive { get; set; } = true;
}
