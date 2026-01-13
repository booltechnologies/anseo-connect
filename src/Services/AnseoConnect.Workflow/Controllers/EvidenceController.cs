using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnseoConnect.Workflow.Controllers;

[ApiController]
[Route("api/evidence")]
[Authorize(Policy = "StaffOnly")]
public sealed class EvidenceController : ControllerBase
{
    private readonly EvidencePackService _evidencePackService;

    public EvidenceController(EvidencePackService evidencePackService)
    {
        _evidencePackService = evidencePackService;
    }

    [HttpPost("cases/{caseId:guid}")]
    public async Task<IActionResult> Generate(Guid caseId, CancellationToken ct)
    {
        var pack = await _evidencePackService.GenerateAsync(caseId, ct);
        return Ok(pack);
    }
}
