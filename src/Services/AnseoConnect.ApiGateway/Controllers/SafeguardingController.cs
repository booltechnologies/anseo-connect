using AnseoConnect.ApiGateway.Models;
using AnseoConnect.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/safeguarding")]
[Authorize(Policy = "SafeguardingAccess")]
public sealed class SafeguardingController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;

    public SafeguardingController(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<IReadOnlyList<SafeguardingAlertSummary>>> GetAlerts(CancellationToken ct = default)
    {
        var alerts = await (from alert in _dbContext.SafeguardingAlerts.AsNoTracking()
                            join c in _dbContext.Cases.AsNoTracking().Include(ca => ca.Student)
                                on alert.CaseId equals c.CaseId into caseJoin
                            from c in caseJoin.DefaultIfEmpty()
                            orderby alert.CreatedAtUtc descending
                            select new SafeguardingAlertSummary(
                                alert.AlertId,
                                c != null ? c.StudentId : Guid.Empty,
                                c != null && c.Student != null ? (c.Student.FirstName + " " + c.Student.LastName).Trim() : "Unknown",
                                alert.Severity,
                                alert.ChecklistId ?? "Safeguarding",
                                alert.CreatedAtUtc,
                                alert.ReviewedBy,
                                alert.AcknowledgedAtUtc))
            .Take(100)
            .ToListAsync(ct);

        return Ok(alerts);
    }

    [HttpPut("alerts/{alertId:guid}/ack")]
    public async Task<IActionResult> Acknowledge(Guid alertId, [FromBody] AcknowledgeAlertRequest request, CancellationToken ct = default)
    {
        var alert = await _dbContext.SafeguardingAlerts.FirstOrDefaultAsync(a => a.AlertId == alertId, ct);
        if (alert == null) return NotFound();

        alert.AcknowledgedAtUtc ??= DateTimeOffset.UtcNow;
        alert.ReviewedBy ??= request.AcknowledgedBy ?? "staff";

        await _dbContext.SaveChangesAsync(ct);
        return Ok(new { alert.AlertId, alert.AcknowledgedAtUtc, alert.ReviewedBy });
    }
}

public sealed record AcknowledgeAlertRequest(string? AcknowledgedBy);
