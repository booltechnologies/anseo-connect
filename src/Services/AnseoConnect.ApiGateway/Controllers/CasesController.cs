using AnseoConnect.ApiGateway.Models;
using AnseoConnect.ApiGateway.Services;
using AnseoConnect.Contracts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/cases")]
[Authorize(Policy = "StaffOnly")]
public sealed class CasesController : ControllerBase
{
    private readonly CaseQueryService _caseQueryService;
    private readonly ILogger<CasesController> _logger;

    public CasesController(CaseQueryService caseQueryService, ILogger<CasesController> logger)
    {
        _caseQueryService = caseQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Lists open cases with pagination.
    /// GET /api/cases?status=OPEN&skip=0&take=50
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCases(
        [FromQuery] string? status = "OPEN",
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (status != "OPEN")
        {
            return BadRequest(new { error = "Only status=OPEN is supported in v0.1" });
        }

        if (take > 100)
        {
            take = 100; // Limit to 100 per page
        }

        var (cases, totalCount) = await _caseQueryService.GetOpenCasesAsync(skip, take, cancellationToken);

        return Ok(new PagedResult<CaseDto>(cases, totalCount, skip, take, (skip + take) < totalCount));
    }

    /// <summary>
    /// Gets case details with full timeline.
    /// GET /api/cases/{caseId}
    /// </summary>
    [HttpGet("{caseId}")]
    public async Task<IActionResult> GetCase(Guid caseId, CancellationToken cancellationToken = default)
    {
        var caseDto = await _caseQueryService.GetCaseAsync(caseId, cancellationToken);

        if (caseDto == null)
        {
            return NotFound();
        }

        var checklist = new List<ChecklistItemDto>();
        return Ok(new CaseDetailDto(caseDto, checklist));
    }

    /// <summary>
    /// Mark a checklist item complete for a case (safeguarding alert or work task).
    /// PATCH /api/cases/{caseId}/checklist/{checklistId}/items/{itemId}/complete
    /// </summary>
    [HttpPatch("{caseId}/checklist/{checklistId}/items/{itemId}/complete")]
    public async Task<IActionResult> MarkChecklistItemComplete(
        Guid caseId,
        string checklistId,
        string itemId,
        [FromBody] CompleteChecklistRequest request,
        CancellationToken cancellationToken = default)
    {
        var ok = await _caseQueryService.CompleteChecklistItemAsync(caseId, checklistId, itemId, request.Notes, cancellationToken);
        if (!ok) return NotFound();
        return Ok();
    }
}

public sealed record CompleteChecklistRequest(string? Notes);
