using System.Security.Cryptography;
using System.Text;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Generates letter artifacts (PDF) from approved templates.
/// </summary>
public sealed class LetterGenerationService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<LetterGenerationService> _logger;

    public LetterGenerationService(
        AnseoConnectDbContext dbContext,
        ILogger<LetterGenerationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<LetterArtifact> GenerateAsync(
        Guid instanceId,
        Guid stageId,
        Guid guardianId,
        string languageCode = "en",
        Dictionary<string, string>? mergeData = null,
        CancellationToken cancellationToken = default)
    {
        var instance = await _dbContext.StudentInterventionInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InstanceId == instanceId, cancellationToken)
            ?? throw new InvalidOperationException($"Intervention instance {instanceId} not found.");

        var stage = await _dbContext.InterventionStages
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StageId == stageId, cancellationToken)
            ?? throw new InvalidOperationException($"Intervention stage {stageId} not found.");

        if (stage.LetterTemplateId == null)
        {
            throw new InvalidOperationException($"Stage {stageId} does not reference a letter template.");
        }

        var template = await _dbContext.LetterTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateId == stage.LetterTemplateId, cancellationToken)
            ?? throw new InvalidOperationException($"Letter template {stage.LetterTemplateId} not found.");

        var body = ApplyMerge(template.BodyHtml, mergeData ?? new Dictionary<string, string>());

        var pdfBytes = RenderPdf(body);
        var hash = ComputeSha256(pdfBytes);
        var storagePath = $"letters/{instanceId}/{Guid.NewGuid():N}.pdf";

        var artifact = new LetterArtifact
        {
            ArtifactId = Guid.NewGuid(),
            TenantId = instance.TenantId,
            SchoolId = instance.SchoolId,
            InstanceId = instanceId,
            StageId = stageId,
            TemplateId = template.TemplateId,
            TemplateVersion = template.Version,
            GuardianId = guardianId,
            LanguageCode = languageCode,
            StoragePath = storagePath,
            ContentHash = hash,
            MergeDataJson = System.Text.Json.JsonSerializer.Serialize(mergeData ?? new Dictionary<string, string>()),
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.LetterArtifacts.Add(artifact);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated letter artifact {ArtifactId} for instance {InstanceId}", artifact.ArtifactId, instanceId);
        return artifact;
    }

    private static string ApplyMerge(string templateBody, Dictionary<string, string> data)
    {
        var result = templateBody;
        foreach (var kvp in data)
        {
            result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }
        return result;
    }

    private static byte[] RenderPdf(string body)
    {
        using var stream = new MemoryStream();
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Inch);
                page.DefaultTextStyle(TextStyle.Default.FontSize(11));

                page.Content().Column(col =>
                {
                    col.Item().Text("Letter").FontSize(16).Bold();
                    col.Item().Text(body).FontSize(11);
                });
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

