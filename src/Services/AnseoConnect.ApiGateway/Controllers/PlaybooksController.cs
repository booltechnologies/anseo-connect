using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/playbooks")]
[Authorize(Policy = "StaffOnly")]
public sealed class PlaybooksController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PlaybooksController> _logger;

    public PlaybooksController(
        AnseoConnectDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<PlaybooksController> logger)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlaybooks(CancellationToken cancellationToken)
    {
        var items = await _dbContext.PlaybookDefinitions
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPlaybook(Guid id, CancellationToken cancellationToken)
    {
        var playbook = await _dbContext.PlaybookDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlaybookId == id, cancellationToken);

        if (playbook == null)
        {
            return NotFound();
        }

        var steps = await _dbContext.PlaybookSteps
            .AsNoTracking()
            .Where(s => s.PlaybookId == id)
            .OrderBy(s => s.Order)
            .ToListAsync(cancellationToken);

        return Ok(new { playbook, steps });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PlaybookDefinition request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId == Guid.Empty)
        {
            return BadRequest(new { error = "Tenant context not set." });
        }

        var playbook = new PlaybookDefinition
        {
            PlaybookId = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            SchoolId = _tenantContext.SchoolId,
            Name = request.Name,
            Description = request.Description,
            TriggerStageType = request.TriggerStageType,
            IsActive = request.IsActive,
            StopConditionsJson = string.IsNullOrWhiteSpace(request.StopConditionsJson) ? "[]" : request.StopConditionsJson,
            EscalationConditionsJson = string.IsNullOrWhiteSpace(request.EscalationConditionsJson) ? "[]" : request.EscalationConditionsJson,
            EscalationAfterDays = request.EscalationAfterDays > 0 ? request.EscalationAfterDays : 7,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.PlaybookDefinitions.Add(playbook);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetPlaybook), new { id = playbook.PlaybookId }, playbook);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PlaybookDefinition update, CancellationToken cancellationToken)
    {
        var playbook = await _dbContext.PlaybookDefinitions.FirstOrDefaultAsync(p => p.PlaybookId == id, cancellationToken);
        if (playbook == null)
        {
            return NotFound();
        }

        playbook.Name = update.Name;
        playbook.Description = update.Description;
        playbook.TriggerStageType = update.TriggerStageType;
        playbook.IsActive = update.IsActive;
        playbook.StopConditionsJson = string.IsNullOrWhiteSpace(update.StopConditionsJson) ? "[]" : update.StopConditionsJson;
        playbook.EscalationConditionsJson = string.IsNullOrWhiteSpace(update.EscalationConditionsJson) ? "[]" : update.EscalationConditionsJson;
        playbook.EscalationAfterDays = update.EscalationAfterDays;
        playbook.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(playbook);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var playbook = await _dbContext.PlaybookDefinitions.FirstOrDefaultAsync(p => p.PlaybookId == id, cancellationToken);
        if (playbook == null)
        {
            return NotFound();
        }

        playbook.IsActive = false;
        playbook.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/steps")]
    public async Task<IActionResult> AddStep(Guid id, [FromBody] PlaybookStep request, CancellationToken cancellationToken)
    {
        var playbook = await _dbContext.PlaybookDefinitions.AsNoTracking().FirstOrDefaultAsync(p => p.PlaybookId == id, cancellationToken);
        if (playbook == null)
        {
            return NotFound();
        }

        var maxOrder = await _dbContext.PlaybookSteps.Where(s => s.PlaybookId == id).Select(s => (int?)s.Order).MaxAsync(cancellationToken) ?? 0;

        var step = new PlaybookStep
        {
            StepId = Guid.NewGuid(),
            TenantId = playbook.TenantId,
            PlaybookId = id,
            Order = maxOrder + 1,
            OffsetDays = request.OffsetDays,
            Channel = request.Channel,
            TemplateKey = request.TemplateKey,
            FallbackChannel = request.FallbackChannel,
            SkipIfPreviousReplied = request.SkipIfPreviousReplied
        };

        _dbContext.PlaybookSteps.Add(step);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(step);
    }

    [HttpPut("{id:guid}/steps/{stepId:guid}")]
    public async Task<IActionResult> UpdateStep(Guid id, Guid stepId, [FromBody] PlaybookStep update, CancellationToken cancellationToken)
    {
        var step = await _dbContext.PlaybookSteps.FirstOrDefaultAsync(s => s.PlaybookId == id && s.StepId == stepId, cancellationToken);
        if (step == null)
        {
            return NotFound();
        }

        step.OffsetDays = update.OffsetDays;
        step.Channel = update.Channel;
        step.TemplateKey = update.TemplateKey;
        step.FallbackChannel = update.FallbackChannel;
        step.SkipIfPreviousReplied = update.SkipIfPreviousReplied;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(step);
    }

    [HttpDelete("{id:guid}/steps/{stepId:guid}")]
    public async Task<IActionResult> DeleteStep(Guid id, Guid stepId, CancellationToken cancellationToken)
    {
        var step = await _dbContext.PlaybookSteps.FirstOrDefaultAsync(s => s.PlaybookId == id && s.StepId == stepId, cancellationToken);
        if (step == null)
        {
            return NotFound();
        }

        _dbContext.PlaybookSteps.Remove(step);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    [HttpGet("runs")]
    public async Task<IActionResult> GetRuns([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var query = _dbContext.PlaybookRuns.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        var runs = await query
            .OrderByDescending(r => r.TriggeredAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return Ok(runs);
    }

    [HttpGet("runs/{runId:guid}")]
    public async Task<IActionResult> GetRun(Guid runId, CancellationToken cancellationToken)
    {
        var run = await _dbContext.PlaybookRuns.AsNoTracking().FirstOrDefaultAsync(r => r.RunId == runId, cancellationToken);
        if (run == null)
        {
            return NotFound();
        }

        var logs = await _dbContext.PlaybookExecutionLogs
            .AsNoTracking()
            .Where(l => l.RunId == runId)
            .OrderBy(l => l.ScheduledForUtc)
            .ToListAsync(cancellationToken);

        return Ok(new { run, logs });
    }

    [HttpPost("runs/{runId:guid}/stop")]
    public async Task<IActionResult> StopRun(Guid runId, CancellationToken cancellationToken)
    {
        var run = await _dbContext.PlaybookRuns.FirstOrDefaultAsync(r => r.RunId == runId, cancellationToken);
        if (run == null)
        {
            return NotFound();
        }

        run.Status = "STOPPED";
        run.StopReason = "MANUAL";
        run.StoppedAtUtc = DateTimeOffset.UtcNow;
        run.NextStepScheduledAtUtc = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(run);
    }
}
