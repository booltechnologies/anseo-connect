using AnseoConnect.Contracts.Common;
using AnseoConnect.Contracts.Events;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

/// <summary>
/// Webhook endpoints for Sendmode customer replies.
/// Based on Sendmode customer replies docs: https://developers.sendmode.com/restdocs/customerreplies
/// Format: Query string parameters: mobilenumber, command, sentto
/// Must return "True" as plain text response (Sendmode requirement).
/// </summary>
[ApiController]
[Route("webhooks/sendmode")]
public sealed class SendmodeWebhookController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<SendmodeWebhookController> _logger;
    private readonly ITenantContext _tenantContext;

    public SendmodeWebhookController(
        AnseoConnectDbContext dbContext,
        IMessageBus messageBus,
        ILogger<SendmodeWebhookController> logger,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _messageBus = messageBus;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Webhook for Sendmode customer SMS replies.
    /// GET/POST /webhooks/sendmode/reply
    /// Query parameters: mobilenumber, command, sentto
    /// Must return "True" as plain text (Sendmode requirement).
    /// </summary>
    [HttpPost("reply")]
    [HttpGet("reply")]
    public async Task<IActionResult> HandleReply(
        [FromQuery] string? mobilenumber,
        [FromQuery] string? command,
        [FromQuery] string? sentto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Accept query parameters from both POST and GET (Sendmode may use either)
            if (string.IsNullOrWhiteSpace(mobilenumber) || string.IsNullOrWhiteSpace(command))
            {
                _logger.LogWarning("Sendmode reply webhook received without required parameters. mobilenumber={MobileNumber}, command={Command}", mobilenumber, command);
                // Return "True" even on error to prevent Sendmode retries
                return Content("True", "text/plain");
            }

            _logger.LogInformation("Sendmode reply webhook: mobilenumber={MobileNumber}, command={Command}, sentto={SentTo}", mobilenumber, command, sentto);

            // Normalize phone number to E.164 format
            var normalizedPhone = NormalizePhoneNumber(mobilenumber);

            // Find guardian by phone number
            var guardian = await _dbContext.Guardians
                .AsNoTracking()
                .Where(g => g.MobileE164 == normalizedPhone || g.MobileE164 == mobilenumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (guardian == null)
            {
                _logger.LogWarning("Guardian not found for phone number {Phone}", mobilenumber);
                // Return "True" to prevent Sendmode retries
                return Content("True", "text/plain");
            }

            // Find most recent message from this guardian (to link reply)
            var recentMessage = await _dbContext.Messages
                .AsNoTracking()
                .Where(m => m.GuardianId == guardian.GuardianId && m.Channel == "SMS" && m.Provider == "SENDMODE")
                .OrderByDescending(m => m.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            // Check for opt-out keywords
            var isOptOut = IsOptOutKeyword(command);
            var channel = "SMS";

            if (isOptOut)
            {
                // Set tenant context for opt-out update
                if (_tenantContext is TenantContext tc)
                {
                    tc.Set(guardian.TenantId, guardian.SchoolId);
                }

                // Update consent state
                var consentState = await _dbContext.ConsentStates
                    .Where(c => c.GuardianId == guardian.GuardianId && c.Channel == channel)
                    .FirstOrDefaultAsync(cancellationToken);

                if (consentState == null)
                {
                    consentState = new ConsentState
                    {
                        GuardianId = guardian.GuardianId,
                        Channel = channel,
                        State = "OPTED_OUT",
                        Source = "GUARDIAN_REPLY",
                        LastUpdatedUtc = DateTimeOffset.UtcNow,
                        UpdatedBy = "SENDMODE_WEBHOOK"
                    };
                    _dbContext.ConsentStates.Add(consentState);
                }
                else
                {
                    consentState.State = "OPTED_OUT";
                    consentState.Source = "GUARDIAN_REPLY";
                    consentState.LastUpdatedUtc = DateTimeOffset.UtcNow;
                    consentState.UpdatedBy = "SENDMODE_WEBHOOK";
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                // Publish opt-out event
                var optOutPayload = new GuardianOptOutRecordedV1(
                    GuardianId: guardian.GuardianId,
                    Channel: channel,
                    Source: "GUARDIAN_REPLY"
                );

                var optOutEnvelope = new MessageEnvelope<GuardianOptOutRecordedV1>(
                    MessageType: MessageTypes.GuardianOptOutRecordedV1,
                    Version: MessageVersions.V1,
                    TenantId: guardian.TenantId,
                    SchoolId: guardian.SchoolId,
                    CorrelationId: Guid.NewGuid().ToString(),
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    Payload: optOutPayload
                );

                await _messageBus.PublishAsync(optOutEnvelope, cancellationToken);
            }

            // Publish reply received event
            var replyPayload = new GuardianReplyReceivedV1(
                MessageId: recentMessage?.MessageId ?? Guid.Empty,
                GuardianId: guardian.GuardianId,
                Channel: channel,
                Text: command,
                IsOptOutKeyword: isOptOut
            );

            var replyEnvelope = new MessageEnvelope<GuardianReplyReceivedV1>(
                MessageType: MessageTypes.GuardianReplyReceivedV1,
                Version: MessageVersions.V1,
                TenantId: guardian.TenantId,
                SchoolId: guardian.SchoolId,
                CorrelationId: Guid.NewGuid().ToString(),
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Payload: replyPayload
            );

            await _messageBus.PublishAsync(replyEnvelope, cancellationToken);

            // Return "True" as plain text (Sendmode requirement)
            return Content("True", "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Sendmode reply webhook");
            // Return "True" even on error to prevent Sendmode retries (may want to log error separately)
            return Content("True", "text/plain");
        }
    }

    private static string NormalizePhoneNumber(string phone)
    {
        // Simple normalization - in production, use a proper phone number library
        return phone?.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "") ?? "";
    }

    private static bool IsOptOutKeyword(string messageBody)
    {
        if (string.IsNullOrWhiteSpace(messageBody))
        {
            return false;
        }

        var normalized = messageBody.Trim().ToUpperInvariant();
        var optOutKeywords = new[] { "STOP", "STOPALL", "UNSUBSCRIBE", "CANCEL", "END", "QUIT" };

        return optOutKeywords.Any(keyword => normalized == keyword || normalized.StartsWith(keyword + " "));
    }
}
