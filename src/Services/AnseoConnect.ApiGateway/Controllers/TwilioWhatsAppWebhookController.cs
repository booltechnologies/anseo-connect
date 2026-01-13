using AnseoConnect.Contracts.Common;
using AnseoConnect.Contracts.Events;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnseoConnect.ApiGateway.Services;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("webhooks/twilio/whatsapp")]
public sealed class TwilioWhatsAppWebhookController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<TwilioWhatsAppWebhookController> _logger;
    private readonly NotificationBroadcaster _broadcaster;

    public TwilioWhatsAppWebhookController(
        AnseoConnectDbContext dbContext,
        IMessageBus messageBus,
        ILogger<TwilioWhatsAppWebhookController> logger,
        NotificationBroadcaster broadcaster)
    {
        _dbContext = dbContext;
        _messageBus = messageBus;
        _logger = logger;
        _broadcaster = broadcaster;
    }

    // Twilio sends form-urlencoded; accept any payload and map minimal fields
    [HttpPost("delivery")]
    public async Task<IActionResult> Delivery([FromForm] Dictionary<string, string> form, CancellationToken ct)
    {
        var messageSid = form.TryGetValue("MessageSid", out var sid) ? sid : null;
        var messageStatus = form.TryGetValue("MessageStatus", out var status) ? status : null;
        var to = form.TryGetValue("To", out var toVal) ? toVal : null;

        if (string.IsNullOrWhiteSpace(messageSid) || string.IsNullOrWhiteSpace(messageStatus))
        {
            return Ok();
        }

        var message = await _dbContext.Messages.FirstOrDefaultAsync(m => m.ProviderMessageId == messageSid, ct);
        if (message == null)
        {
            _logger.LogWarning("WhatsApp delivery webhook could not find message for sid {Sid}", messageSid);
            return Ok();
        }

        message.Status = messageStatus.ToUpperInvariant();
        message.DeliveredAtUtc = messageStatus.Equals("delivered", StringComparison.OrdinalIgnoreCase)
            ? DateTimeOffset.UtcNow
            : message.DeliveredAtUtc;
        await _dbContext.SaveChangesAsync(ct);

        var payload = new MessageDeliveryUpdatedV1(
            MessageId: message.MessageId,
            Provider: "TWILIO_WHATSAPP",
            Status: message.Status,
            ProviderMessageId: messageSid,
            ErrorCode: null,
            ErrorMessage: null);

        var envelope = new MessageEnvelope<MessageDeliveryUpdatedV1>(
            MessageType: MessageTypes.MessageDeliveryUpdatedV1,
            Version: MessageVersions.V1,
            TenantId: message.TenantId,
            SchoolId: message.SchoolId,
            CorrelationId: Guid.NewGuid().ToString(),
            OccurredAtUtc: DateTimeOffset.UtcNow,
            Payload: payload);

        await _messageBus.PublishAsync(envelope, ct);

        return Ok();
    }

    // Inbound replies (WhatsApp) – treated similar to SMS replies
    [HttpPost("reply")]
    public async Task<IActionResult> Reply([FromForm] Dictionary<string, string> form, CancellationToken ct)
    {
        var body = form.TryGetValue("Body", out var b) ? b : null;
        var from = form.TryGetValue("From", out var f) ? f : null;
        var messageSid = form.TryGetValue("SmsSid", out var sid) ? sid : null;

        _logger.LogInformation("WhatsApp reply received from {From}: {Body}", from, body);

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(body))
        {
            return Ok();
        }

        var guardian = await _dbContext.Guardians.AsNoTracking()
            .FirstOrDefaultAsync(g => g.MobileE164 == from.Replace("whatsapp:", ""), ct);
        if (guardian == null)
        {
            _logger.LogWarning("Guardian not found for WhatsApp reply sender {From}", from);
            return Ok();
        }

        var isOptOut = string.Equals(body, "STOP", StringComparison.OrdinalIgnoreCase);
        if (isOptOut)
        {
            var consentState = await _dbContext.ConsentStates
                .FirstOrDefaultAsync(c => c.GuardianId == guardian.GuardianId && c.Channel == "WHATSAPP", ct);
            if (consentState == null)
            {
                consentState = new ConsentState
                {
                    ConsentStateId = Guid.NewGuid(),
                    GuardianId = guardian.GuardianId,
                    Channel = "WHATSAPP",
                    State = "OPTED_OUT",
                    Source = "GUARDIAN_REPLY",
                    LastUpdatedUtc = DateTimeOffset.UtcNow,
                    UpdatedBy = "TWILIO_WHATSAPP_WEBHOOK",
                    TenantId = guardian.TenantId,
                    SchoolId = guardian.SchoolId
                };
                _dbContext.ConsentStates.Add(consentState);
            }
            else
            {
                consentState.State = "OPTED_OUT";
                consentState.Source = "GUARDIAN_REPLY";
                consentState.LastUpdatedUtc = DateTimeOffset.UtcNow;
                consentState.UpdatedBy = "TWILIO_WHATSAPP_WEBHOOK";
            }

            var record = new ConsentRecord
            {
                ConsentRecordId = Guid.NewGuid(),
                GuardianId = guardian.GuardianId,
                TenantId = guardian.TenantId,
                SchoolId = guardian.SchoolId,
                Channel = "WHATSAPP",
                Action = "OPTED_OUT",
                Source = "GUARDIAN_REPLY",
                Notes = "STOP keyword via WhatsApp",
                CapturedAtUtc = DateTimeOffset.UtcNow
            };
            _dbContext.ConsentRecords.Add(record);
            await _dbContext.SaveChangesAsync(ct);
        }

        var payload = new GuardianReplyReceivedV1(
            MessageId: Guid.NewGuid(),
            GuardianId: guardian.GuardianId,
            Channel: "WHATSAPP",
            Text: body,
            IsOptOutKeyword: isOptOut);

        var envelope = new MessageEnvelope<GuardianReplyReceivedV1>(
            MessageType: MessageTypes.GuardianReplyReceivedV1,
            Version: MessageVersions.V1,
            TenantId: guardian.TenantId,
            SchoolId: guardian.SchoolId,
            CorrelationId: Guid.NewGuid().ToString(),
            OccurredAtUtc: DateTimeOffset.UtcNow,
            Payload: payload);

        await _messageBus.PublishAsync(envelope, ct);

        // Record engagement reply
        var engagement = new EngagementEvent
        {
            EventId = Guid.NewGuid(),
            TenantId = guardian.TenantId,
            MessageId = Guid.Empty,
            GuardianId = guardian.GuardianId,
            EventType = "REPLIED",
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        _dbContext.EngagementEvents.Add(engagement);
        await _dbContext.SaveChangesAsync(ct);

        await _broadcaster.BroadcastEngagementAsync(guardian.TenantId, new
        {
            guardianId = guardian.GuardianId,
            channel = "WHATSAPP",
            eventType = "REPLIED"
        }, ct);

        return Ok();
    }
}
