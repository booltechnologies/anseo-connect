namespace AnseoConnect.Comms.Services;

/// <summary>
/// Placeholder SignalR notifier for in-app notifications (to be wired to a hub).
/// </summary>
public sealed class SignalRNotifier : IInAppNotifier
{
    private readonly ILogger<SignalRNotifier> _logger;

    public SignalRNotifier(ILogger<SignalRNotifier> logger)
    {
        _logger = logger;
    }

    public Task NotifyAsync(Guid userId, string type, object payload, CancellationToken ct)
    {
        // TODO: wire to SignalR hub and push payload to connected clients
        _logger.LogInformation("SignalR notification placeholder for user {UserId}, type {Type}", userId, type);
        return Task.CompletedTask;
    }
}
