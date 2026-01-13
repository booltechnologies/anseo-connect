using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace AnseoConnect.Web.Services;

public sealed class NotificationHubClient
{
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<NotificationHubClient> _logger;
    private HubConnection? _connection;

    public event Action? DeliveryUpdated;
    public event Action? EngagementUpdated;

    public NotificationHubClient(NavigationManager navigationManager, ILogger<NotificationHubClient> logger)
    {
        _navigationManager = navigationManager;
        _logger = logger;
    }

    public async Task EnsureStartedAsync()
    {
        if (_connection != null && _connection.State == HubConnectionState.Connected)
        {
            return;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(_navigationManager.ToAbsoluteUri("/hubs/notifications"))
            .WithAutomaticReconnect()
            .Build();

        _connection.On<object>("deliveryUpdated", _ => DeliveryUpdated?.Invoke());
        _connection.On<object>("engagementUpdated", _ => EngagementUpdated?.Invoke());

        await _connection.StartAsync();
        _logger.LogInformation("SignalR notifications connected");
    }
}
