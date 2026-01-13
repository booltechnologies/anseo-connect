using AnseoConnect.ApiGateway.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AnseoConnect.ApiGateway.Services;

public sealed class NotificationBroadcaster
{
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly ILogger<NotificationBroadcaster> _logger;

    public NotificationBroadcaster(IHubContext<NotificationsHub> hubContext, ILogger<NotificationBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task BroadcastDeliveryAsync(Guid tenantId, object payload, CancellationToken ct)
    {
        _logger.LogInformation("Broadcasting delivery update for tenant {TenantId}", tenantId);
        return _hubContext.Clients.Group($"tenant:{tenantId}").SendAsync("deliveryUpdated", payload, ct);
    }

    public Task BroadcastEngagementAsync(Guid tenantId, object payload, CancellationToken ct)
    {
        _logger.LogInformation("Broadcasting engagement update for tenant {TenantId}", tenantId);
        return _hubContext.Clients.Group($"tenant:{tenantId}").SendAsync("engagementUpdated", payload, ct);
    }
}
