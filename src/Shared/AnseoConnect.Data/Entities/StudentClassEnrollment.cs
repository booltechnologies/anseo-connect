namespace AnseoConnect.Data.Entities;

/// <summary>
/// Links students to classes/groups they are enrolled in.
/// </summary>
public sealed class StudentClassEnrollment : SchoolEntity
{
    public Guid EnrollmentId { get; set; }

    /// <summary>
    /// Foreign key to the student.
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// Foreign key to the class/group.
    /// </summary>
    public Guid ClassGroupId { get; set; }

    /// <summary>
    /// Start date of enrollment.
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// End date of enrollment (null if currently active).
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Whether this enrollment is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last time this enrollment was synced.
    /// </summary>
    public DateTimeOffset LastSyncedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Navigation property to the student.
    /// </summary>
    public Student? Student { get; set; }

    /// <summary>
    /// Navigation property to the class/group.
    /// </summary>
    public ClassGroup? ClassGroup { get; set; }
}
