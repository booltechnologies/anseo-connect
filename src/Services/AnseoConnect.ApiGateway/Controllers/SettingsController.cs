using AnseoConnect.ApiGateway.Models;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Policy = "SettingsAdmin")]
public sealed class SettingsController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public SettingsController(AnseoConnectDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    [HttpGet("school")]
    public async Task<ActionResult<SchoolSettingsDto>> GetSchoolSettings(CancellationToken ct)
    {
        var school = await GetCurrentSchoolAsync(ct);
        if (school == null) return NotFound();

        var settings = await _dbContext.SchoolSettings.FirstOrDefaultAsync(s => s.SchoolId == school.SchoolId, ct);
        if (settings == null)
        {
            settings = new SchoolSettings
            {
                SchoolSettingsId = Guid.NewGuid(),
                SchoolId = school.SchoolId,
                TenantId = school.TenantId
            };
            _dbContext.SchoolSettings.Add(settings);
            await _dbContext.SaveChangesAsync(ct);
        }

        var tenant = await _dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.TenantId == school.TenantId, ct);

        return Ok(new SchoolSettingsDto
        {
            Timezone = "UTC",
            AmCutoff = settings.AMCutoffTime.ToString("HH\\:mm"),
            PmCutoff = settings.PMCutoffTime.ToString("HH\\:mm"),
            ChannelOrder = "SMS,EMAIL",
            PolicyPackVersion = tenant?.DefaultPolicyPackVersion ?? "1.3.0",
            TranslationReviewRequired = settings.TranslationReviewRequired
        });
    }

    [HttpPut("school")]
    public async Task<IActionResult> UpdateSchoolSettings([FromBody] SchoolSettingsDto dto, CancellationToken ct)
    {
        var school = await GetCurrentSchoolAsync(ct);
        if (school == null) return NotFound();

        var settings = await _dbContext.SchoolSettings.FirstOrDefaultAsync(s => s.SchoolId == school.SchoolId, ct);
        if (settings == null)
        {
            settings = new SchoolSettings
            {
                SchoolSettingsId = Guid.NewGuid(),
                SchoolId = school.SchoolId,
                TenantId = school.TenantId
            };
            _dbContext.SchoolSettings.Add(settings);
        }

        if (TimeOnly.TryParse(dto.AmCutoff, out var am)) settings.AMCutoffTime = am;
        if (TimeOnly.TryParse(dto.PmCutoff, out var pm)) settings.PMCutoffTime = pm;
        settings.TranslationReviewRequired = dto.TranslationReviewRequired;

        await _dbContext.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpGet("policy")]
    public async Task<ActionResult<PolicyPackAssignmentDto>> GetPolicyPack(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var tenant = await _dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.TenantId == tenantId, ct);
        if (tenant == null) return NotFound();

        return Ok(new PolicyPackAssignmentDto(tenant.DefaultPolicyPackId, tenant.DefaultPolicyPackVersion, null));
    }

    [HttpPut("policy")]
    public async Task<IActionResult> UpdatePolicyPack([FromBody] PolicyPackAssignmentDto dto, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.TenantId == tenantId, ct);
        if (tenant == null) return NotFound();

        tenant.DefaultPolicyPackId = dto.PackName;
        tenant.DefaultPolicyPackVersion = dto.Version;
        await _dbContext.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpGet("/api/integrations/status")]
    public async Task<ActionResult<IReadOnlyList<IntegrationStatusDto>>> GetIntegrationStatus(CancellationToken ct)
    {
        var school = await GetCurrentSchoolAsync(ct);
        if (school == null) return Ok(Array.Empty<IntegrationStatusDto>());

        var latestSync = await _dbContext.IngestionSyncLogs
            .AsNoTracking()
            .Where(l => l.SchoolId == school.SchoolId)
            .OrderByDescending(l => l.CompletedAtUtc ?? l.StartedAtUtc)
            .FirstOrDefaultAsync(ct);

        var status = new IntegrationStatusDto(
            "Wonde",
            school.SyncStatus.ToString(),
            school.SyncStatus == Data.Entities.SyncStatus.Failed ? "Check ingestion errors" : null,
            latestSync?.CompletedAtUtc ?? latestSync?.StartedAtUtc);

        return Ok(new[] { status });
    }

    private async Task<School?> GetCurrentSchoolAsync(CancellationToken ct)
    {
        if (_tenantContext.SchoolId.HasValue)
        {
            return await _dbContext.Schools.FirstOrDefaultAsync(s => s.SchoolId == _tenantContext.SchoolId.Value, ct);
        }

        if (_tenantContext.TenantId != Guid.Empty)
        {
            return await _dbContext.Schools.FirstOrDefaultAsync(s => s.TenantId == _tenantContext.TenantId, ct);
        }

        return await _dbContext.Schools.FirstOrDefaultAsync(ct);
    }
}
