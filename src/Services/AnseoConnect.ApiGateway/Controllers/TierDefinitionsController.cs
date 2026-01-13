using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/tiers/definitions")]
[Authorize]
public sealed class TierDefinitionsController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public TierDefinitionsController(AnseoConnectDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> ListDefinitions(CancellationToken cancellationToken)
    {
        var definitions = await _dbContext.MtssTierDefinitions
            .AsNoTracking()
            .Where(t => t.TenantId == _tenantContext.TenantId)
            .OrderBy(t => t.TierNumber)
            .ToListAsync(cancellationToken);

        return Ok(definitions);
    }

    [HttpPost]
    [Authorize(Policy = "TierManagement")]
    public async Task<IActionResult> CreateDefinition([FromBody] MtssTierDefinition definition, CancellationToken cancellationToken)
    {
        definition.TierDefinitionId = Guid.NewGuid();
        definition.TenantId = _tenantContext.TenantId;

        _dbContext.MtssTierDefinitions.Add(definition);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetDefinition), new { id = definition.TierDefinitionId }, definition);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDefinition(Guid id, CancellationToken cancellationToken)
    {
        var definition = await _dbContext.MtssTierDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TierDefinitionId == id && t.TenantId == _tenantContext.TenantId, cancellationToken);

        if (definition == null) return NotFound();

        return Ok(definition);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "TierManagement")]
    public async Task<IActionResult> UpdateDefinition(Guid id, [FromBody] MtssTierDefinition definition, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.MtssTierDefinitions
            .FirstOrDefaultAsync(t => t.TierDefinitionId == id && t.TenantId == _tenantContext.TenantId, cancellationToken);

        if (existing == null) return NotFound();

        existing.Name = definition.Name;
        existing.Description = definition.Description;
        existing.EntryCriteriaJson = definition.EntryCriteriaJson;
        existing.ExitCriteriaJson = definition.ExitCriteriaJson;
        existing.EscalationCriteriaJson = definition.EscalationCriteriaJson;
        existing.ReviewIntervalDays = definition.ReviewIntervalDays;
        existing.RequiredArtifactsJson = definition.RequiredArtifactsJson;
        existing.RecommendedInterventionsJson = definition.RecommendedInterventionsJson;
        existing.IsActive = definition.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(existing);
    }
}
