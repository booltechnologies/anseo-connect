using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Stage in an intervention ladder (Letter1, Letter2, Meeting, Escalation).
/// </summary>
public sealed class InterventionStage : ITenantScoped
{
    public Guid StageId { get; set; }
    public Guid TenantId { get; set; }

    public Guid RuleSetId { get; set; }
    public int Order { get; set; }
    public string StageType { get; set; } = "LETTER_1";
    public Guid? LetterTemplateId { get; set; }
    public int? DaysBeforeNextStage { get; set; }
    public string? StopConditionsJson { get; set; }
    public string? EscalationConditionsJson { get; set; }

    public InterventionRuleSet? RuleSet { get; set; }
}

