using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;

namespace AnseoConnect.Comms.Services;

/// <summary>
/// Service for sending email messages via Twilio SendGrid API.
/// </summary>
public sealed class SendGridEmailSender
{
    private readonly SendGridClient _client;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(
        string apiKey,
        string fromEmail,
        string fromName,
        ILogger<SendGridEmailSender> logger)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentNullException(nameof(apiKey));
        if (string.IsNullOrWhiteSpace(fromEmail))
            throw new ArgumentNullException(nameof(fromEmail));

        _client = new SendGridClient(apiKey);
        _fromEmail = fromEmail;
        _fromName = fromName ?? "Anseo Connect";
        _logger = logger;
    }

    /// <summary>
    /// Sends an email message via SendGrid API.
    /// </summary>
    public async Task<SendGridEmailResult> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlContent,
        string plainTextContent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending email to {ToEmail} via SendGrid", toEmail);

            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var message = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent,
                htmlContent);

            // Send email
            var response = await _client.SendEmailAsync(message, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
            {
                // Extract message ID from response headers if available
                string? messageId = null;
                if (response.Headers.TryGetValues("X-Message-Id", out var messageIds))
                {
                    messageId = messageIds.FirstOrDefault();
                }

                _logger.LogInformation(
                    "Email sent successfully via SendGrid. MessageId: {MessageId}, Status: {Status}",
                    messageId,
                    response.StatusCode);

                return new SendGridEmailResult
                {
                    Success = true,
                    ProviderMessageId = messageId ?? Guid.NewGuid().ToString(), // Fallback to GUID if no message ID
                    Status = response.StatusCode.ToString()
                };
            }
            else
            {
                string responseBody = string.Empty;
                try
                {
                    responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read SendGrid response body");
                }

                _logger.LogError(
                    "Failed to send email via SendGrid. Status: {Status}, Response: {Response}",
                    response.StatusCode,
                    responseBody);

                return new SendGridEmailResult
                {
                    Success = false,
                    ErrorMessage = $"SendGrid API error: {response.StatusCode} - {responseBody}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} via SendGrid", toEmail);
            return new SendGridEmailResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

public sealed record SendGridEmailResult
{
    public bool Success { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}
