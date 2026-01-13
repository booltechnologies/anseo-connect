using System.Linq;
using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class SafeguardingClient : ApiClientBase
{
    public SafeguardingClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<SafeguardingClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public async Task<IReadOnlyList<SafeguardingAlertSummary>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<List<SafeguardingAlertSummary>>("api/safeguarding/alerts", cancellationToken);
        return result ?? new List<SafeguardingAlertSummary>();
    }

    public async Task<SafeguardingAlertSummary?> AcknowledgeAsync(Guid alertId, string user = "you", CancellationToken cancellationToken = default)
    {
        var ok = await PutOrFalseAsync($"api/safeguarding/alerts/{alertId}/ack", new { acknowledgedBy = user }, cancellationToken);
        return ok ? await GetSingleAsync(alertId, cancellationToken) : null;
    }

    private async Task<SafeguardingAlertSummary?> GetSingleAsync(Guid alertId, CancellationToken cancellationToken)
    {
        var alerts = await GetOrDefaultAsync<List<SafeguardingAlertSummary>>("api/safeguarding/alerts", cancellationToken);
        return alerts?.FirstOrDefault(a => a.AlertId == alertId);
    }
}
