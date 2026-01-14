using AnseoConnect.Data;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AnseoConnect.ApiGateway.Health;

/// <summary>
/// Health check for message deliverability (recent failure rate).
/// </summary>
public sealed class DeliverabilityHealthCheck : IHealthCheck
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DeliverabilityHealthCheck> _logger;

    private const double MaxFailureRate = 0.10; // 10% failure rate threshold
    private const int LookbackHours = 24;

    public DeliverabilityHealthCheck(
        AnseoConnectDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<DeliverabilityHealthCheck> logger)
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
            var sinceUtc = DateTimeOffset.UtcNow.AddHours(-LookbackHours);

            // Count delivery attempts in the last 24 hours
            var totalAttempts = await _dbContext.MessageDeliveryAttempts
                .AsNoTracking()
                .Where(a => a.TenantId == tenantId && a.AttemptedAtUtc >= sinceUtc)
                .CountAsync(cancellationToken);

            var failedAttempts = await _dbContext.MessageDeliveryAttempts
                .AsNoTracking()
                .Where(a => a.TenantId == tenantId &&
                           a.AttemptedAtUtc >= sinceUtc &&
                           (a.Status == "FAILED" || a.Status == "BOUNCED" || a.Status == "REJECTED"))
                .CountAsync(cancellationToken);

            var failureRate = totalAttempts > 0 ? (double)failedAttempts / totalAttempts : 0.0;

            var data = new Dictionary<string, object>
            {
                ["totalAttempts"] = totalAttempts,
                ["failedAttempts"] = failedAttempts,
                ["failureRate"] = failureRate,
                ["threshold"] = MaxFailureRate,
                ["lookbackHours"] = LookbackHours
            };

            if (totalAttempts == 0)
            {
                return HealthCheckResult.Healthy("No delivery attempts in the last 24 hours", data);
            }

            if (failureRate > MaxFailureRate)
            {
                return HealthCheckResult.Degraded(
                    $"Message deliverability failure rate ({failureRate:P1}) exceeds threshold ({MaxFailureRate:P0})",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Message deliverability is healthy (failure rate: {failureRate:P1})",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for deliverability");
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
}
