using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class SafeguardingClient : ApiClientBase
{
    public SafeguardingClient(HttpClient httpClient, IOptions<ApiClientOptions> options, SampleDataProvider sampleData, ILogger<SafeguardingClient> logger)
        : base(httpClient, options, sampleData, logger)
    {
    }

    public async Task<IReadOnlyList<SafeguardingAlertSummary>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<List<SafeguardingAlertSummary>>("api/safeguarding/alerts", cancellationToken);
        return result ?? SampleData.SafeguardingAlerts;
    }

    public async Task<SafeguardingAlertSummary?> AcknowledgeAsync(Guid alertId, string user = "you", CancellationToken cancellationToken = default)
    {
        var ok = await PutOrFalseAsync($"api/safeguarding/alerts/{alertId}/ack", new { acknowledgedBy = user }, cancellationToken);
        if (ok)
        {
            return SampleData.AcknowledgeAlert(alertId, user);
        }

        return SampleData.AcknowledgeAlert(alertId, user);
    }
}
