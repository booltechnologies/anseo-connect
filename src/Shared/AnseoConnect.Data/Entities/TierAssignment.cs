namespace AnseoConnect.Data.Entities;

/// <summary>
/// Current tier assignment for a student/case.
/// </summary>
public sealed class TierAssignment : SchoolEntity
{
    public Guid AssignmentId { get; set; }
    public Guid StudentId { get; set; }
    public Guid CaseId { get; set; }
    public int TierNumber { get; set; }
    public Guid TierDefinitionId { get; set; }
    public string AssignmentReason { get; set; } = string.Empty; // AUTO_ESCALATED, MANUAL, RULE_TRIGGERED
    public string RationaleJson { get; set; } = "{}"; // explains why this tier was assigned
    public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? NextReviewAtUtc { get; set; }
    public Guid? AssignedByUserId { get; set; } // null if auto-assigned

    public Case? Case { get; set; }
    public MtssTierDefinition? TierDefinition { get; set; }
}
