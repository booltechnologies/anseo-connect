using System.Text.Json;
using AnseoConnect.Contracts.Commands;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Comms.Services;

/// <summary>
/// Background service that materializes campaign snapshots and enqueues sends.
/// </summary>
public sealed class CampaignRunner : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CampaignRunner> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30);

    public CampaignRunner(IServiceProvider services, ILogger<CampaignRunner> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CampaignRunner started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnce(stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CampaignRunner failed");
            }

            try
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) { }
        }
    }

    private async Task RunOnce(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnseoConnectDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>() as TenantContext;
        var segmentEngine = scope.ServiceProvider.GetRequiredService<SegmentQueryEngine>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();

        var now = DateTimeOffset.UtcNow;
        var campaigns = await db.Campaigns
            .Where(c => c.Status == "SCHEDULED" && (c.ScheduledAtUtc == null || c.ScheduledAtUtc <= now))
            .OrderBy(c => c.CreatedAtUtc)
            .Take(5)
            .ToListAsync(ct);

        foreach (var campaign in campaigns)
        {
            _logger.LogInformation("Processing campaign {CampaignId}", campaign.CampaignId);
            tenantContext?.Set(campaign.TenantId, null);
            campaign.Status = "SENDING";
            await db.SaveChangesAsync(ct);

            var recipients = await segmentEngine.ResolveRecipientsAsync(campaign.SegmentId, ct);
            var snapshot = new AudienceSnapshot
            {
                SnapshotId = Guid.NewGuid(),
                TenantId = campaign.TenantId,
                SegmentId = campaign.SegmentId,
                RecipientIdsJson = JsonSerializer.Serialize(recipients.Select(r => r.GuardianId).Distinct()),
                RecipientCount = recipients.Count,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.AudienceSnapshots.Add(snapshot);
            await db.SaveChangesAsync(ct);
            campaign.SnapshotId = snapshot.SnapshotId;

            var template = await db.MessageTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.MessageTemplateId == campaign.TemplateVersionId && t.Status == "APPROVED", ct);
            if (template == null)
            {
                _logger.LogWarning("Campaign {CampaignId} template not approved or missing", campaign.CampaignId);
                campaign.Status = "FAILED";
                await db.SaveChangesAsync(ct);
                continue;
            }

            var channel = template.Channel;
            var templateKey = template.TemplateKey;

            foreach (var recipient in recipients)
            {
                var command = new SendMessageRequestedV1(
                    CaseId: Guid.Empty,
                    StudentId: recipient.StudentId,
                    GuardianId: recipient.GuardianId,
                    Channel: channel,
                    MessageType: "CAMPAIGN",
                    TemplateId: templateKey,
                    TemplateData: new Dictionary<string, string>());

                var idempotencyKey = $"campaign:{campaign.CampaignId}:{recipient.GuardianId}:{recipient.StudentId}";
                await outbox.EnqueueAsync(command, "SEND_MESSAGE", idempotencyKey, campaign.TenantId, null, ct);
            }

            campaign.Status = "COMPLETED";
            campaign.SnapshotId = snapshot.SnapshotId;
            await db.SaveChangesAsync(ct);
        }
    }
}
