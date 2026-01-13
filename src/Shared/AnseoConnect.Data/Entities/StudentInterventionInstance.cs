using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Tracks a student's progress through an intervention rule set.
/// </summary>
public sealed class StudentInterventionInstance : SchoolEntity
{
    public Guid InstanceId { get; set; }
    public Guid StudentId { get; set; }
    public Guid CaseId { get; set; }
    public Guid RuleSetId { get; set; }
    public Guid CurrentStageId { get; set; }
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, STOPPED, COMPLETED, ESCALATED
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastStageAtUtc { get; set; }
}

