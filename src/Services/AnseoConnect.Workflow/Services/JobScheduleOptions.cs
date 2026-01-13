namespace AnseoConnect.Workflow.Services;

public sealed class JobScheduleOptions
{
    public TimeSpan InterventionSchedulerInterval { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan ScheduledReportsInterval { get; set; } = TimeSpan.FromHours(4);
    public TimeSpan PlaybookRunnerInterval { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan AutomationMetricsInterval { get; set; } = TimeSpan.FromHours(6);
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public bool EnablePlaybooks { get; set; } = false;
}

