namespace AnseoConnect.Data.Entities;

/// <summary>
/// Daily aggregated automation metrics for ROI reporting.
/// </summary>
public sealed class AutomationMetrics : SchoolEntity
{
    public Guid MetricsId { get; set; }
    public DateOnly Date { get; set; }
    public int PlaybooksStarted { get; set; }
    public int StepsScheduled { get; set; }
    public int StepsSent { get; set; }
    public int PlaybooksStoppedByReply { get; set; }
    public int PlaybooksStoppedByImprovement { get; set; }
    public int Escalations { get; set; }
    public decimal EstimatedMinutesSaved { get; set; }
    public decimal AttendanceImprovementDelta { get; set; }
}
