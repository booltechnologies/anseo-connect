using AnseoConnect.Client.Models;
using AnseoConnect.Contracts.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class TimelineClient : ApiClientBase
{
    public TimelineClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<TimelineClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public async Task<PagedResult<TimelineEventDto>> GetStudentTimelineAsync(
        Guid studentId,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        string[]? categories = null,
        string[]? eventTypes = null,
        Guid? caseId = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/students/{studentId}/timeline?skip={skip}&take={take}";
        if (fromUtc.HasValue) endpoint += $"&fromUtc={Uri.EscapeDataString(fromUtc.Value.ToString("O"))}";
        if (toUtc.HasValue) endpoint += $"&toUtc={Uri.EscapeDataString(toUtc.Value.ToString("O"))}";
        if (caseId.HasValue) endpoint += $"&caseId={caseId.Value}";
        if (categories != null && categories.Length > 0)
        {
            foreach (var cat in categories)
            {
                endpoint += $"&categories={Uri.EscapeDataString(cat)}";
            }
        }
        if (eventTypes != null && eventTypes.Length > 0)
        {
            foreach (var et in eventTypes)
            {
                endpoint += $"&eventTypes={Uri.EscapeDataString(et)}";
            }
        }

        var result = await GetOrDefaultAsync<PagedResult<TimelineEventDto>>(endpoint, cancellationToken);
        return result ?? new PagedResult<TimelineEventDto>(Array.Empty<TimelineEventDto>(), 0, skip, take, false);
    }

    public async Task<PagedResult<TimelineEventDto>> SearchTimelineAsync(
        Guid studentId,
        string searchTerm,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/students/{studentId}/timeline/search?q={Uri.EscapeDataString(searchTerm)}&skip={skip}&take={take}";

        var result = await GetOrDefaultAsync<PagedResult<TimelineEventDto>>(endpoint, cancellationToken);
        return result ?? new PagedResult<TimelineEventDto>(Array.Empty<TimelineEventDto>(), 0, skip, take, false);
    }

    public string GetExportUrl(
        Guid studentId,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        string[]? categories = null,
        bool includeRedacted = false,
        string format = "PDF")
    {
        var endpoint = $"api/students/{studentId}/timeline/export?format={format}";
        if (fromUtc.HasValue) endpoint += $"&fromUtc={Uri.EscapeDataString(fromUtc.Value.ToString("O"))}";
        if (toUtc.HasValue) endpoint += $"&toUtc={Uri.EscapeDataString(toUtc.Value.ToString("O"))}";
        if (includeRedacted) endpoint += "&includeRedacted=true";
        if (categories != null && categories.Length > 0)
        {
            foreach (var cat in categories)
            {
                endpoint += $"&categories={Uri.EscapeDataString(cat)}";
            }
        }
        return endpoint;
    }
}
