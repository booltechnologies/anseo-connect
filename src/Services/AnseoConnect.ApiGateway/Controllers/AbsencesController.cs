using AnseoConnect.ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/absences")]
[Authorize(Policy = "StaffOnly")]
public sealed class AbsencesController : ControllerBase
{
    private readonly CaseQueryService _caseQueryService;
    private readonly ILogger<AbsencesController> _logger;

    public AbsencesController(CaseQueryService caseQueryService, ILogger<AbsencesController> logger)
    {
        _caseQueryService = caseQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Lists today's unexplained absences.
    /// GET /api/absences/today
    /// </summary>
    [HttpGet("today")]
    public async Task<IActionResult> GetTodayAbsences(CancellationToken cancellationToken = default)
    {
        var absences = await _caseQueryService.GetTodayUnexplainedAbsencesAsync(cancellationToken);

        return Ok(new
        {
            date = DateOnly.FromDateTime(DateTime.UtcNow),
            count = absences.Count,
            items = absences
        });
    }
}
