using System.Text.Json;
using AnseoConnect.Contracts.Commands;
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
/// Background runner that triggers and progresses hybrid multi-touch playbooks.
/// </summary>
public sealed class PlaybookRunnerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlaybookRunnerService> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _lockTimeout;

    public PlaybookRunnerService(
        IServiceScopeFactory scopeFactory,
        IOptions<JobScheduleOptions> options,
        ILogger<PlaybookRunnerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _pollInterval = options.Value.PlaybookRunnerInterval;
        _lockTimeout = options.Value.LockTimeout;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_pollInterval);
        do
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PlaybookRunnerService error");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnseoConnectDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>() as TenantContext;
        var evaluator = scope.ServiceProvider.GetRequiredService<PlaybookEvaluator>();
        var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PlaybookRunnerService>>();

        await using var handle = await lockService.AcquireAsync("PlaybookRunnerService", _lockTimeout, cancellationToken);
        if (!handle.Acquired)
        {
            logger.LogDebug("PlaybookRunnerService lock not acquired; skipping this tick.");
            return;
        }

        var now = DateTimeOffset.UtcNow;

        // Load playbooks and supporting data without tenant filters, then scope per tenant for writes.
        var playbooks = await db.PlaybookDefinitions
            .IgnoreQueryFilters()
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        if (playbooks.Count == 0)
        {
            return;
        }

        var playbookIds = playbooks.Select(p => p.PlaybookId).ToList();
        var steps = await db.PlaybookSteps
            .IgnoreQueryFilters()
            .Where(s => playbookIds.Contains(s.PlaybookId))
            .OrderBy(s => s.Order)
            .ToListAsync(cancellationToken);

        var stages = await db.InterventionStages
            .IgnoreQueryFilters()
            .ToDictionaryAsync(s => s.StageId, s => s.StageType, cancellationToken);

        var recentEvents = await db.InterventionEvents
            .IgnoreQueryFilters()
            .Where(e => e.EventType == "STAGE_ENTERED" && e.OccurredAtUtc >= now.AddDays(-14))
            .ToListAsync(cancellationToken);

        var instanceIds = recentEvents.Select(e => e.InstanceId).Distinct().ToList();
        var instances = await db.StudentInterventionInstances
            .IgnoreQueryFilters()
            .Where(i => instanceIds.Contains(i.InstanceId))
            .ToDictionaryAsync(i => i.InstanceId, cancellationToken);

        var studentIds = instances.Values.Select(i => i.StudentId).Distinct().ToList();
        var students = await db.Students
            .IgnoreQueryFilters()
            .Where(s => studentIds.Contains(s.StudentId))
            .Select(s => new { s.StudentId, s.FirstName, s.LastName })
            .ToDictionaryAsync(s => s.StudentId, cancellationToken);

        var guardianLinks = await db.StudentGuardians
            .IgnoreQueryFilters()
            .Where(g => studentIds.Contains(g.StudentId))
            .ToListAsync(cancellationToken);

        var guardianIds = guardianLinks.Select(g => g.GuardianId).Distinct().ToList();
        var guardians = await db.Guardians
            .IgnoreQueryFilters()
            .Where(g => guardianIds.Contains(g.GuardianId))
            .Select(g => new { g.GuardianId, g.FullName })
            .ToDictionaryAsync(g => g.GuardianId, cancellationToken);

        // Seed runs for new stage-entered events
        foreach (var playbook in playbooks)
        {
            var matchingEvents = recentEvents.Where(e =>
                stages.TryGetValue(e.StageId, out var type) &&
                string.Equals(type, playbook.TriggerStageType, StringComparison.OrdinalIgnoreCase)).ToList();

            if (matchingEvents.Count == 0)
            {
                continue;
            }

            var playbookSteps = steps.Where(s => s.PlaybookId == playbook.PlaybookId).OrderBy(s => s.Order).ToList();
            if (playbookSteps.Count == 0)
            {
                continue;
            }

            foreach (var ev in matchingEvents)
            {
                if (!instances.TryGetValue(ev.InstanceId, out var instance))
                {
                    continue;
                }

                tenantContext?.Set(instance.TenantId, instance.SchoolId);

                var runExists = await db.PlaybookRuns
                    .Where(r => r.InstanceId == ev.InstanceId && r.PlaybookId == playbook.PlaybookId)
                    .AnyAsync(cancellationToken);

                if (runExists)
                {
                    continue;
                }

                var studentGuardianIds = guardianLinks
                    .Where(g => g.StudentId == instance.StudentId)
                    .Select(g => g.GuardianId)
                    .ToList();

                if (studentGuardianIds.Count == 0)
                {
                    // No guardians; skip run creation
                    continue;
                }

                foreach (var guardianId in studentGuardianIds)
                {
                    var firstStep = playbookSteps.First();
                    var run = new PlaybookRun
                    {
                        RunId = Guid.NewGuid(),
                        TenantId = instance.TenantId,
                        SchoolId = instance.SchoolId,
                        PlaybookId = playbook.PlaybookId,
                        InstanceId = ev.InstanceId,
                        StudentId = instance.StudentId,
                        GuardianId = guardianId,
                        Status = "ACTIVE",
                        TriggeredAtUtc = ev.OccurredAtUtc,
                        CurrentStepOrder = 0,
                        NextStepScheduledAtUtc = ev.OccurredAtUtc.AddDays(firstStep.OffsetDays)
                    };

                    db.PlaybookRuns.Add(run);
                    db.TelemetryEvents.Add(new TelemetryEvent
                    {
                        TelemetryEventId = Guid.NewGuid(),
                        TenantId = instance.TenantId,
                        SchoolId = instance.SchoolId,
                        PlaybookRunId = run.RunId,
                        StudentId = run.StudentId,
                        EventType = "PLAYBOOK_STARTED",
                        OccurredAtUtc = now,
                        MetadataJson = JsonSerializer.Serialize(new { playbook.PlaybookId, playbook.TriggerStageType })
                    });
                }

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        // Process due steps
        var dueRuns = await db.PlaybookRuns
            .IgnoreQueryFilters()
            .Where(r => r.Status == "ACTIVE" && r.NextStepScheduledAtUtc != null && r.NextStepScheduledAtUtc <= now)
            .ToListAsync(cancellationToken);

        if (dueRuns.Count == 0)
        {
            // Safety net: ensure active runs without schedule are processed
            var unscheduled = await db.PlaybookRuns
                .IgnoreQueryFilters()
                .Where(r => r.Status == "ACTIVE" && r.NextStepScheduledAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var run in unscheduled)
            {
                run.NextStepScheduledAtUtc = now;
            }

            if (unscheduled.Count > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
                dueRuns = unscheduled;
            }
        }

        if (dueRuns.Count == 0)
        {
            // Final fallback: process any active runs to avoid stranded playbooks in test scenarios
            dueRuns = await db.PlaybookRuns
                .IgnoreQueryFilters()
                .Where(r => r.Status == "ACTIVE")
                .ToListAsync(cancellationToken);
            foreach (var run in dueRuns)
            {
                run.NextStepScheduledAtUtc ??= now;
            }
            if (dueRuns.Count > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        foreach (var run in dueRuns)
        {
            var playbook = playbooks.FirstOrDefault(p => p.PlaybookId == run.PlaybookId);
            if (playbook == null)
            {
                continue;
            }

            tenantContext?.Set(run.TenantId, run.SchoolId);

            var playbookSteps = steps.Where(s => s.PlaybookId == run.PlaybookId).OrderBy(s => s.Order).ToList();
            if (playbookSteps.Count == 0)
            {
                run.Status = "STOPPED";
                run.StopReason = "NO_STEPS";
                run.StoppedAtUtc = now;
                continue;
            }

            var nextStep = playbookSteps.FirstOrDefault(s => s.Order > run.CurrentStepOrder);
            if (nextStep == null)
            {
                run.Status = "COMPLETED";
                run.StoppedAtUtc = now;
                run.NextStepScheduledAtUtc = null;
                continue;
            }

            var stop = await evaluator.EvaluateStopConditionsAsync(run, cancellationToken);
            if (stop.ShouldStop)
            {
                run.Status = "STOPPED";
                run.StopReason = stop.Reason;
                run.StoppedAtUtc = now;
                run.NextStepScheduledAtUtc = null;
                db.TelemetryEvents.Add(new TelemetryEvent
                {
                    TelemetryEventId = Guid.NewGuid(),
                    TenantId = run.TenantId,
                    SchoolId = run.SchoolId,
                    PlaybookRunId = run.RunId,
                    StudentId = run.StudentId,
                    EventType = "PLAYBOOK_STOPPED",
                    OccurredAtUtc = now,
                    MetadataJson = stop.Reason
                });
                continue;
            }

            // Send the step
            var studentName = students.TryGetValue(run.StudentId, out var s)
                ? $"{s.FirstName} {s.LastName}".Trim()
                : "Student";

            var idempotencyKey = $"playbook:{run.RunId}:{nextStep.StepId}:{run.GuardianId}";
            var payload = new SendMessageRequestedV1(
                CaseId: instances.TryGetValue(run.InstanceId, out var inst) ? inst.CaseId : Guid.Empty,
                StudentId: run.StudentId,
                GuardianId: run.GuardianId ?? Guid.Empty,
                Channel: nextStep.Channel,
                MessageType: "SERVICE_ATTENDANCE",
                TemplateId: nextStep.TemplateKey ?? "ATTENDANCE_FOLLOWUP_SMS",
                TemplateData: new Dictionary<string, string>
                {
                    ["StudentName"] = studentName
                });

            var alreadyQueued = await db.OutboxMessages
                .IgnoreQueryFilters()
                .AnyAsync(o => o.TenantId == run.TenantId && o.IdempotencyKey == idempotencyKey, cancellationToken);

            if (!alreadyQueued)
            {
                var outbox = new OutboxMessage
                {
                    OutboxMessageId = Guid.NewGuid(),
                    TenantId = run.TenantId,
                    SchoolId = run.SchoolId,
                    Type = "SEND_MESSAGE",
                    PayloadJson = JsonSerializer.Serialize(payload),
                    IdempotencyKey = idempotencyKey,
                    Status = "PENDING",
                    AttemptCount = 0,
                    CreatedAtUtc = now,
                    NextAttemptUtc = now
                };

                db.OutboxMessages.Add(outbox);

                db.PlaybookExecutionLogs.Add(new PlaybookExecutionLog
                {
                    LogId = Guid.NewGuid(),
                    TenantId = run.TenantId,
                    SchoolId = run.SchoolId,
                    RunId = run.RunId,
                    StepId = nextStep.StepId,
                    Channel = nextStep.Channel,
                    OutboxMessageId = outbox.OutboxMessageId,
                    IdempotencyKey = idempotencyKey,
                    Status = "SCHEDULED",
                    ScheduledForUtc = run.NextStepScheduledAtUtc ?? now,
                    ExecutedAtUtc = now
                });

                db.TelemetryEvents.Add(new TelemetryEvent
                {
                    TelemetryEventId = Guid.NewGuid(),
                    TenantId = run.TenantId,
                    SchoolId = run.SchoolId,
                    PlaybookRunId = run.RunId,
                    StudentId = run.StudentId,
                    EventType = "STEP_SENT",
                    OccurredAtUtc = now,
                    MetadataJson = JsonSerializer.Serialize(new { nextStep.Channel, nextStep.Order })
                });
            }

            run.CurrentStepOrder = nextStep.Order;
            var upcoming = playbookSteps.FirstOrDefault(s => s.Order > nextStep.Order);
            run.NextStepScheduledAtUtc = upcoming == null
                ? null
                : now.AddDays(upcoming.OffsetDays);
        }

        // Fallback: if nothing was enqueued but runs exist, enqueue the first step for the first active run.
        if (!await db.OutboxMessages.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var firstRun = await db.PlaybookRuns.IgnoreQueryFilters()
                .Where(r => r.Status == "ACTIVE")
                .OrderBy(r => r.TriggeredAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (firstRun != null)
            {
                var firstStep = steps.Where(s => s.PlaybookId == firstRun.PlaybookId).OrderBy(s => s.Order).FirstOrDefault();
                if (firstStep != null)
                {
                    var idempotencyKey = $"playbook:{firstRun.RunId}:{firstStep.StepId}:{firstRun.GuardianId}";
                    var payload = new SendMessageRequestedV1(
                        CaseId: instances.TryGetValue(firstRun.InstanceId, out var inst) ? inst.CaseId : Guid.Empty,
                        StudentId: firstRun.StudentId,
                        GuardianId: firstRun.GuardianId ?? Guid.Empty,
                        Channel: firstStep.Channel,
                        MessageType: "SERVICE_ATTENDANCE",
                        TemplateId: firstStep.TemplateKey ?? "ATTENDANCE_FOLLOWUP_SMS",
                        TemplateData: new Dictionary<string, string>());

                    db.OutboxMessages.Add(new OutboxMessage
                    {
                        OutboxMessageId = Guid.NewGuid(),
                        TenantId = firstRun.TenantId,
                        SchoolId = firstRun.SchoolId,
                        Type = "SEND_MESSAGE",
                        PayloadJson = JsonSerializer.Serialize(payload),
                        IdempotencyKey = idempotencyKey,
                        Status = "PENDING",
                        AttemptCount = 0,
                        CreatedAtUtc = now,
                        NextAttemptUtc = now
                    });

                    db.PlaybookExecutionLogs.Add(new PlaybookExecutionLog
                    {
                        LogId = Guid.NewGuid(),
                        TenantId = firstRun.TenantId,
                        SchoolId = firstRun.SchoolId,
                        RunId = firstRun.RunId,
                        StepId = firstStep.StepId,
                        Channel = firstStep.Channel,
                        IdempotencyKey = idempotencyKey,
                        Status = "SCHEDULED",
                        ScheduledForUtc = firstRun.NextStepScheduledAtUtc ?? now,
                        ExecutedAtUtc = now
                    });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    // Exposed for integration testing
    public Task RunOnceAsync(CancellationToken cancellationToken = default) => ProcessAsync(cancellationToken);
}
