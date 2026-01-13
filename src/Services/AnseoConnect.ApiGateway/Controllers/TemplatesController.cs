using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/templates")]
[Authorize(Policy = "StaffOnly")]
public sealed class TemplatesController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;

    public TemplatesController(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await _dbContext.MessageTemplates.AsNoTracking()
            .OrderBy(t => t.TemplateKey)
            .ThenByDescending(t => t.Version)
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var template = await _dbContext.MessageTemplates.FirstOrDefaultAsync(t => t.MessageTemplateId == id, ct);
        if (template == null) return NotFound();
        template.Status = "APPROVED";
        template.ApprovedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
        return Ok(template);
    }

    [HttpPost("{id:guid}/rollback")]
    public async Task<IActionResult> Rollback(Guid id, CancellationToken ct)
    {
        var template = await _dbContext.MessageTemplates.FirstOrDefaultAsync(t => t.MessageTemplateId == id, ct);
        if (template == null) return NotFound();
        template.Status = "RETIRED";
        await _dbContext.SaveChangesAsync(ct);
        return Ok(template);
    }
}
