using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class TelemetryClient : ApiClientBase
{
    public TelemetryClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<TelemetryClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public Task<IReadOnlyList<AutomationMetricsDto>?> GetMetricsAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        var query = $"?from={(from?.ToString("yyyy-MM-dd") ?? string.Empty)}&to={(to?.ToString("yyyy-MM-dd") ?? string.Empty)}";
        return GetOrDefaultAsync<IReadOnlyList<AutomationMetricsDto>>($"api/telemetry/metrics{query}", cancellationToken);
    }

    public Task<RoiSummaryDto?> GetRoiAsync(Guid schoolId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        var query = $"?schoolId={schoolId}&from={(from?.ToString("yyyy-MM-dd") ?? string.Empty)}&to={(to?.ToString("yyyy-MM-dd") ?? string.Empty)}";
        return GetOrDefaultAsync<RoiSummaryDto>($"api/telemetry/roi{query}", cancellationToken);
    }
}
