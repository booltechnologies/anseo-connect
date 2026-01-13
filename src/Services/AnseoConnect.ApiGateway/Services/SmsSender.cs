using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AnseoConnect.ApiGateway.Services;

public interface ISmsSender
{
    Task SendAsync(string toE164, string message, CancellationToken ct);
}

public sealed class SmsSender : ISmsSender
{
    private readonly string _from;
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(string accountSid, string authToken, string from, ILogger<SmsSender> logger)
    {
        _logger = logger;
        _from = from;
        TwilioClient.Init(accountSid, authToken);
    }

    public async Task SendAsync(string toE164, string message, CancellationToken ct)
    {
        var msg = await MessageResource.CreateAsync(
            to: new PhoneNumber(toE164),
            from: new PhoneNumber(_from),
            body: message);
        _logger.LogInformation("Twilio SMS sent Sid={Sid} Status={Status}", msg.Sid, msg.Status);
    }
}
