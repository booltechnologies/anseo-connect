using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/interventions")]
[Authorize]
public sealed class InterventionsController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public InterventionsController(AnseoConnectDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> ListInterventions(CancellationToken cancellationToken)
    {
        var interventions = await _dbContext.MtssInterventions
            .AsNoTracking()
            .Where(i => i.TenantId == _tenantContext.TenantId && i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);

        return Ok(interventions);
    }

    [HttpPost]
    [Authorize(Policy = "TierManagement")]
    public async Task<IActionResult> CreateIntervention([FromBody] MtssIntervention intervention, CancellationToken cancellationToken)
    {
        intervention.InterventionId = Guid.NewGuid();
        intervention.TenantId = _tenantContext.TenantId;

        _dbContext.MtssInterventions.Add(intervention);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetIntervention), new { id = intervention.InterventionId }, intervention);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetIntervention(Guid id, CancellationToken cancellationToken)
    {
        var intervention = await _dbContext.MtssInterventions
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InterventionId == id && i.TenantId == _tenantContext.TenantId, cancellationToken);

        if (intervention == null) return NotFound();

        return Ok(intervention);
    }

    [HttpGet("cases/{caseId:guid}")]
    public async Task<IActionResult> ListCaseInterventions(Guid caseId, CancellationToken cancellationToken)
    {
        var interventions = await _dbContext.CaseInterventions
            .AsNoTracking()
            .Include(ci => ci.Intervention)
            .Where(ci => ci.CaseId == caseId)
            .OrderBy(ci => ci.StartedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(interventions);
    }

    [HttpPost("cases/{caseId:guid}")]
    [Authorize(Policy = "CaseManagement")]
    public async Task<IActionResult> ApplyIntervention(Guid caseId, [FromBody] ApplyInterventionRequest request, CancellationToken cancellationToken)
    {
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CaseId == caseId, cancellationToken);

        if (caseEntity == null) return NotFound();

        var intervention = await _dbContext.MtssInterventions
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InterventionId == request.InterventionId && i.TenantId == _tenantContext.TenantId, cancellationToken);

        if (intervention == null) return NotFound();

        var caseIntervention = new CaseIntervention
        {
            CaseInterventionId = Guid.NewGuid(),
            CaseId = caseId,
            InterventionId = request.InterventionId,
            TierWhenApplied = caseEntity.Tier,
            Status = "PLANNED",
            StartedAtUtc = DateTimeOffset.UtcNow,
            AssignedToUserId = request.AssignedToUserId
        };

        _dbContext.CaseInterventions.Add(caseIntervention);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCaseIntervention), new { caseId, id = caseIntervention.CaseInterventionId }, caseIntervention);
    }

    [HttpPut("cases/{caseId:guid}/{id:guid}")]
    [Authorize(Policy = "CaseManagement")]
    public async Task<IActionResult> UpdateCaseIntervention(Guid caseId, Guid id, [FromBody] UpdateInterventionRequest request, CancellationToken cancellationToken)
    {
        var caseIntervention = await _dbContext.CaseInterventions
            .FirstOrDefaultAsync(ci => ci.CaseInterventionId == id && ci.CaseId == caseId, cancellationToken);

        if (caseIntervention == null) return NotFound();

        caseIntervention.Status = request.Status;
        caseIntervention.OutcomeNotes = request.OutcomeNotes;
        if (request.Status == "COMPLETED" && !caseIntervention.CompletedAtUtc.HasValue)
        {
            caseIntervention.CompletedAtUtc = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(caseIntervention);
    }

    [HttpGet("cases/{caseId:guid}/{id:guid}")]
    public async Task<IActionResult> GetCaseIntervention(Guid caseId, Guid id, CancellationToken cancellationToken)
    {
        var caseIntervention = await _dbContext.CaseInterventions
            .AsNoTracking()
            .Include(ci => ci.Intervention)
            .FirstOrDefaultAsync(ci => ci.CaseInterventionId == id && ci.CaseId == caseId, cancellationToken);

        if (caseIntervention == null) return NotFound();

        return Ok(caseIntervention);
    }
}

public sealed record ApplyInterventionRequest(Guid InterventionId, Guid? AssignedToUserId);
public sealed record UpdateInterventionRequest(string Status, string? OutcomeNotes);
