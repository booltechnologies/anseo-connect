using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class NotificationClient : ApiClientBase
{
    private readonly ILogger<NotificationClient> _logger;

    public NotificationClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<NotificationClient> logger)
        : base(httpClient, options, logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var response = await GetOrDefaultAsync<IReadOnlyList<NotificationDto>>("api/notifications", cancellationToken);
        if (response != null)
        {
            return response;
        }

        _logger.LogInformation("Returning stubbed notifications data");
        return Array.Empty<NotificationDto>();
    }

    public async Task<bool> MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var ok = await PutOrFalseAsync<object>($"api/notifications/{notificationId}/read", new { }, cancellationToken);
        return ok;
    }
}
