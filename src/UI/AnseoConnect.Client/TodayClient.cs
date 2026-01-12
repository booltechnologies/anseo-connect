using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class TodayClient : ApiClientBase
{
    private readonly ILogger<TodayClient> _logger;

    public TodayClient(HttpClient httpClient, IOptions<ApiClientOptions> options, SampleDataProvider sampleData, ILogger<TodayClient> logger)
        : base(httpClient, options, sampleData, logger)
    {
        _logger = logger;
    }

    public async Task<TodayDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var todayResponse = await GetOrDefaultAsync<TodayDashboardDto>("api/absences/today", cancellationToken);

        if (todayResponse != null)
        {
            return todayResponse with
            {
                Tasks = SampleData.Tasks,
                SafeguardingAlerts = SampleData.SafeguardingAlerts,
                Failures = SampleData.Failures,
                MissingContacts = SampleData.MissingContacts
            };
        }

        _logger.LogInformation("Returning stubbed Today dashboard data");
        return new TodayDashboardDto(
            DateOnly.FromDateTime(DateTime.UtcNow),
            SampleData.Absences,
            SampleData.Tasks,
            SampleData.SafeguardingAlerts,
            SampleData.Failures,
            SampleData.MissingContacts);
    }
}
