using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/cases/{caseId:guid}/evidence")]
[Authorize(Policy = "CaseManagement")]
public sealed class EvidenceController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly EvidencePackBuilder _packBuilder;
    private readonly EvidencePackIntegrityService _integrityService;

    public EvidenceController(
        AnseoConnectDbContext dbContext,
        EvidencePackBuilder packBuilder,
        EvidencePackIntegrityService integrityService)
    {
        _dbContext = dbContext;
        _packBuilder = packBuilder;
        _integrityService = integrityService;
    }

    [HttpPost]
    [Authorize(Policy = "EvidenceExport")]
    public async Task<IActionResult> GenerateEvidencePack(
        Guid caseId,
        [FromBody] GenerateEvidencePackRequest request,
        CancellationToken cancellationToken)
    {
        var caseEntity = await _dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CaseId == caseId, cancellationToken);

        if (caseEntity == null) return NotFound();

        var packRequest = new EvidencePackRequest(
            caseId,
            request.DateRangeStart,
            request.DateRangeEnd,
            request.IncludeSections,
            request.Purpose,
            Guid.Empty); // Would get from User.Identity

        var pack = await _packBuilder.BuildAsync(packRequest, cancellationToken);

        return CreatedAtAction(nameof(GetEvidencePack), new { caseId, id = pack.EvidencePackId }, pack);
    }

    [HttpGet]
    public async Task<IActionResult> ListEvidencePacks(Guid caseId, CancellationToken cancellationToken)
    {
        var packs = await _dbContext.EvidencePacks
            .AsNoTracking()
            .Where(p => p.CaseId == caseId)
            .OrderByDescending(p => p.GeneratedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(packs);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEvidencePack(Guid caseId, Guid id, CancellationToken cancellationToken)
    {
        var pack = await _dbContext.EvidencePacks
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EvidencePackId == id && p.CaseId == caseId, cancellationToken);

        if (pack == null) return NotFound();

        return Ok(pack);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadPdf(Guid caseId, Guid id, CancellationToken cancellationToken)
    {
        var pack = await _dbContext.EvidencePacks
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EvidencePackId == id && p.CaseId == caseId, cancellationToken);

        if (pack == null) return NotFound();

        // In a real system, read PDF from blob storage at pack.StoragePath
        // For now, return not implemented
        return StatusCode(501, "PDF download not yet implemented - would read from blob storage");
    }

    [HttpGet("{id:guid}/zip")]
    public async Task<IActionResult> DownloadZip(Guid caseId, Guid id, CancellationToken cancellationToken)
    {
        var pack = await _dbContext.EvidencePacks
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EvidencePackId == id && p.CaseId == caseId, cancellationToken);

        if (pack == null) return NotFound();

        if (string.IsNullOrEmpty(pack.ZipStoragePath))
        {
            return BadRequest("ZIP bundle not available for this evidence pack");
        }

        // In a real system, read ZIP from blob storage at pack.ZipStoragePath
        return StatusCode(501, "ZIP download not yet implemented - would read from blob storage");
    }

    [HttpPost("{id:guid}/verify")]
    public async Task<IActionResult> VerifyIntegrity(Guid caseId, Guid id, CancellationToken cancellationToken)
    {
        var pack = await _dbContext.EvidencePacks
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EvidencePackId == id && p.CaseId == caseId, cancellationToken);

        if (pack == null) return NotFound();

        var isValid = await _integrityService.VerifyIntegrityAsync(id, cancellationToken);

        return Ok(new { IsValid = isValid, PackId = id });
    }
}

public sealed record GenerateEvidencePackRequest(
    DateOnly DateRangeStart,
    DateOnly DateRangeEnd,
    EvidencePackSections IncludeSections,
    string Purpose);
