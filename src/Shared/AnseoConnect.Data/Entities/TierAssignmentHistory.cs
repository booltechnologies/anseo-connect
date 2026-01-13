namespace AnseoConnect.Data.Entities;

/// <summary>
/// Audit trail of tier changes for a case.
/// </summary>
public sealed class TierAssignmentHistory : SchoolEntity
{
    public Guid HistoryId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid StudentId { get; set; }
    public Guid CaseId { get; set; }
    public int FromTier { get; set; }
    public int ToTier { get; set; }
    public string ChangeType { get; set; } = string.Empty; // ENTRY, ESCALATION, DE_ESCALATION, EXIT
    public string ChangeReason { get; set; } = string.Empty;
    public string RationaleJson { get; set; } = "{}";
    public Guid? ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public TierAssignment? Assignment { get; set; }
    public Case? Case { get; set; }
}
