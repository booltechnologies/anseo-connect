using AnseoConnect.Ingestion.Wonde.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnseoConnect.Ingestion.Wonde.Controllers;

[ApiController]
[Route("ingestion")]
// TODO: Add authentication for v0.1 - for now allow anonymous for testing
// [Authorize(Policy = "StaffOnly")]
public sealed class IngestionController : ControllerBase
{
    private readonly IngestionService _ingestionService;
    private readonly ILogger<IngestionController> _logger;

    public IngestionController(IngestionService ingestionService, ILogger<IngestionController> logger)
    {
        _ingestionService = ingestionService;
        _logger = logger;
    }

    /// <summary>
    /// Manual trigger endpoint for ingestion.
    /// POST /ingestion/run?schoolId={guid}&date={yyyy-MM-dd}
    /// </summary>
    [HttpPost("run")]
    public async Task<IActionResult> RunIngestion(
        [FromQuery] Guid schoolId,
        [FromQuery] DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        if (schoolId == Guid.Empty)
        {
            return BadRequest(new { error = "schoolId is required" });
        }

        var ingestionDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        try
        {
            _logger.LogInformation("Manual ingestion triggered for school {SchoolId}, date {Date}", schoolId, ingestionDate);
            var result = await _ingestionService.RunIngestionAsync(schoolId, ingestionDate, cancellationToken);

            return Ok(new
            {
                success = result.Success,
                schoolId = result.SchoolId,
                date = result.Date,
                studentCount = result.StudentCount,
                guardianCount = result.GuardianCount,
                markCount = result.MarkCount,
                duration = result.Duration.TotalMilliseconds,
                errorMessage = result.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion failed for school {SchoolId}, date {Date}", schoolId, ingestionDate);
            return StatusCode(500, new { error = "Ingestion failed", details = ex.Message });
        }
    }
}
