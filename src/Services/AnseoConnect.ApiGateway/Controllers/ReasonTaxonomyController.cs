using AnseoConnect.Data;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/reasons")]
[Authorize(Policy = "StaffOnly")]
public sealed class ReasonTaxonomyController : ControllerBase
{
    private readonly ReasonTaxonomySyncService _syncService;
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public ReasonTaxonomyController(
        ReasonTaxonomySyncService syncService,
        AnseoConnectDbContext dbContext,
        ITenantContext tenantContext)
    {
        _syncService = syncService;
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken ct)
    {
        if (_tenantContext.TenantId == Guid.Empty)
        {
            return BadRequest(new { error = "Tenant not resolved from token." });
        }

        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantId == _tenantContext.TenantId, ct);

        if (tenant == null)
        {
            return NotFound(new { error = "Tenant not found." });
        }

        var count = await _syncService.SyncAsync(
            tenant.TenantId,
            tenant.DefaultPolicyPackId,
            tenant.DefaultPolicyPackVersion,
            tenant.CountryCode,
            ct);

        return Ok(new { synced = count });
    }
}
