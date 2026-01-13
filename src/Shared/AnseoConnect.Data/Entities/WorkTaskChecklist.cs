using TaskEntity = AnseoConnect.Data.Entities.WorkTask;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Checklist item attached to a work task.
/// </summary>
public sealed class WorkTaskChecklist : SchoolEntity
{
    public Guid WorkTaskChecklistId { get; set; }
    public Guid WorkTaskId { get; set; }

    public string ItemId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Required { get; set; } = true;

    public bool Completed { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public Guid? CompletedByUserId { get; set; }

    public TaskEntity? WorkTask { get; set; }
}
