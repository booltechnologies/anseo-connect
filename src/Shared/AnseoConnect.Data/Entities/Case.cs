using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Attendance or safeguarding case for a student.
/// </summary>
public sealed class Case : SchoolEntity
{
    public Guid CaseId { get; set; }
    public Guid StudentId { get; set; }

    public string CaseType { get; set; } = "ATTENDANCE"; // ATTENDANCE, SAFEGUARDING
    public int Tier { get; set; } = 1; // 1, 2, 3
    public string Status { get; set; } = "OPEN"; // OPEN, CLOSED, ESCALATED
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public string? ResolvedBy { get; set; }
    public string? ResolutionNotes { get; set; }

    public Student? Student { get; set; }
    public ICollection<CaseTimelineEvent> TimelineEvents { get; set; } = new List<CaseTimelineEvent>();
    public ICollection<SafeguardingAlert> SafeguardingAlerts { get; set; } = new List<SafeguardingAlert>();
}
