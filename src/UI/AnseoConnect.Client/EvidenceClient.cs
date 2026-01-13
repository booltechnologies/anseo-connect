using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class EvidenceClient : ApiClientBase
{
    public EvidenceClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<EvidenceClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public async Task<EvidencePackDto?> GenerateEvidencePackAsync(
        Guid caseId,
        DateOnly dateRangeStart,
        DateOnly dateRangeEnd,
        EvidencePackSections includeSections,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            DateRangeStart = dateRangeStart,
            DateRangeEnd = dateRangeEnd,
            IncludeSections = includeSections,
            Purpose = purpose
        };

        return await PostOrDefaultAsync<object, EvidencePackDto>($"api/cases/{caseId}/evidence", payload, cancellationToken);
    }

    public async Task<List<EvidencePackDto>> ListEvidencePacksAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<List<EvidencePackDto>>($"api/cases/{caseId}/evidence", cancellationToken);
        return result ?? new List<EvidencePackDto>();
    }

    public async Task<EvidencePackDto?> GetEvidencePackAsync(Guid caseId, Guid packId, CancellationToken cancellationToken = default)
    {
        return await GetOrDefaultAsync<EvidencePackDto>($"api/cases/{caseId}/evidence/{packId}", cancellationToken);
    }

    public async Task<bool> VerifyIntegrityAsync(Guid caseId, Guid packId, CancellationToken cancellationToken = default)
    {
        var result = await PostOrDefaultAsync<object?, IntegrityVerificationResult>($"api/cases/{caseId}/evidence/{packId}/verify", null, cancellationToken);
        return result?.IsValid ?? false;
    }

    public string GetDownloadUrl(Guid caseId, Guid packId) => $"api/cases/{caseId}/evidence/{packId}/download";
    public string GetZipUrl(Guid caseId, Guid packId) => $"api/cases/{caseId}/evidence/{packId}/zip";
}

[Flags]
public enum EvidencePackSections
{
    Attendance = 1,
    Communications = 2,
    Letters = 4,
    Meetings = 8,
    Tasks = 16,
    TierHistory = 32,
    Safeguarding = 64,
    All = Attendance | Communications | Letters | Meetings | Tasks | TierHistory
}

public sealed class EvidencePackDto
{
    public Guid EvidencePackId { get; set; }
    public Guid CaseId { get; set; }
    public Guid StudentId { get; set; }
    public DateOnly DateRangeStart { get; set; }
    public DateOnly DateRangeEnd { get; set; }
    public EvidencePackSections IncludedSections { get; set; }
    public string Format { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string ManifestHash { get; set; } = string.Empty;
    public Guid GeneratedByUserId { get; set; }
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public string GenerationPurpose { get; set; } = string.Empty;
}

public sealed class IntegrityVerificationResult
{
    public bool IsValid { get; set; }
    public Guid PackId { get; set; }
}
