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
/// Background service that reviews tier assignments and re-evaluates tier placement.
/// </summary>
public sealed class TierReviewService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TierReviewService> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _lockTimeout;

    public TierReviewService(
        IServiceScopeFactory scopeFactory,
        IOptions<JobScheduleOptions> options,
        ILogger<TierReviewService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = options.Value.InterventionSchedulerInterval; // Use same interval as intervention scheduler
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
                _logger.LogError(ex, "TierReviewService encountered an error");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnseoConnectDbContext>();
        var tierService = scope.ServiceProvider.GetRequiredService<MtssTierService>();
        var tierEvaluator = scope.ServiceProvider.GetRequiredService<TierEvaluator>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();

        await using var lockHandle = await lockService.AcquireAsync("TierReviewService", _lockTimeout, cancellationToken);
        if (!lockHandle.Acquired)
        {
            _logger.LogInformation("TierReviewService lock not acquired; skipping run.");
            return;
        }

        var now = DateTimeOffset.UtcNow;

        // Find assignments that are due for review
        var assignmentsDueForReview = await dbContext.TierAssignments
            .AsNoTracking()
            .Include(a => a.TierDefinition)
            .Where(a => a.NextReviewAtUtc.HasValue && a.NextReviewAtUtc <= now)
            .ToListAsync(cancellationToken);

        if (!assignmentsDueForReview.Any())
        {
            _logger.LogInformation("No tier assignments due for review.");
            return;
        }

        _logger.LogInformation("Reviewing {Count} tier assignments", assignmentsDueForReview.Count);

        foreach (var assignment in assignmentsDueForReview)
        {
            try
            {
                tenantContext.Set(assignment.TenantId, assignment.SchoolId);

                // Re-evaluate tier placement
                var evaluation = await tierService.EvaluateTierAsync(assignment.StudentId, assignment.CaseId, cancellationToken);

                // Check if should escalate
                var shouldEscalate = await tierEvaluator.ShouldEscalateAsync(assignment.StudentId, assignment, cancellationToken);
                if (shouldEscalate && evaluation.TierDefinitionId.HasValue)
                {
                    var tierDefinition = await dbContext.MtssTierDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.TierDefinitionId == evaluation.TierDefinitionId.Value, cancellationToken);

                    if (tierDefinition != null && tierDefinition.TierNumber > assignment.TierNumber)
                    {
                        var rationaleJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            evaluation.TriggeredConditions,
                            evaluation.AttendancePercent,
                            Reason = "AUTO_REVIEW_ESCALATION"
                        });

                        await tierService.AssignTierAsync(
                            assignment.CaseId,
                            tierDefinition.TierNumber,
                            "AUTO_REVIEW_ESCALATION",
                            rationaleJson,
                            null,
                            cancellationToken);

                        _logger.LogInformation("Auto-escalated case {CaseId} from tier {FromTier} to tier {ToTier}",
                            assignment.CaseId, assignment.TierNumber, tierDefinition.TierNumber);
                        continue;
                    }
                }

                // Check if should de-escalate
                var shouldDeEscalate = await tierEvaluator.MeetsExitCriteriaAsync(assignment.StudentId, assignment, cancellationToken);
                if (shouldDeEscalate && assignment.TierNumber > 1)
                {
                    var lowerTier = await dbContext.MtssTierDefinitions
                        .AsNoTracking()
                        .Where(t => t.TenantId == assignment.TenantId && t.TierNumber == assignment.TierNumber - 1 && t.IsActive)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (lowerTier != null)
                    {
                        var rationaleJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            evaluation.TriggeredConditions,
                            evaluation.AttendancePercent,
                            Reason = "AUTO_REVIEW_DE_ESCALATION"
                        });

                        await tierService.AssignTierAsync(
                            assignment.CaseId,
                            lowerTier.TierNumber,
                            "AUTO_REVIEW_DE_ESCALATION",
                            rationaleJson,
                            null,
                            cancellationToken);

                        _logger.LogInformation("Auto-de-escalated case {CaseId} from tier {FromTier} to tier {ToTier}",
                            assignment.CaseId, assignment.TierNumber, lowerTier.TierNumber);
                        continue;
                    }
                }

                // Update next review date if no change
                var currentAssignment = await dbContext.TierAssignments
                    .FirstOrDefaultAsync(a => a.AssignmentId == assignment.AssignmentId, cancellationToken);

                if (currentAssignment != null && currentAssignment.TierDefinition != null)
                {
                    currentAssignment.NextReviewAtUtc = now.AddDays(currentAssignment.TierDefinition.ReviewIntervalDays);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing tier assignment {AssignmentId}", assignment.AssignmentId);
            }
        }
    }
}
