using AnseoConnect.ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/consent")]
[Authorize(Policy = "StaffOnly")]
public sealed class ConsentController : ControllerBase
{
    private readonly CaseQueryService _caseQueryService;
    private readonly ILogger<ConsentController> _logger;

    public ConsentController(CaseQueryService caseQueryService, ILogger<ConsentController> logger)
    {
        _caseQueryService = caseQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets consent status for a guardian and channel.
    /// GET /api/consent/{guardianId}?channel=SMS
    /// </summary>
    [HttpGet("{guardianId}")]
    public async Task<IActionResult> GetConsentStatus(
        Guid guardianId,
        [FromQuery] string channel = "SMS",
        CancellationToken cancellationToken = default)
    {
        if (guardianId == Guid.Empty)
        {
            return BadRequest(new { error = "guardianId is required" });
        }

        if (string.IsNullOrWhiteSpace(channel))
        {
            return BadRequest(new { error = "channel is required" });
        }

        var consentStatus = await _caseQueryService.GetConsentStatusAsync(guardianId, channel, cancellationToken);

        if (consentStatus == null)
        {
            return NotFound();
        }

        return Ok(consentStatus);
    }
}
