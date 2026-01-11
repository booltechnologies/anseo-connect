using AnseoConnect.Contracts.Common;
using AnseoConnect.Contracts.Events;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AnseoConnect.ApiGateway.Controllers;

/// <summary>
/// Webhook endpoints for SendGrid Events Webhook.
/// Based on SendGrid Events Webhook documentation: https://docs.sendgrid.com/for-developers/tracking-events/event
/// Format: JSON array of event objects
/// Must return 200 OK to acknowledge receipt (SendGrid will retry if not 2xx).
/// </summary>
[ApiController]
[Route("webhooks/sendgrid")]
public sealed class SendGridWebhookController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<SendGridWebhookController> _logger;
    private readonly ITenantContext _tenantContext;

    public SendGridWebhookController(
        AnseoConnectDbContext dbContext,
        IMessageBus messageBus,
        ILogger<SendGridWebhookController> logger,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _messageBus = messageBus;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Webhook for SendGrid Events Webhook.
    /// POST /webhooks/sendgrid/events
    /// Receives JSON array of SendGrid event objects.
    /// Must return 200 OK to acknowledge receipt.
    /// </summary>
    [HttpPost("events")]
    public async Task<IActionResult> HandleEvents(CancellationToken cancellationToken)
    {
        try
        {
            // Read request body
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("SendGrid webhook received empty request body");
                return Ok(); // Return 200 to prevent SendGrid retries
            }

            // Parse JSON array of events
            JsonElement[]? events;
            try
            {
                var jsonDoc = JsonDocument.Parse(requestBody);
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    events = jsonDoc.RootElement.EnumerateArray().ToArray();
                }
                else
                {
                    // Single event object (wrapped in array)
                    events = new[] { jsonDoc.RootElement };
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse SendGrid webhook JSON payload");
                return Ok(); // Return 200 to prevent SendGrid retries
            }

            if (events == null || events.Length == 0)
            {
                _logger.LogWarning("SendGrid webhook received empty events array");
                return Ok();
            }

            _logger.LogInformation("SendGrid webhook received {EventCount} events", events.Length);

            // Process each event
            foreach (var eventElement in events)
            {
                try
                {
                    await ProcessEventAsync(eventElement, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing SendGrid event. Event: {EventJson}", eventElement.GetRawText());
                    // Continue processing other events - don't fail the entire webhook
                }
            }

            // Return 200 OK to acknowledge receipt (SendGrid will retry if not 2xx)
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SendGrid webhook");
            // Return 200 OK even on error to prevent SendGrid retries (may want to log error separately)
            return Ok();
        }
    }

    private async Task ProcessEventAsync(JsonElement eventElement, CancellationToken cancellationToken)
    {
        // Extract SendGrid event fields
        var sgMessageId = eventElement.TryGetProperty("sg_message_id", out var sgMsgId) ? sgMsgId.GetString() : null;
        var eventType = eventElement.TryGetProperty("event", out var evt) ? evt.GetString() : null;
        var email = eventElement.TryGetProperty("email", out var eml) ? eml.GetString() : null;
        long? timestamp = eventElement.TryGetProperty("timestamp", out var ts) ? ts.GetInt64() : (long?)null;
        var reason = eventElement.TryGetProperty("reason", out var rsn) ? rsn.GetString() : null;
        var status = eventElement.TryGetProperty("status", out var stat) ? stat.GetString() : null;

        if (string.IsNullOrWhiteSpace(sgMessageId) || string.IsNullOrWhiteSpace(eventType))
        {
            _logger.LogWarning("SendGrid event missing required fields. sg_message_id: {SgMessageId}, event: {EventType}", sgMessageId, eventType);
            return;
        }

        _logger.LogInformation(
            "Processing SendGrid event. sg_message_id: {SgMessageId}, event: {EventType}, email: {Email}",
            sgMessageId,
            eventType,
            email);

        // Note: SendGrid's sg_message_id format is different from X-Message-Id header.
        // We need to match by email + provider, or find another way to correlate.
        // For now, we'll try to find by email and provider=SENDGRID, then match recent messages.
        // In production, you might want to include sg_message_id in custom_args when sending.

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("SendGrid event missing email address. Cannot match to message. sg_message_id: {SgMessageId}", sgMessageId);
            return;
        }

        // Find message by matching email and provider
        // Since we don't store sg_message_id, we'll find the most recent SENDGRID message for this email
        var guardian = await _dbContext.Guardians
            .AsNoTracking()
            .Where(g => g.Email == email)
            .FirstOrDefaultAsync(cancellationToken);

        if (guardian == null)
        {
            _logger.LogWarning("Guardian not found for email {Email} in SendGrid event", email);
            return;
        }

        // Find most recent message for this guardian with SENDGRID provider
        var message = await _dbContext.Messages
            .Where(m => m.GuardianId == guardian.GuardianId && m.Provider == "SENDGRID" && m.Channel == "EMAIL")
            .OrderByDescending(m => m.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (message == null)
        {
            _logger.LogWarning("Message not found for guardian {GuardianId}, email {Email} in SendGrid event", guardian.GuardianId, email);
            return;
        }

        // Set tenant context
        if (_tenantContext is TenantContext tc)
        {
            tc.Set(guardian.TenantId, guardian.SchoolId);
        }

        // Map SendGrid event type to our status
        var mappedStatus = MapSendGridEventToStatus(eventType);
        var errorCode = eventType == "bounce" || eventType == "dropped" ? "SENDGRID_" + eventType.ToUpperInvariant() : null;
        var errorMessage = reason ?? status;

        // Update message status if it's a delivery status event (not tracking events like opened/clicked)
        if (mappedStatus != null && IsDeliveryStatusEvent(eventType))
        {
            message.Status = mappedStatus;
            if (mappedStatus == "DELIVERED" && timestamp.HasValue)
            {
                message.DeliveredAtUtc = DateTimeOffset.FromUnixTimeSeconds(timestamp.Value);
            }
            if (errorCode != null)
            {
                message.ErrorCode = errorCode;
                message.ErrorMessage = errorMessage;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated message {MessageId} status to {Status} from SendGrid event {EventType}",
                message.MessageId,
                mappedStatus,
                eventType);
        }

        // Publish delivery event (only for delivery status events)
        if (mappedStatus != null && IsDeliveryStatusEvent(eventType))
        {
            var payload = new MessageDeliveryUpdatedV1(
                MessageId: message.MessageId,
                Provider: "SENDGRID",
                Status: mappedStatus,
                ProviderMessageId: sgMessageId,
                ErrorCode: errorCode,
                ErrorMessage: errorMessage
            );

            var envelope = new MessageEnvelope<MessageDeliveryUpdatedV1>(
                MessageType: MessageTypes.MessageDeliveryUpdatedV1,
                Version: MessageVersions.V1,
                TenantId: guardian.TenantId,
                SchoolId: guardian.SchoolId,
                CorrelationId: Guid.NewGuid().ToString(),
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Payload: payload
            );

            await _messageBus.PublishAsync(envelope, cancellationToken);

            _logger.LogInformation(
                "Published MessageDeliveryUpdatedV1 event for message {MessageId}, status {Status}",
                message.MessageId,
                mappedStatus);
        }
    }

    private static string? MapSendGridEventToStatus(string? eventType)
    {
        return eventType?.ToLowerInvariant() switch
        {
            "delivered" => "DELIVERED",
            "bounce" => "FAILED",
            "dropped" => "FAILED",
            "deferred" => "DEFERRED",
            "processed" => "SENT",
            _ => null // opened, clicked, spamreport, unsubscribe, group_unsubscribe, group_resubscribe, etc. are tracking events, not delivery status
        };
    }

    private static bool IsDeliveryStatusEvent(string? eventType)
    {
        // Only process delivery status events, not tracking events
        return eventType?.ToLowerInvariant() switch
        {
            "delivered" => true,
            "bounce" => true,
            "dropped" => true,
            "deferred" => true,
            "processed" => true,
            _ => false // opened, clicked, spamreport, etc. are tracking events
        };
    }
}
