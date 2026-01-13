namespace AnseoConnect.Comms.Services;

public interface IInAppNotifier
{
    Task NotifyAsync(Guid userId, string type, object payload, CancellationToken ct);
}
