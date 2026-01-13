using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Cryptography;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = "ReportingAccess")]
public sealed class ReportsController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ReportingService _reportingService;

    public ReportsController(AnseoConnectDbContext dbContext, ReportingService reportingService)
    {
        _dbContext = dbContext;
        _reportingService = reportingService;
    }

    [HttpGet("definitions")]
    public async Task<IActionResult> GetDefinitions(CancellationToken cancellationToken)
    {
        var defs = await _dbContext.ReportDefinitions.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(defs);
    }

    [HttpGet("runs")]
    public async Task<IActionResult> GetRuns([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var runs = await _dbContext.ReportRuns
            .AsNoTracking()
            .OrderByDescending(r => r.StartedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .Select(r => new
            {
                r.RunId,
                r.DefinitionId,
                r.StartedAtUtc,
                r.CompletedAtUtc,
                r.Status,
                r.ErrorMessage,
                ArtifactId = _dbContext.ReportArtifacts
                    .AsNoTracking()
                    .Where(a => a.RunId == r.RunId)
                    .Select(a => (Guid?)a.ArtifactId)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        return Ok(runs);
    }

    [HttpPost("definitions/{definitionId:guid}/run")]
    public async Task<IActionResult> TriggerRun(Guid definitionId, CancellationToken ct)
    {
        var definition = await _dbContext.ReportDefinitions.FirstOrDefaultAsync(d => d.DefinitionId == definitionId, ct);
        if (definition == null)
        {
            return NotFound();
        }

        var run = new ReportRun
        {
            RunId = Guid.NewGuid(),
            TenantId = definition.TenantId,
            SchoolId = Guid.Empty,
            DefinitionId = definitionId,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = "RUNNING"
        };

        _dbContext.ReportRuns.Add(run);
        await _dbContext.SaveChangesAsync(ct);

        try
        {
            var artifact = await GenerateReportArtifactAsync(run, ct);
            run.Status = "COMPLETED";
            run.CompletedAtUtc = DateTimeOffset.UtcNow;

            _dbContext.ReportArtifacts.Add(artifact);
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            run.Status = "FAILED";
            run.CompletedAtUtc = DateTimeOffset.UtcNow;
            run.ErrorMessage = ex.Message;
            await _dbContext.SaveChangesAsync(ct);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to run report." });
        }

        return Ok(run);
    }

    [HttpGet("artifacts/{artifactId:guid}/download")]
    public async Task<IActionResult> DownloadArtifact(Guid artifactId, CancellationToken ct)
    {
        var artifact = await _dbContext.ReportArtifacts.AsNoTracking().FirstOrDefaultAsync(a => a.ArtifactId == artifactId, ct);
        if (artifact == null)
        {
            return NotFound();
        }

        var pdfBytes = RenderPdf($"Report Artifact: {artifact.ArtifactId}\nGenerated: {artifact.GeneratedAtUtc:u}");
        return File(pdfBytes, "application/pdf", $"report-{artifact.ArtifactId}.pdf");
    }

    private static Task<ReportArtifact> GenerateReportArtifactAsync(ReportRun run, CancellationToken cancellationToken)
    {
        var content = $"Report Run: {run.RunId}\nGenerated: {DateTimeOffset.UtcNow:u}";
        var pdfBytes = RenderPdf(content);
        var storagePath = $"reports/{run.DefinitionId}/{run.RunId}.pdf";

        var artifact = new ReportArtifact
        {
            ArtifactId = Guid.NewGuid(),
            TenantId = run.TenantId,
            SchoolId = run.SchoolId,
            RunId = run.RunId,
            Format = "PDF",
            StoragePath = storagePath,
            DataSnapshotHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pdfBytes)),
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };

        return Task.FromResult(artifact);
    }

    private static byte[] RenderPdf(string content)
    {
        using var stream = new MemoryStream();
        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(1, QuestPDF.Infrastructure.Unit.Inch);
                page.DefaultTextStyle(QuestPDF.Infrastructure.TextStyle.Default.FontSize(11));
                page.Content().Text(content);
            });
        }).GeneratePdf(stream);
        return stream.ToArray();
    }

    [HttpGet("school-dashboard")]
    public async Task<IActionResult> GetSchoolDashboard(CancellationToken ct)
    {
        var result = await _reportingService.GetSchoolDashboardAsync(ct);
        return Ok(result);
    }

    [HttpGet("etb-dashboard/{etbTrustId:guid}")]
    [Authorize(Policy = "ETBTrustAccess")]
    public async Task<IActionResult> GetEtbDashboard(Guid etbTrustId, CancellationToken ct)
    {
        var result = await _reportingService.GetEtbDashboardAsync(etbTrustId, ct);
        return Ok(result);
    }
}
