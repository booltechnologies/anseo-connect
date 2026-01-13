using AnseoConnect.Client.Models;
using AnseoConnect.Contracts.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class CasesClient : ApiClientBase
{
    public CasesClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<CasesClient> logger)
        : base(httpClient, options, logger)
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
        return result ?? new PagedResult<CaseDto>(Array.Empty<CaseDto>(), 0, skip, take, false);
    }

    public async Task<CaseDetailDto?> GetCaseAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<CaseDetailDto>($"api/cases/{caseId}", cancellationToken);
        return result;
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
