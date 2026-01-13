using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/campaigns")]
[Authorize(Policy = "StaffOnly")]
public sealed class CampaignsController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<CampaignsController> _logger;

    public CampaignsController(AnseoConnectDbContext dbContext, ILogger<CampaignsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await _dbContext.Campaigns.AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new
            {
                c.CampaignId,
                c.Name,
                c.Status,
                c.ScheduledAtUtc,
                c.CreatedAtUtc,
                c.SegmentId,
                c.SnapshotId,
                c.TemplateVersionId
            })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCampaign request, CancellationToken ct)
    {
        var campaign = new Campaign
        {
            CampaignId = Guid.NewGuid(),
            TenantId = request.TenantId,
            Name = request.Name,
            SegmentId = request.SegmentId,
            TemplateVersionId = request.TemplateVersionId,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "SCHEDULED" : request.Status,
            ScheduledAtUtc = request.ScheduledAtUtc ?? DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Campaigns.Add(campaign);
        await _dbContext.SaveChangesAsync(ct);
        return Ok(campaign);
    }

    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> Schedule(Guid id, [FromBody] ScheduleCampaign request, CancellationToken ct)
    {
        var campaign = await _dbContext.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == id, ct);
        if (campaign == null) return NotFound();

        campaign.ScheduledAtUtc = request.ScheduledAtUtc ?? DateTimeOffset.UtcNow;
        campaign.Status = "SCHEDULED";
        await _dbContext.SaveChangesAsync(ct);
        return Ok(campaign);
    }

    public sealed record CreateCampaign(Guid TenantId, string Name, Guid SegmentId, Guid TemplateVersionId, DateTimeOffset? ScheduledAtUtc, string? Status);
    public sealed record ScheduleCampaign(DateTimeOffset? ScheduledAtUtc);
}
