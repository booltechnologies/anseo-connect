using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AnseoConnect.Client.Models;

namespace AnseoConnect.Client;

public sealed class AnalyticsClient : ApiClientBase
{
    public AnalyticsClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<AnalyticsClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public Task<InterventionAnalyticsDto?> GetInterventionAnalyticsAsync(Guid schoolId, DateOnly? date = null, CancellationToken cancellationToken = default)
    {
        var suffix = date.HasValue ? $"&date={date:yyyy-MM-dd}" : string.Empty;
        return GetOrDefaultAsync<InterventionAnalyticsDto>($"api/analytics/interventions?schoolId={schoolId}{suffix}", cancellationToken);
    }

    public Task<AnalyticsTrendDto?> GetInterventionTrendAsync(Guid schoolId, int days = 30, CancellationToken cancellationToken = default)
        => GetOrDefaultAsync<AnalyticsTrendDto>($"api/analytics/interventions/trend?schoolId={schoolId}&days={days}", cancellationToken);
}

