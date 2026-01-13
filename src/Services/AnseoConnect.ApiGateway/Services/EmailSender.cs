using SendGrid;
using SendGrid.Helpers.Mail;

namespace AnseoConnect.ApiGateway.Services;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlContent, string? plainTextContent, CancellationToken ct);
}

public sealed class EmailSender : IEmailSender
{
    private readonly SendGridClient _client;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(string apiKey, string fromEmail, string fromName, ILogger<EmailSender> logger)
    {
        _client = new SendGridClient(apiKey);
        _fromEmail = fromEmail;
        _fromName = string.IsNullOrWhiteSpace(fromName) ? "Anseo Connect" : fromName;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlContent, string? plainTextContent, CancellationToken ct)
    {
        var from = new EmailAddress(_fromEmail, _fromName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent ?? htmlContent, htmlContent);
        var response = await _client.SendEmailAsync(msg, ct);
        _logger.LogInformation("SendGrid send status {Status}", response.StatusCode);
    }
}
