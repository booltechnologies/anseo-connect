using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Aggregates telemetry events into daily AutomationMetrics rows.
/// </summary>
public sealed class AutomationMetricsAggregator : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomationMetricsAggregator> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _lockTimeout;
    private const decimal MinutesPerManualTouch = 5m;

    public AutomationMetricsAggregator(
        IServiceScopeFactory scopeFactory,
        IOptions<JobScheduleOptions> options,
        ILogger<AutomationMetricsAggregator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = options.Value.AutomationMetricsInterval;
        _lockTimeout = options.Value.LockTimeout;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_interval);
        do
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutomationMetricsAggregator failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnseoConnectDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>() as TenantContext;
        var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AutomationMetricsAggregator>>();

        await using var handle = await lockService.AcquireAsync("AutomationMetricsAggregator", _lockTimeout, cancellationToken);
        if (!handle.Acquired)
        {
            logger.LogDebug("AutomationMetricsAggregator lock not acquired");
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var telemetry = await db.TelemetryEvents
            .IgnoreQueryFilters()
            .Where(t => DateOnly.FromDateTime(t.OccurredAtUtc.UtcDateTime) == today)
            .ToListAsync(cancellationToken);

        if (telemetry.Count == 0)
        {
            return;
        }

        var grouped = telemetry
            .GroupBy(t => new { t.TenantId, t.SchoolId })
            .ToList();

        foreach (var group in grouped)
        {
            tenantContext?.Set(group.Key.TenantId, group.Key.SchoolId);

            var playbooksStarted = group.Count(t => t.EventType == "PLAYBOOK_STARTED");
            var stepsSent = group.Count(t => t.EventType == "STEP_SENT");
            var stoppedByReply = group.Count(t => string.Equals(t.MetadataJson, "GUARDIAN_REPLIED", StringComparison.OrdinalIgnoreCase));
            var stoppedByImprovement = group.Count(t => string.Equals(t.MetadataJson, "ATTENDANCE_IMPROVED", StringComparison.OrdinalIgnoreCase));

            var metrics = await db.AutomationMetrics.FirstOrDefaultAsync(
                m => m.TenantId == group.Key.TenantId && m.SchoolId == group.Key.SchoolId && m.Date == today,
                cancellationToken);

            if (metrics == null)
            {
                metrics = new AutomationMetrics
                {
                    MetricsId = Guid.NewGuid(),
                    TenantId = group.Key.TenantId,
                    SchoolId = group.Key.SchoolId,
                    Date = today
                };
                db.AutomationMetrics.Add(metrics);
            }

            metrics.PlaybooksStarted = playbooksStarted;
            metrics.StepsScheduled = stepsSent;
            metrics.StepsSent = stepsSent;
            metrics.PlaybooksStoppedByReply = stoppedByReply;
            metrics.PlaybooksStoppedByImprovement = stoppedByImprovement;
            metrics.Escalations = metrics.Escalations; // unchanged for now
            metrics.EstimatedMinutesSaved = stepsSent * MinutesPerManualTouch;
            metrics.AttendanceImprovementDelta = metrics.AttendanceImprovementDelta; // placeholder until richer analytics
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
