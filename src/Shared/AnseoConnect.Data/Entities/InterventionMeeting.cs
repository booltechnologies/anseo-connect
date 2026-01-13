using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Meeting attached to an intervention instance and stage.
/// </summary>
public sealed class InterventionMeeting : SchoolEntity
{
    public Guid MeetingId { get; set; }
    public Guid InstanceId { get; set; }
    public Guid StageId { get; set; }
    public DateTimeOffset ScheduledAtUtc { get; set; }
    public DateTimeOffset? HeldAtUtc { get; set; }
    public string Status { get; set; } = "SCHEDULED"; // SCHEDULED, HELD, CANCELLED, NO_SHOW
    public string? AttendeesJson { get; set; }
    public string? NotesJson { get; set; }
    public string? OutcomeCode { get; set; }
    public string? OutcomeNotes { get; set; }
    public Guid? CreatedByUserId { get; set; }
}

