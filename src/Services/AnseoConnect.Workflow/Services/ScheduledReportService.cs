using System.Security.Cryptography;
using System.Text;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Background service to generate scheduled reports and persist artifacts.
/// </summary>
public sealed class ScheduledReportService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledReportService> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _lockTimeout;

    public ScheduledReportService(IServiceScopeFactory scopeFactory, IOptions<JobScheduleOptions> options, ILogger<ScheduledReportService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = options.Value.ScheduledReportsInterval;
        _lockTimeout = options.Value.LockTimeout;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_interval);
        do
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ScheduledReportService failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnseoConnectDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ScheduledReportService>>();
        var lockService = scope.ServiceProvider.GetRequiredService<DistributedLockService>();

        await using var lockHandle = await lockService.AcquireAsync("ScheduledReportService", _lockTimeout, cancellationToken);
        if (!lockHandle.Acquired)
        {
            logger.LogInformation("ScheduledReportService lock not acquired; skipping run.");
            return;
        }

        var definitions = await dbContext.ReportDefinitions
            .AsNoTracking()
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var definition in definitions)
        {
            tenantContext.Set(definition.TenantId, null);

            var run = new ReportRun
            {
                RunId = Guid.NewGuid(),
                TenantId = definition.TenantId,
                SchoolId = Guid.Empty,
                DefinitionId = definition.DefinitionId,
                StartedAtUtc = DateTimeOffset.UtcNow,
                Status = "RUNNING"
            };

            dbContext.ReportRuns.Add(run);
            await dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                var artifact = await GenerateReportAsync(dbContext, run, cancellationToken);
                run.Status = "COMPLETED";
                run.CompletedAtUtc = DateTimeOffset.UtcNow;

                dbContext.ReportArtifacts.Add(artifact);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                run.Status = "FAILED";
                run.CompletedAtUtc = DateTimeOffset.UtcNow;
                run.ErrorMessage = ex.Message;
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogError(ex, "Failed to generate report for definition {DefinitionId}", definition.DefinitionId);
            }
        }
    }

    private static async Task<ReportArtifact> GenerateReportAsync(AnseoConnectDbContext dbContext, ReportRun run, CancellationToken cancellationToken)
    {
        // Basic placeholder content; future: fetch analytics and render charts.
        var content = $"Report Run: {run.RunId}\nGenerated: {DateTimeOffset.UtcNow:u}";
        var pdfBytes = RenderPdf(content);
        var hash = ComputeSha256(pdfBytes);
        var storagePath = $"reports/{run.DefinitionId}/{run.RunId}.pdf";

        var artifact = new ReportArtifact
        {
            ArtifactId = Guid.NewGuid(),
            TenantId = run.TenantId,
            SchoolId = run.SchoolId,
            RunId = run.RunId,
            Format = "PDF",
            StoragePath = storagePath,
            DataSnapshotHash = hash,
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };

        // In real implementation, upload pdfBytes to blob storage at storagePath
        await Task.CompletedTask;
        return artifact;
    }

    private static byte[] RenderPdf(string content)
    {
        using var stream = new MemoryStream();
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Inch);
                page.DefaultTextStyle(TextStyle.Default.FontSize(11));
                page.Content().Text(content);
            });
        }).GeneratePdf(stream);
        return stream.ToArray();
    }

    private static string ComputeSha256(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}

