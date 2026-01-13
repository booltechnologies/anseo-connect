namespace AnseoConnect.Comms.Services;

public interface ISmsProvider
{
    string ProviderName { get; }
    Task<SendResult> SendAsync(string to, string body, CancellationToken ct);
}
