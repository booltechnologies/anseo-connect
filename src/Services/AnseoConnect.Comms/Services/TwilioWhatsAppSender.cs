using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AnseoConnect.Comms.Services;

/// <summary>
/// Sends WhatsApp messages via Twilio.
/// </summary>
public sealed class TwilioWhatsAppSender : IWhatsAppProvider
{
    private readonly string _fromNumber;
    private readonly ILogger<TwilioWhatsAppSender> _logger;

    public string ProviderName => "TWILIO_WHATSAPP";

    public TwilioWhatsAppSender(string accountSid, string authToken, string fromNumber, ILogger<TwilioWhatsAppSender> logger)
    {
        if (string.IsNullOrWhiteSpace(accountSid)) throw new ArgumentNullException(nameof(accountSid));
        if (string.IsNullOrWhiteSpace(authToken)) throw new ArgumentNullException(nameof(authToken));
        if (string.IsNullOrWhiteSpace(fromNumber)) throw new ArgumentNullException(nameof(fromNumber));

        TwilioClient.Init(accountSid, authToken);
        _fromNumber = fromNumber.StartsWith("whatsapp:") ? fromNumber : $"whatsapp:{fromNumber}";
        _logger = logger;
    }

    public async Task<WhatsAppSendResult> SendWhatsAppAsync(string toE164, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            var to = new PhoneNumber(toE164.StartsWith("whatsapp:") ? toE164 : $"whatsapp:{toE164}");
            var message = await MessageResource.CreateAsync(
                to: to,
                from: new PhoneNumber(_fromNumber),
                body: body);

            _logger.LogInformation("Twilio WhatsApp sent. Sid: {Sid}, Status: {Status}", message.Sid, message.Status);

            return new WhatsAppSendResult
            {
                Success = true,
                ProviderMessageId = message.Sid,
                Status = message.Status?.ToString() ?? "sent"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio WhatsApp send failed");
            return new WhatsAppSendResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<SendResult> SendAsync(string to, string body, CancellationToken cancellationToken)
    {
        var result = await SendWhatsAppAsync(to, body, cancellationToken: cancellationToken);
        return new SendResult(
            result.Success,
            result.ProviderMessageId,
            result.Status,
            result.Success ? null : "WHATSAPP_ERROR",
            result.ErrorMessage);
    }
}

public sealed record WhatsAppSendResult
{
    public bool Success { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}
