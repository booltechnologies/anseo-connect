using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.PolicyRuntime;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Service for evaluating safeguarding triggers and creating alerts.
/// </summary>
public sealed class SafeguardingService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ISafeguardingEvaluator _safeguardingEvaluator;
    private readonly ILogger<SafeguardingService> _logger;
    private readonly ITenantContext _tenantContext;
    private readonly NotificationRoutingService _notificationRouting;

    public SafeguardingService(
        AnseoConnectDbContext dbContext,
        ISafeguardingEvaluator safeguardingEvaluator,
        ILogger<SafeguardingService> logger,
        ITenantContext tenantContext,
        NotificationRoutingService notificationRouting)
    {
        _dbContext = dbContext;
        _safeguardingEvaluator = safeguardingEvaluator;
        _logger = logger;
        _tenantContext = tenantContext;
        _notificationRouting = notificationRouting;
    }

    /// <summary>
    /// Evaluates safeguarding triggers for a case and creates alerts if needed.
    /// </summary>
    public async Task<SafeguardingAlert?> EvaluateAndCreateAlertAsync(
        Guid caseId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Evaluating safeguarding triggers for case {CaseId}, student {StudentId}", caseId, studentId);

        // Load tenant to get policy pack info
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.TenantId == _tenantContext.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant {TenantId} not found", _tenantContext.TenantId);
            return null;
        }

        // Load policy pack JSON
        var policyPackPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "policy-packs",
            tenant.CountryCode,
            tenant.DefaultPolicyPackId,
            tenant.DefaultPolicyPackVersion,
            "safeguarding.json");

        if (!File.Exists(policyPackPath))
        {
            _logger.LogWarning("Policy pack not found at {Path}", policyPackPath);
            return null;
        }

        var policyPackJson = await File.ReadAllTextAsync(policyPackPath, cancellationToken);
        var policyPackDoc = JsonDocument.Parse(policyPackJson);
        var policyPackRoot = policyPackDoc.RootElement;

        // Compute metrics for safeguarding evaluation
        var metrics = await ComputeMetricsAsync(studentId, caseId, cancellationToken);

        // Evaluate safeguarding triggers
        var result = _safeguardingEvaluator.Evaluate(policyPackRoot, metrics);

        if (!result.CreateAlert)
        {
            _logger.LogInformation("No safeguarding alert triggered for case {CaseId}", caseId);
            return null;
        }

        // Create safeguarding alert
        var alert = new SafeguardingAlert
        {
            CaseId = caseId,
            Severity = result.Severity ?? "MEDIUM",
            ChecklistId = result.ChecklistId,
            RequiresHumanReview = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.SafeguardingAlerts.Add(alert);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created safeguarding alert {AlertId} for case {CaseId}, severity {Severity}",
            alert.AlertId,
            caseId,
            alert.Severity);

        await _notificationRouting.RouteAsync(
            route: "SAFEGUARDING_DEFAULT",
            type: "SAFETY_ALERT",
            payload: new
            {
                alert.AlertId,
                alert.CaseId,
                alert.Severity,
                alert.CreatedAtUtc
            },
            cancellationToken);

        return alert;
    }

    private async Task<Dictionary<string, object>> ComputeMetricsAsync(
        Guid studentId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var metrics = new Dictionary<string, object>();

        // Get case age in days
        var caseEntity = await _dbContext.Cases
            .Where(c => c.CaseId == caseId)
            .FirstOrDefaultAsync(cancellationToken);

        if (caseEntity != null)
        {
            var caseAgeDays = (DateTimeOffset.UtcNow - caseEntity.CreatedAtUtc).TotalDays;
            metrics["caseAgeDays"] = Math.Floor(caseAgeDays);
        }

        // Get guardian no-reply days (count days since last message sent with no reply)
        var lastMessage = await _dbContext.Messages
            .Where(m => m.CaseId == caseId && m.Status == "SENT")
            .OrderByDescending(m => m.SentAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastMessage?.SentAtUtc != null)
        {
            var noReplyDays = (DateTimeOffset.UtcNow - lastMessage.SentAtUtc.Value).TotalDays;
            metrics["guardianNoReplyDays"] = Math.Floor(noReplyDays);
        }
        else
        {
            metrics["guardianNoReplyDays"] = 0;
        }

        // Get consecutive absence days
        var recentAbsences = await _dbContext.AttendanceMarks
            .Where(am => am.StudentId == studentId &&
                        (am.Status == "ABSENT" || am.Status == "UNKNOWN") &&
                        am.Date <= DateOnly.FromDateTime(DateTime.UtcNow))
            .OrderByDescending(am => am.Date)
            .Take(30)
            .ToListAsync(cancellationToken);

        int consecutiveDays = 0;
        var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var absence in recentAbsences.OrderByDescending(a => a.Date))
        {
            if (absence.Date == currentDate.AddDays(-consecutiveDays))
            {
                consecutiveDays++;
            }
            else
            {
                break;
            }
        }

        metrics["consecutiveAbsenceDays"] = consecutiveDays;

        // Get total absences in last 30 days
        var totalAbsences = recentAbsences.Count;
        metrics["totalAbsenceDays30"] = totalAbsences;

        return metrics;
    }
}
