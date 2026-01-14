using AnseoConnect.Data;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AnseoConnect.ApiGateway.Health;

/// <summary>
/// Health check for outbox queue status (DLQ count, pending message age).
/// </summary>
public sealed class OutboxHealthCheck : IHealthCheck
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<OutboxHealthCheck> _logger;

    private const int MaxDlqCount = 100; // Alert if DLQ > 100
    private const int MaxPendingHours = 24; // Alert if oldest pending > 24 hours

    public OutboxHealthCheck(
        AnseoConnectDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<OutboxHealthCheck> logger)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;

            // Count DLQ messages
            var dlqCount = await _dbContext.DeadLetterMessages
                .AsNoTracking()
                .Where(dlq => dlq.TenantId == tenantId)
                .CountAsync(cancellationToken);

            // Get oldest pending message age
            var oldestPending = await _dbContext.OutboxMessages
                .AsNoTracking()
                .Where(o => o.TenantId == tenantId && o.Status == "PENDING")
                .OrderBy(o => o.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["dlqCount"] = dlqCount,
                ["tenantId"] = tenantId.ToString()
            };

            var issues = new List<string>();

            if (dlqCount > MaxDlqCount)
            {
                issues.Add($"DLQ count ({dlqCount}) exceeds threshold ({MaxDlqCount})");
            }

            if (oldestPending != null)
            {
                var ageHours = (DateTimeOffset.UtcNow - oldestPending.CreatedAtUtc).TotalHours;
                data["oldestPendingAgeHours"] = ageHours;

                if (ageHours > MaxPendingHours)
                {
                    issues.Add($"Oldest pending message is {ageHours:F1} hours old (threshold: {MaxPendingHours} hours)");
                }
            }

            if (issues.Count > 0)
            {
                return HealthCheckResult.Degraded(
                    string.Join("; ", issues),
                    data: data);
            }

            return HealthCheckResult.Healthy("Outbox is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for outbox");
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
}
