using System.Text.Json;
using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AnseoConnect.Data.Entities;

namespace AnseoConnect.Workflow.Controllers;

[ApiController]
[Route("api/cases")]
[Authorize(Policy = "CaseManagement")]
public sealed class TierController : ControllerBase
{
    private readonly CaseService _caseService;
    private readonly EvidencePackService _evidencePackService;
    private readonly MtssTierService _tierService;

    public TierController(
        CaseService caseService,
        EvidencePackService evidencePackService,
        MtssTierService tierService)
    {
        _caseService = caseService;
        _evidencePackService = evidencePackService;
        _tierService = tierService;
    }

    [HttpPost("{caseId:guid}/tier3")]
    public async Task<IActionResult> EscalateToTier3(Guid caseId, [FromBody] Tier3Request request, CancellationToken ct)
    {
        EvidencePack? pack = null;
        if (request.GenerateEvidencePack)
        {
            pack = await _evidencePackService.GenerateAsync(caseId, ct);
        }

        var ok = await _caseService.EscalateToTier3Async(caseId, request.Reason ?? "Tier 3 escalation", request.ChecklistId, ct);
        if (!ok) return NotFound();

        return Ok(new
        {
            Escalated = true,
            EvidencePackId = pack?.EvidencePackId
        });
    }

    [HttpGet("{caseId:guid}/tier")]
    public async Task<IActionResult> GetCurrentTier(Guid caseId, CancellationToken ct)
    {
        var tier = await _tierService.GetCurrentTierAsync(caseId, ct);
        if (tier == null) return NotFound();

        return Ok(tier);
    }

    [HttpPost("{caseId:guid}/tier")]
    public async Task<IActionResult> AssignTier(Guid caseId, [FromBody] AssignTierRequest request, CancellationToken ct)
    {
        var rationaleJson = JsonSerializer.Serialize(new { request.Reason, request.Rationale });
        var assignment = await _tierService.AssignTierAsync(
            caseId,
            request.TierNumber,
            request.Reason ?? "MANUAL",
            rationaleJson,
            null, // Would get from User.Identity
            ct);

        return Ok(assignment);
    }

    [HttpGet("{caseId:guid}/tier/history")]
    public async Task<IActionResult> GetTierHistory(Guid caseId, CancellationToken ct)
    {
        var history = await _tierService.GetHistoryAsync(caseId, ct);
        return Ok(history);
    }

    [HttpPost("{caseId:guid}/tier/evaluate")]
    public async Task<IActionResult> EvaluateTier(Guid caseId, CancellationToken ct)
    {
        var caseEntity = await _caseService.GetOrCreateAttendanceCaseAsync(
            Guid.Empty, // Would get from case
            ct);

        var evaluation = await _tierService.EvaluateTierAsync(caseEntity.StudentId, caseId, ct);
        return Ok(evaluation);
    }
}

public sealed record Tier3Request(string? Reason, string? ChecklistId, bool GenerateEvidencePack = true);
public sealed record AssignTierRequest(int TierNumber, string? Reason, string? Rationale);
