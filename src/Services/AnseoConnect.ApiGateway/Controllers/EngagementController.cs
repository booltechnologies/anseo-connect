using AnseoConnect.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/engagement")]
[Authorize(Policy = "StaffOnly")]
public sealed class EngagementController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;

    public EngagementController(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var grouped = await _dbContext.EngagementEvents.AsNoTracking()
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var unreachable = await _dbContext.GuardianReachabilities.AsNoTracking()
            .Where(r => r.TotalFailed > r.TotalDelivered)
            .Select(r => new { r.GuardianId, r.Channel, r.TotalFailed })
            .ToListAsync(ct);

        return Ok(new { grouped, unreachable });
    }
}
