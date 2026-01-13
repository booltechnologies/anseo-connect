namespace AnseoConnect.Data.Entities;

/// <summary>
/// Work item attached to a case or operational workflow.
/// </summary>
public sealed class WorkTask : SchoolEntity
{
    public Guid WorkTaskId { get; set; }
    public Guid? CaseId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "OPEN"; // OPEN, COMPLETED, CANCELLED

    public Guid? AssignedToUserId { get; set; }
    public StaffRole? AssignedRole { get; set; }

    public DateTimeOffset? DueAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string? ChecklistId { get; set; }
    public string? ChecklistProgress { get; set; } // JSON blob to track progress
    public string? Notes { get; set; }

    public Case? Case { get; set; }
}
