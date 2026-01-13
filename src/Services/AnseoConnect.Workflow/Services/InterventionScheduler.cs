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
/// Background scheduler that evaluates rule sets daily and seeds intervention instances.
/// </summary>
public sealed class InterventionScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InterventionScheduler> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _lockTimeout;

    public InterventionScheduler(IServiceScopeFactory scopeFactory, IOptions<JobScheduleOptions> options, ILogger<InterventionScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = options.Value.InterventionSchedulerInterval;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "InterventionScheduler encountered an error");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnseoConnectDbContext>();
        var ruleEngine = scope.ServiceProvider.GetRequiredService<InterventionRuleEngine>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<InterventionScheduler>>();
        var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();

        await using var lockHandle = await lockService.AcquireAsync("InterventionScheduler", _lockTimeout, cancellationToken);
        if (!lockHandle.Acquired)
        {
            logger.LogInformation("InterventionScheduler lock not acquired; skipping run.");
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var ruleSets = await dbContext.InterventionRuleSets
            .AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        if (!ruleSets.Any())
        {
            logger.LogInformation("No active intervention rule sets found; skipping evaluation.");
            return;
        }

        var stages = await dbContext.InterventionStages
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var group in ruleSets.GroupBy(r => new { r.TenantId, r.SchoolId }))
        {
            tenantContext.Set(group.Key.TenantId, group.Key.SchoolId);

            foreach (var ruleSet in group)
            {
                var eligible = await ruleEngine.EvaluateAsync(ruleSet.SchoolId, today, cancellationToken);
                var firstStage = stages
                    .Where(s => s.RuleSetId == ruleSet.RuleSetId)
                    .OrderBy(s => s.Order)
                    .FirstOrDefault();

                foreach (var student in eligible.Where(e => e.RuleSetId == ruleSet.RuleSetId))
                {
                    var existingInstance = await dbContext.StudentInterventionInstances
                        .Where(i => i.StudentId == student.StudentId && i.RuleSetId == ruleSet.RuleSetId && i.Status == "ACTIVE")
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existingInstance != null)
                    {
                        continue;
                    }

                    var instance = new StudentInterventionInstance
                    {
                        InstanceId = Guid.NewGuid(),
                        TenantId = ruleSet.TenantId,
                        SchoolId = ruleSet.SchoolId,
                        StudentId = student.StudentId,
                        CaseId = Guid.Empty,
                        RuleSetId = ruleSet.RuleSetId,
                        CurrentStageId = firstStage?.StageId ?? Guid.Empty,
                        Status = "ACTIVE",
                        StartedAtUtc = DateTimeOffset.UtcNow,
                        LastStageAtUtc = null
                    };

                    dbContext.StudentInterventionInstances.Add(instance);

                    dbContext.InterventionEvents.Add(new InterventionEvent
                    {
                        EventId = Guid.NewGuid(),
                        TenantId = ruleSet.TenantId,
                        SchoolId = ruleSet.SchoolId,
                        InstanceId = instance.InstanceId,
                        StageId = firstStage?.StageId ?? Guid.Empty,
                        EventType = "STAGE_ENTERED",
                        OccurredAtUtc = DateTimeOffset.UtcNow
                    });
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        logger.LogInformation("InterventionScheduler completed evaluation for {Date}", today);
    }
}

