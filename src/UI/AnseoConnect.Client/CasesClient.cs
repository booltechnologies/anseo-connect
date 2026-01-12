using AnseoConnect.Client.Models;
using AnseoConnect.Contracts.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class CasesClient : ApiClientBase
{
    public CasesClient(HttpClient httpClient, IOptions<ApiClientOptions> options, SampleDataProvider sampleData, ILogger<CasesClient> logger)
        : base(httpClient, options, sampleData, logger)
    {
    }

    public async Task<PagedResult<CaseDto>> GetOpenCasesAsync(
        string status = "OPEN",
        string? caseType = null,
        int? tier = null,
        string? yearGroup = null,
        string? assignedRole = null,
        bool? overdue = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = $"api/cases?status={status}&skip={skip}&take={take}";
        if (!string.IsNullOrWhiteSpace(caseType)) query += $"&type={caseType}";
        if (tier.HasValue) query += $"&tier={tier}";
        if (!string.IsNullOrWhiteSpace(yearGroup)) query += $"&yearGroup={yearGroup}";
        if (!string.IsNullOrWhiteSpace(assignedRole)) query += $"&assignedRole={assignedRole}";
        if (overdue.HasValue) query += $"&overdue={overdue.Value}";

        var result = await GetOrDefaultAsync<PagedResult<CaseDto>>(query, cancellationToken);
        if (result != null)
        {
            return result;
        }

        var all = SampleData.Cases
            .Where(c => string.Equals(c.Status, status, StringComparison.OrdinalIgnoreCase))
            .Where(c => string.IsNullOrWhiteSpace(caseType) || string.Equals(c.CaseType, caseType, StringComparison.OrdinalIgnoreCase))
            .Where(c => !tier.HasValue || c.Tier == tier.Value)
            .ToList();
        var items = all.Skip(skip).Take(take).ToList();
        var totalCount = all.Count;
        return new PagedResult<CaseDto>(items, totalCount, skip, take, (skip + take) < totalCount);
    }

    public async Task<CaseDetailDto?> GetCaseAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<CaseDto>($"api/cases/{caseId}", cancellationToken);
        if (result != null)
        {
            return new CaseDetailDto(result, SampleData.Checklist);
        }

        var sample = SampleData.FindCase(caseId);
        if (sample != null)
        {
            return new CaseDetailDto(sample, SampleData.Checklist);
        }

        return null;
    }

    public async Task<bool> AddNoteAsync(Guid caseId, string note, CancellationToken cancellationToken = default)
    {
        var payload = new { note };
        return await PutOrFalseAsync($"api/cases/{caseId}/note", payload, cancellationToken);
    }

    public async Task<bool> ChangeTierAsync(Guid caseId, int tier, CancellationToken cancellationToken = default)
    {
        var payload = new { tier };
        return await PutOrFalseAsync($"api/cases/{caseId}/tier", payload, cancellationToken);
    }

    public async Task<bool> CloseCaseAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var payload = new { status = "CLOSED" };
        return await PutOrFalseAsync($"api/cases/{caseId}/close", payload, cancellationToken);
    }
}
