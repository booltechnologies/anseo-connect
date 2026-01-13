using AnseoConnect.Data;
using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/letters")]
[Authorize(Policy = "StaffOnly")]
public sealed class LettersController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly LetterGenerationService _letterGenerationService;

    public LettersController(
        AnseoConnectDbContext dbContext,
        LetterGenerationService letterGenerationService)
    {
        _dbContext = dbContext;
        _letterGenerationService = letterGenerationService;
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates(CancellationToken cancellationToken)
    {
        var templates = await _dbContext.LetterTemplates.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(templates);
    }

    public sealed record GenerateLetterRequest(Guid InstanceId, Guid StageId, Guid GuardianId, string? LanguageCode, Dictionary<string, string>? MergeData);

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateLetterRequest request, CancellationToken cancellationToken)
    {
        var artifact = await _letterGenerationService.GenerateAsync(
            request.InstanceId,
            request.StageId,
            request.GuardianId,
            request.LanguageCode ?? "en",
            request.MergeData,
            cancellationToken);

        return Ok(artifact);
    }

    [HttpGet("artifacts/{artifactId:guid}")]
    public async Task<IActionResult> GetArtifact(Guid artifactId, CancellationToken cancellationToken)
    {
        var artifact = await _dbContext.LetterArtifacts.AsNoTracking().FirstOrDefaultAsync(a => a.ArtifactId == artifactId, cancellationToken);
        if (artifact == null)
        {
            return NotFound();
        }

        return Ok(artifact);
    }
}

