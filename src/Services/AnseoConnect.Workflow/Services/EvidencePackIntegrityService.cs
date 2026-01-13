using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Service for computing and verifying evidence pack integrity hashes.
/// </summary>
public sealed class EvidencePackIntegrityService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<EvidencePackIntegrityService> _logger;

    public EvidencePackIntegrityService(
        AnseoConnectDbContext dbContext,
        ILogger<EvidencePackIntegrityService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Computes SHA-256 hash of generated PDF bytes.
    /// </summary>
    public string ComputeContentHash(byte[] pdfBytes)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(pdfBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Computes SHA-256 hash of the manifest/index JSON.
    /// </summary>
    public string ComputeManifestHash(string indexJson)
    {
        var bytes = Encoding.UTF8.GetBytes(indexJson);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies an existing pack's integrity by recomputing hashes.
    /// </summary>
    public async Task<bool> VerifyIntegrityAsync(Guid evidencePackId, CancellationToken cancellationToken = default)
    {
        var pack = await _dbContext.EvidencePacks
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EvidencePackId == evidencePackId, cancellationToken);

        if (pack == null)
        {
            _logger.LogWarning("Evidence pack {PackId} not found for integrity verification", evidencePackId);
            return false;
        }

        // Note: In a real system, we would read the PDF from blob storage and recompute the hash
        // For now, we just verify that hashes are present and non-empty
        if (string.IsNullOrWhiteSpace(pack.ContentHash) || string.IsNullOrWhiteSpace(pack.ManifestHash))
        {
            _logger.LogWarning("Evidence pack {PackId} missing integrity hashes", evidencePackId);
            return false;
        }

        // Verify manifest hash matches the stored index
        var computedManifestHash = ComputeManifestHash(pack.IndexJson);
        if (computedManifestHash != pack.ManifestHash)
        {
            _logger.LogWarning("Evidence pack {PackId} manifest hash mismatch. Expected: {Expected}, Got: {Computed}",
                evidencePackId, pack.ManifestHash, computedManifestHash);
            return false;
        }

        _logger.LogInformation("Evidence pack {PackId} integrity verified", evidencePackId);
        return true;
    }

    /// <summary>
    /// Creates an audit log entry for evidence export (placeholder - would integrate with audit system).
    /// </summary>
    public async Task LogExportAsync(Guid evidencePackId, Guid userId, string purpose, CancellationToken cancellationToken = default)
    {
        // In a real system, this would write to an AuditEvent table
        // For now, we just log it
        _logger.LogInformation(
            "Evidence pack {PackId} exported by user {UserId} for purpose {Purpose}",
            evidencePackId, userId, purpose);

        // Could also add a timeline event to the case
        var pack = await _dbContext.EvidencePacks
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EvidencePackId == evidencePackId, cancellationToken);

        if (pack != null)
        {
            // This would typically go through CaseService.AddTimelineEventAsync
            // For now, we just log
            _logger.LogInformation(
                "Evidence pack export logged for case {CaseId}: Pack {PackId}, User {UserId}, Purpose {Purpose}",
                pack.CaseId, evidencePackId, userId, purpose);
        }

        await Task.CompletedTask;
    }
}
