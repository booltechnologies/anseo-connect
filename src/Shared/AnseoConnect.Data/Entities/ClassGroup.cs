namespace AnseoConnect.Data.Entities;

/// <summary>
/// Represents a class or group in the school system, normalized across SIS providers.
/// </summary>
public sealed class ClassGroup : SchoolEntity
{
    public Guid ClassGroupId { get; set; }

    /// <summary>
    /// External class ID from the SIS provider.
    /// </summary>
    public string ExternalClassId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the class/group.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional code or identifier for the class.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Academic year this class belongs to.
    /// </summary>
    public string? AcademicYear { get; set; }

    /// <summary>
    /// Whether this class is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last time this class was synced.
    /// </summary>
    public DateTimeOffset LastSyncedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Source/provider where this class came from.
    /// </summary>
    public string Source { get; set; } = "WONDE";

    /// <summary>
    /// Navigation property to student enrollments.
    /// </summary>
    public ICollection<StudentClassEnrollment> StudentEnrollments { get; set; } = new List<StudentClassEnrollment>();
}
