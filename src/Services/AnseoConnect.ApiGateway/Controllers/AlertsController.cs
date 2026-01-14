using AnseoConnect.ApiGateway.Models;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/admin/alerts")]
[Authorize(Policy = "StaffOnly")]
public sealed class AlertsController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        AnseoConnectDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<AlertsController> logger)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets all alert rules for the tenant.
    /// GET /api/admin/alerts/rules
    /// </summary>
    [HttpGet("rules")]
    public async Task<ActionResult<List<AlertRule>>> GetAlertRules(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var rules = await _dbContext.AlertRules
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Category)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return Ok(rules);
    }

    /// <summary>
    /// Gets active alert instances.
    /// GET /api/admin/alerts/instances?status=Active
    /// </summary>
    [HttpGet("instances")]
    public async Task<ActionResult<PagedResult<AlertInstance>>> GetAlertInstances(
        [FromQuery] string? status = "Active",
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (take > 100)
        {
            take = 100;
        }

        var tenantId = _tenantContext.TenantId;

        var query = _dbContext.AlertInstances
            .AsNoTracking()
            .Include(a => a.AlertRule)
            .Where(a => a.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var instances = await query
            .OrderByDescending(a => a.TriggeredAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<AlertInstance>(instances, totalCount, skip, take, (skip + take) < totalCount));
    }

    /// <summary>
    /// Creates or updates an alert rule.
    /// POST /api/admin/alerts/rules
    /// </summary>
    [HttpPost("rules")]
    [Authorize(Policy = "SettingsAdmin")]
    public async Task<IActionResult> CreateAlertRule(
        [FromBody] AlertRule rule,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        rule.AlertRuleId = Guid.NewGuid();
        rule.TenantId = tenantId;
        rule.CreatedAtUtc = DateTimeOffset.UtcNow;

        _dbContext.AlertRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAlertRule), new { id = rule.AlertRuleId }, rule);
    }

    /// <summary>
    /// Gets a specific alert rule.
    /// GET /api/admin/alerts/rules/{id}
    /// </summary>
    [HttpGet("rules/{id:guid}")]
    public async Task<IActionResult> GetAlertRule(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var rule = await _dbContext.AlertRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.AlertRuleId == id, cancellationToken);

        if (rule == null)
        {
            return NotFound();
        }

        return Ok(rule);
    }

    /// <summary>
    /// Acknowledges an alert instance.
    /// PATCH /api/admin/alerts/instances/{id}/acknowledge
    /// </summary>
    [HttpPatch("instances/{id:guid}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = User.Identity?.Name ?? "system";

        var instance = await _dbContext.AlertInstances
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.AlertInstanceId == id, cancellationToken);

        if (instance == null)
        {
            return NotFound();
        }

        instance.Status = "Acknowledged";
        instance.AcknowledgedAtUtc = DateTimeOffset.UtcNow;
        instance.AcknowledgedBy = userId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(instance);
    }

    /// <summary>
    /// Resolves an alert instance.
    /// PATCH /api/admin/alerts/instances/{id}/resolve
    /// </summary>
    [HttpPatch("instances/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveAlert(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var instance = await _dbContext.AlertInstances
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.AlertInstanceId == id, cancellationToken);

        if (instance == null)
        {
            return NotFound();
        }

        instance.Status = "Resolved";
        instance.ResolvedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(instance);
    }
}
