namespace AnseoConnect.Comms.Services;

public interface IWhatsAppProvider
{
    string ProviderName { get; }
    Task<SendResult> SendAsync(string to, string body, CancellationToken ct);
}
