using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Generates evidence packs (PDF) for cases.
/// </summary>
public sealed class EvidencePackService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<EvidencePackService> _logger;

    public EvidencePackService(AnseoConnectDbContext dbContext, ILogger<EvidencePackService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<EvidencePack> GenerateAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var caseEntity = await _dbContext.Cases
            .Include(c => c.Student)
            .Include(c => c.TimelineEvents)
            .Include(c => c.SafeguardingAlerts)
            .FirstOrDefaultAsync(c => c.CaseId == caseId, cancellationToken);

        if (caseEntity == null)
        {
            throw new InvalidOperationException($"Case {caseId} not found");
        }

        var messages = await _dbContext.Messages
            .Where(m => m.CaseId == caseId)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var pdfBytes = BuildPdf(caseEntity, messages);
        var storagePath = $"evidence/{caseId}/{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.pdf";

        var pack = new EvidencePack
        {
            CaseId = caseId,
            Format = "PDF",
            StoragePath = storagePath,
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.EvidencePacks.Add(pack);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // In a real system, upload pdfBytes to blob storage at storagePath.
        _logger.LogInformation("Generated evidence pack {PackId} for case {CaseId}", pack.EvidencePackId, caseId);

        return pack;
    }

    private static byte[] BuildPdf(Case caseEntity, List<Message> messages)
    {
        using var stream = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Inch);
                page.DefaultTextStyle(TextStyle.Default.FontSize(11));

                page.Header()
                    .Column(col =>
                    {
                        col.Item().Text($"Case Evidence Pack").FontSize(16).Bold();
                        col.Item().Text($"Case ID: {caseEntity.CaseId}");
                        col.Item().Text($"Student: {caseEntity.Student?.FirstName} {caseEntity.Student?.LastName}");
                        col.Item().Text($"Tier: {caseEntity.Tier} | Status: {caseEntity.Status}");
                        col.Item().Text($"Created: {caseEntity.CreatedAtUtc}");
                    });

                page.Content()
                    .Column(col =>
                    {
                        col.Item().Text("Timeline").FontSize(14).Bold();
                        foreach (var evt in caseEntity.TimelineEvents.OrderBy(t => t.CreatedAtUtc))
                        {
                            col.Item().Text($"{evt.CreatedAtUtc:u} - {evt.EventType} - {evt.EventData}");
                        }

                        col.Item().Text("Messages").FontSize(14).Bold();
                        foreach (var msg in messages)
                        {
                            col.Item().Text($"{msg.CreatedAtUtc:u} [{msg.Channel}] {msg.Status} - {msg.MessageType}");
                            col.Item().Text(msg.Body ?? string.Empty).FontSize(10).FontColor(Colors.Grey.Darken2);
                        }

                        if (caseEntity.SafeguardingAlerts.Any())
                        {
                            col.Item().Text("Safeguarding Alerts").FontSize(14).Bold();
                            foreach (var alert in caseEntity.SafeguardingAlerts.OrderBy(a => a.CreatedAtUtc))
                            {
                                col.Item().Text($"{alert.CreatedAtUtc:u} - {alert.Severity} - Checklist: {alert.ChecklistId}");
                            }
                        }
                    });
            });
        })
        .GeneratePdf(stream);

        return stream.ToArray();
    }

    public async Task<EvidencePack?> GetLatestAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EvidencePacks
            .AsNoTracking()
            .Where(p => p.CaseId == caseId)
            .OrderByDescending(p => p.GeneratedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
