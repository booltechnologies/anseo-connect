namespace AnseoConnect.Data.Entities;

/// <summary>
/// Tracks completion of checklist items for cases/tasks/alerts.
/// </summary>
public sealed class ChecklistCompletion : SchoolEntity
{
    public Guid ChecklistCompletionId { get; set; }

    public Guid CaseId { get; set; }
    public string ChecklistId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;

    public DateTimeOffset CompletedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CompletedByUserId { get; set; }
    public string? Notes { get; set; }

    public Guid? WorkTaskId { get; set; }
    public Guid? AlertId { get; set; }

    public Case? Case { get; set; }
    public WorkTask? WorkTask { get; set; }
    public SafeguardingAlert? Alert { get; set; }
}
