using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/reconciliation")]
[Authorize(Policy = "StaffOnly")]
public sealed class ReconciliationController : ControllerBase
{
    private readonly AttendanceReconciliationService _service;

    public ReconciliationController(AttendanceReconciliationService service)
    {
        _service = service;
    }

    [HttpGet("{date}")]
    public async Task<IActionResult> Get(DateOnly date, CancellationToken ct)
    {
        var result = await _service.ReconcileAsync(date, ct);
        return Ok(result);
    }
}
