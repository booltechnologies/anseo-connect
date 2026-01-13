using AnseoConnect.Client.Models;
using AnseoConnect.Contracts.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class TodayClient : ApiClientBase
{
    private readonly ILogger<TodayClient> _logger;

    public TodayClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<TodayClient> logger)
        : base(httpClient, options, logger)
    {
        _logger = logger;
    }

    public async Task<TodayDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var todayResponse = await GetOrDefaultAsync<TodayDashboardDto>("api/today/dashboard", cancellationToken);

        if (todayResponse != null)
        {
            return todayResponse;
        }

        _logger.LogWarning("Today dashboard unavailable, returning empty payload");
        return new TodayDashboardDto(
            DateOnly.FromDateTime(DateTime.UtcNow),
            Array.Empty<AbsenceDto>(),
            Array.Empty<TaskSummary>(),
            Array.Empty<SafeguardingAlertSummary>(),
            Array.Empty<MessageSummary>(),
            Array.Empty<GuardianContact>());
    }
}
