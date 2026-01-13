using AnseoConnect.Data;
using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/telemetry")]
[Authorize(Policy = "StaffOnly")]
public sealed class TelemetryController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly RoiCalculatorService _roiCalculator;
    private readonly ILogger<TelemetryController> _logger;

    public TelemetryController(
        AnseoConnectDbContext dbContext,
        RoiCalculatorService roiCalculator,
        ILogger<TelemetryController> logger)
    {
        _dbContext = dbContext;
        _roiCalculator = roiCalculator;
        _logger = logger;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var start = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var metrics = await _dbContext.AutomationMetrics
            .AsNoTracking()
            .Where(m => m.Date >= start && m.Date <= end)
            .OrderByDescending(m => m.Date)
            .ToListAsync(cancellationToken);

        return Ok(metrics);
    }

    [HttpGet("roi")]
    public async Task<IActionResult> GetRoi([FromQuery] Guid schoolId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        if (schoolId == Guid.Empty)
        {
            return BadRequest(new { error = "schoolId is required" });
        }

        var start = from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var summary = await _roiCalculator.CalculateAsync(schoolId, start, end, cancellationToken);
        return Ok(summary);
    }
}
