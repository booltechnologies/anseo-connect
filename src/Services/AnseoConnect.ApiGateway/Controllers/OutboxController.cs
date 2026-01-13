using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/outbox")]
[Authorize(Policy = "StaffOnly")]
public sealed class OutboxController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;

    public OutboxController(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
    {
        var query = _dbContext.OutboxMessages.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }
        var items = await query
            .OrderBy(o => o.CreatedAtUtc)
            .Take(200)
            .ToListAsync(ct);
        return Ok(items);
    }
}

[ApiController]
[Route("api/dlq")]
[Authorize(Policy = "StaffOnly")]
public sealed class DeadLetterController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    public DeadLetterController(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await _dbContext.DeadLetterMessages.AsNoTracking()
            .OrderByDescending(d => d.FailedAtUtc)
            .Take(200)
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("{id:guid}/replay")]
    public async Task<IActionResult> Replay(Guid id, CancellationToken ct)
    {
        var dead = await _dbContext.DeadLetterMessages.FirstOrDefaultAsync(d => d.DeadLetterId == id, ct);
        if (dead == null) return NotFound();

        // TODO: wire replay to Comms outbox dispatcher via messaging boundary
        dead.ReplayedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
        return Accepted(new { message = "Replay not implemented; marked replayed timestamp only." });
    }
}
