using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Policy = "StaffOnly")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly InterventionAnalyticsService _analyticsService;
    private readonly ITenantContext _tenantContext;

    public AnalyticsController(InterventionAnalyticsService analyticsService, ITenantContext tenantContext)
    {
        _analyticsService = analyticsService;
        _tenantContext = tenantContext;
    }

    [HttpGet("interventions")]
    public async Task<IActionResult> GetInterventionAnalytics([FromQuery] Guid schoolId, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var resolvedSchoolId = schoolId != Guid.Empty
            ? schoolId
            : (_tenantContext.SchoolId ?? Guid.Empty);

        if (resolvedSchoolId == Guid.Empty)
        {
            return BadRequest(new { error = "School context not set." });
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var snapshot = await _analyticsService.BuildSnapshotAsync(resolvedSchoolId, targetDate, cancellationToken);
        return Ok(snapshot);
    }

    [HttpGet("interventions/trend")]
    public async Task<IActionResult> GetInterventionTrend([FromQuery] Guid schoolId, [FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        var resolvedSchoolId = schoolId != Guid.Empty
            ? schoolId
            : (_tenantContext.SchoolId ?? Guid.Empty);

        if (resolvedSchoolId == Guid.Empty)
        {
            return BadRequest(new { error = "School context not set." });
        }

        var range = Math.Clamp(days, 1, 90);
        var points = await _analyticsService.BuildTrendAsync(resolvedSchoolId, range, cancellationToken);
        return Ok(new AnalyticsTrendResponse(points));
    }

    public sealed record AnalyticsTrendResponse(IReadOnlyList<DailyAnalyticsPoint> Points);
}

