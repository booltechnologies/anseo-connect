using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace AnseoConnect.ApiGateway.Services;

/// <summary>
/// Background service that evaluates alert rules periodically and creates alert instances.
/// </summary>
public sealed class AlertEvaluationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertEvaluationService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Run every 5 minutes

    public AlertEvaluationService(
        IServiceScopeFactory scopeFactory,
        ILogger<AlertEvaluationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_interval);
        do
        {
            try
            {
                await EvaluateAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AlertEvaluationService encountered an error");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task EvaluateAlertsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnseoConnectDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        // Get all enabled alert rules
        var rules = await dbContext.AlertRules
            .AsNoTracking()
            .Where(r => r.IsEnabled)
            .ToListAsync(cancellationToken);

        if (!rules.Any())
        {
            return;
        }

        // Group by tenant for efficient processing
        foreach (var ruleGroup in rules.GroupBy(r => r.TenantId))
        {
            var tenantId = ruleGroup.Key;

            // Set tenant context (school-scoped rules will be evaluated per school)
            if (tenantContext is TenantContext tc)
            {
                tc.Set(tenantId, null);
            }

            foreach (var rule in ruleGroup)
            {
                try
                {
                    var triggered = await EvaluateRuleAsync(dbContext, tenantId, rule, cancellationToken);
                    if (triggered)
                    {
                        await CreateAlertInstanceAsync(dbContext, tenantId, rule, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to evaluate alert rule {RuleId} ({RuleName})", rule.AlertRuleId, rule.Name);
                }
            }
        }
    }

    private async Task<bool> EvaluateRuleAsync(
        AnseoConnectDbContext dbContext,
        Guid tenantId,
        AlertRule rule,
        CancellationToken cancellationToken)
    {
        // Parse condition JSON to determine what to check
        // For now, implement basic checks based on category
        switch (rule.Category.ToUpperInvariant())
        {
            case "OUTBOX":
                return await EvaluateOutboxRuleAsync(dbContext, tenantId, rule, cancellationToken);
            case "DELIVERABILITY":
                return await EvaluateDeliverabilityRuleAsync(dbContext, tenantId, rule, cancellationToken);
            case "SIS":
                return await EvaluateSisRuleAsync(dbContext, tenantId, rule, cancellationToken);
            default:
                _logger.LogWarning("Unknown alert rule category: {Category}", rule.Category);
                return false;
        }
    }

    private async Task<bool> EvaluateOutboxRuleAsync(
        AnseoConnectDbContext dbContext,
        Guid tenantId,
        AlertRule rule,
        CancellationToken cancellationToken)
    {
        // Parse condition - example: { "maxDlqCount": 100, "maxPendingHours": 24 }
        var condition = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rule.ConditionJson) 
            ?? new Dictionary<string, JsonElement>();

        var maxDlqCount = condition.TryGetValue("maxDlqCount", out var dlqValue) 
            ? dlqValue.GetInt32() 
            : 100;

        var dlqCount = await dbContext.DeadLetterMessages
            .AsNoTracking()
            .Where(dlq => dlq.TenantId == tenantId)
            .CountAsync(cancellationToken);

        return dlqCount > maxDlqCount;
    }

    private async Task<bool> EvaluateDeliverabilityRuleAsync(
        AnseoConnectDbContext dbContext,
        Guid tenantId,
        AlertRule rule,
        CancellationToken cancellationToken)
    {
        var condition = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rule.ConditionJson) 
            ?? new Dictionary<string, JsonElement>();

        var maxFailureRate = condition.TryGetValue("maxFailureRate", out var rateValue) 
            ? rateValue.GetDouble() 
            : 0.10;

        var lookbackHours = condition.TryGetValue("lookbackHours", out var hoursValue) 
            ? hoursValue.GetInt32() 
            : 24;

        var sinceUtc = DateTimeOffset.UtcNow.AddHours(-lookbackHours);

        var totalAttempts = await dbContext.MessageDeliveryAttempts
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.AttemptedAtUtc >= sinceUtc)
            .CountAsync(cancellationToken);

        if (totalAttempts == 0)
        {
            return false;
        }

        var failedAttempts = await dbContext.MessageDeliveryAttempts
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId &&
                       a.AttemptedAtUtc >= sinceUtc &&
                       (a.Status == "FAILED" || a.Status == "BOUNCED" || a.Status == "REJECTED"))
            .CountAsync(cancellationToken);

        var failureRate = (double)failedAttempts / totalAttempts;
        return failureRate > maxFailureRate;
    }

    private async Task<bool> EvaluateSisRuleAsync(
        AnseoConnectDbContext dbContext,
        Guid tenantId,
        AlertRule rule,
        CancellationToken cancellationToken)
    {
        // SIS rule evaluation would integrate with ConnectorHealthService
        // For now, return false (not implemented)
        return false;
    }

    private async Task CreateAlertInstanceAsync(
        AnseoConnectDbContext dbContext,
        Guid tenantId,
        AlertRule rule,
        CancellationToken cancellationToken)
    {
        // Check if there's already an active alert for this rule
        var existingActive = await dbContext.AlertInstances
            .Where(a => a.TenantId == tenantId &&
                       a.AlertRuleId == rule.AlertRuleId &&
                       a.Status == "Active")
            .AnyAsync(cancellationToken);

        if (existingActive)
        {
            return; // Don't create duplicate active alerts
        }

        var instance = new AlertInstance
        {
            AlertInstanceId = Guid.NewGuid(),
            TenantId = tenantId,
            AlertRuleId = rule.AlertRuleId,
            TriggeredAtUtc = DateTimeOffset.UtcNow,
            Status = "Active",
            DetailsJson = "{}"
        };

        dbContext.AlertInstances.Add(instance);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alert triggered: {RuleName} (RuleId: {RuleId})", rule.Name, rule.AlertRuleId);

        // TODO: Send notifications via configured channels (email/webhook)
    }
}
