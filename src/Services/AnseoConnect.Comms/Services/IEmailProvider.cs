namespace AnseoConnect.Comms.Services;

public interface IEmailProvider
{
    string ProviderName { get; }
    Task<SendResult> SendAsync(string to, string subject, string htmlBody, string plainTextBody, CancellationToken ct);
}
