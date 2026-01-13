using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Materialized analytics for interventions.
/// </summary>
public sealed class InterventionAnalytics : SchoolEntity
{
    public Guid AnalyticsId { get; set; }
    public DateOnly Date { get; set; }
    public int TotalStudents { get; set; }
    public int StudentsInIntervention { get; set; }
    public int Letter1Sent { get; set; }
    public int Letter2Sent { get; set; }
    public int MeetingsScheduled { get; set; }
    public int MeetingsHeld { get; set; }
    public int Escalated { get; set; }
    public int Resolved { get; set; }
    public decimal PreInterventionAttendanceAvg { get; set; }
    public decimal PostInterventionAttendanceAvg { get; set; }
}

