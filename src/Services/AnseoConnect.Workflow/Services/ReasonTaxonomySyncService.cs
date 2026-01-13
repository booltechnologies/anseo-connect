using System.Text.Json;
using AnseoConnect.Data;
using AnseoConnect.PolicyRuntime;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Syncs reason taxonomy from policy pack into ReasonCodes table (cache/override).
/// </summary>
public sealed class ReasonTaxonomySyncService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ReasonTaxonomyService _loader;
    private readonly ILogger<ReasonTaxonomySyncService> _logger;

    public ReasonTaxonomySyncService(
        AnseoConnectDbContext dbContext,
        ReasonTaxonomyService loader,
        ILogger<ReasonTaxonomySyncService> logger)
    {
        _dbContext = dbContext;
        _loader = loader;
        _logger = logger;
    }

    public async Task<int> SyncAsync(Guid tenantId, string policyPackId, string policyPackVersion, string countryCode = "IE", CancellationToken cancellationToken = default)
    {
        var packPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "policy-packs",
            countryCode.ToLowerInvariant(),
            policyPackId,
            policyPackVersion,
            "reason-taxonomy.json");

        if (!File.Exists(packPath))
        {
            _logger.LogWarning("Reason taxonomy not found at {Path}", packPath);
            return 0;
        }

        var json = await File.ReadAllTextAsync(packPath, cancellationToken);
        var doc = JsonDocument.Parse(json);
        var codes = _loader.LoadCodes(doc.RootElement, countryCode);

        var existing = await _dbContext.ReasonCodes
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var upserts = 0;
        foreach (var code in codes)
        {
            var match = existing.FirstOrDefault(r =>
                r.Code == code.Code &&
                r.Scheme == code.Scheme &&
                r.Version == code.Version);

            if (match == null)
            {
                _dbContext.ReasonCodes.Add(new Data.Entities.ReasonCode
                {
                    ReasonCodeId = Guid.NewGuid(),
                    TenantId = tenantId,
                    Code = code.Code,
                    Label = code.Label,
                    Type = code.Type,
                    Scheme = code.Scheme,
                    Version = code.Version,
                    IsDefault = true,
                    Source = "POLICY_PACK",
                    IsOverride = false
                });
                upserts++;
            }
            else
            {
                match.Label = code.Label;
                match.Type = code.Type;
                match.IsDefault = true;
                match.Source = "POLICY_PACK";
                match.IsOverride = match.IsOverride && !match.IsDefault ? match.IsOverride : match.IsOverride;
                upserts++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Synced {Count} reason codes from policy pack {PolicyPackId}@{Version}", upserts, policyPackId, policyPackVersion);
        return upserts;
    }
}
