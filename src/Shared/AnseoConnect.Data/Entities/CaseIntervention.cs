namespace AnseoConnect.Data.Entities;

/// <summary>
/// Interventions applied to a case.
/// </summary>
public sealed class CaseIntervention : SchoolEntity
{
    public Guid CaseInterventionId { get; set; }
    public Guid CaseId { get; set; }
    public Guid InterventionId { get; set; }
    public int TierWhenApplied { get; set; }
    public string Status { get; set; } = "PLANNED"; // PLANNED, ACTIVE, COMPLETED, CANCELLED
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? OutcomeNotes { get; set; }
    public Guid? AssignedToUserId { get; set; }

    public Case? Case { get; set; }
    public MtssIntervention? Intervention { get; set; }
}
