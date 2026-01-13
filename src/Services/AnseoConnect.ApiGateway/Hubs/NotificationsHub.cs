using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AnseoConnect.ApiGateway.Hubs;

[Authorize]
public sealed class NotificationsHub : Hub
{
    public Task JoinTenant(Guid tenantId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
    }

    public Task LeaveTenant(Guid tenantId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
    }
}
