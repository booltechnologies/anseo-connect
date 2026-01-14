using AnseoConnect.Client.Models;
using AnseoConnect.Data.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class AuditClient : ApiClientBase
{
    public AuditClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<AuditClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public async Task<PagedResult<AuditEvent>> SearchAuditEventsAsync(
        Guid? schoolId = null,
        string? actorId = null,
        string? action = null,
        string? entityType = null,
        string? entityId = null,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (schoolId.HasValue) queryParams.Add($"schoolId={schoolId.Value}");
        if (!string.IsNullOrWhiteSpace(actorId)) queryParams.Add($"actorId={Uri.EscapeDataString(actorId)}");
        if (!string.IsNullOrWhiteSpace(action)) queryParams.Add($"action={Uri.EscapeDataString(action)}");
        if (!string.IsNullOrWhiteSpace(entityType)) queryParams.Add($"entityType={Uri.EscapeDataString(entityType)}");
        if (!string.IsNullOrWhiteSpace(entityId)) queryParams.Add($"entityId={Uri.EscapeDataString(entityId)}");
        if (fromUtc.HasValue) queryParams.Add($"fromUtc={Uri.EscapeDataString(fromUtc.Value.ToString("O"))}");
        if (toUtc.HasValue) queryParams.Add($"toUtc={Uri.EscapeDataString(toUtc.Value.ToString("O"))}");
        queryParams.Add($"skip={skip}");
        queryParams.Add($"take={take}");

        var endpoint = "api/admin/audit?" + string.Join("&", queryParams);

        var result = await GetOrDefaultAsync<PagedResult<AuditEvent>>(endpoint, cancellationToken);
        return result ?? new PagedResult<AuditEvent>(new List<AuditEvent>(), 0, skip, take, false);
    }
}
