using AnseoConnect.Contracts.Commands;
using AnseoConnect.Contracts.Common;
using AnseoConnect.Contracts.Events;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.PolicyRuntime;
using AnseoConnect.Shared;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using System.IO;
using System.Text;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace AnseoConnect.Comms.Services;

/// <summary>
/// Service that processes message requests, evaluates consent, and sends messages.
/// </summary>
public sealed class MessageService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly SendmodeSender? _sendmodeSender;
    private readonly SendGridEmailSender? _sendGridEmailSender;
    private readonly TwilioWhatsAppSender? _twilioWhatsAppSender;
    private readonly IConsentEvaluator _consentEvaluator;
    private readonly TemplateEngine _templateEngine;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<MessageService> _logger;
    private readonly ITenantContext _tenantContext;
    private readonly IOutboxDispatcher _outboxDispatcher;
    private readonly ITranslationService _translationService;
    private static readonly AsyncRetryPolicy RetryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    public MessageService(
        AnseoConnectDbContext dbContext,
        SendmodeSender? sendmodeSender,
        SendGridEmailSender? sendGridEmailSender,
        TwilioWhatsAppSender? twilioWhatsAppSender,
        IConsentEvaluator consentEvaluator,
        TemplateEngine templateEngine,
        IMessageBus messageBus,
        ILogger<MessageService> logger,
        ITenantContext tenantContext,
        IOutboxDispatcher outboxDispatcher,
        ITranslationService translationService)
    {
        _dbContext = dbContext;
        _sendmodeSender = sendmodeSender;
        _sendGridEmailSender = sendGridEmailSender;
        _twilioWhatsAppSender = twilioWhatsAppSender;
        _consentEvaluator = consentEvaluator;
        _templateEngine = templateEngine;
        _messageBus = messageBus;
        _logger = logger;
        _tenantContext = tenantContext;
        _outboxDispatcher = outboxDispatcher;
        _translationService = translationService;
    }

    /// <summary>
    /// Processes a SendMessageRequestedV1 command.
    /// </summary>
    public async Task ProcessMessageRequestAsync(SendMessageRequestedV1 command, CancellationToken cancellationToken = default)
    {
        // Enqueue to outbox for reliable delivery
        var tenantId = _tenantContext.TenantId;
        var schoolId = _tenantContext.SchoolId;
        var idempotencyKey = BuildIdempotencyKey(command);

        await _outboxDispatcher.EnqueueAsync(command, "SEND_MESSAGE", idempotencyKey, tenantId, schoolId, cancellationToken);
    }

    /// <summary>
    /// Executes the actual send (called by OutboxDispatcherService).
    /// </summary>
    public async Task SendViaProvidersAsync(SendMessageRequestedV1 command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing message request for case {CaseId}, student {StudentId}, guardian {GuardianId}, channel {Channel}",
            command.CaseId,
            command.StudentId,
            command.GuardianId,
            command.Channel);

        // Check school sync status - block if Failed
        if (_tenantContext.SchoolId.HasValue)
        {
            var school = await _dbContext.Schools
                .AsNoTracking()
                .Where(s => s.SchoolId == _tenantContext.SchoolId.Value)
                .Select(s => new { s.SyncStatus, s.SyncErrorCount })
                .FirstOrDefaultAsync(cancellationToken);

            if (school != null && school.SyncStatus == SyncStatus.Failed)
            {
                _logger.LogWarning("Blocking message due to sync status FAILED for school {SchoolId}", _tenantContext.SchoolId);
                await CreateBlockedMessageAsync(command, "SYNC_FAILED", cancellationToken);
                return;
            }
        }

        // Get guardian to check consent
        var guardian = await _dbContext.Guardians
            .Where(g => g.GuardianId == command.GuardianId)
            .FirstOrDefaultAsync(cancellationToken);

        if (guardian == null)
        {
            _logger.LogWarning("Guardian {GuardianId} not found", command.GuardianId);
            await CreateBlockedMessageAsync(command, "GUARDIAN_NOT_FOUND", cancellationToken);
            return;
        }

        // Load contact preferences
        var contactPreference = await _dbContext.ContactPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GuardianId == command.GuardianId, cancellationToken);

        if (IsWithinQuietHours(contactPreference))
        {
            _logger.LogInformation("Message blocked due to quiet hours for guardian {GuardianId}", command.GuardianId);
            await CreateBlockedMessageAsync(command, "QUIET_HOURS", cancellationToken);
            return;
        }

        var translationReviewRequired = await IsTranslationReviewRequiredAsync(cancellationToken);
        if (translationReviewRequired &&
            contactPreference != null &&
            !string.IsNullOrWhiteSpace(contactPreference.PreferredLanguage) &&
            !string.Equals(contactPreference.PreferredLanguage, "en", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Message blocked for translation review requirement for guardian {GuardianId}", command.GuardianId);
            await CreateBlockedMessageAsync(command, "TRANSLATION_REVIEW_REQUIRED", cancellationToken);
            return;
        }

        // Get consent state
        var consentState = await _dbContext.ConsentStates
            .Where(c => c.GuardianId == command.GuardianId && c.Channel == command.Channel)
            .FirstOrDefaultAsync(cancellationToken);

        var currentConsentState = consentState?.State ?? "UNKNOWN";

        // Load policy pack and evaluate consent
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.TenantId == _tenantContext.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant {TenantId} not found", _tenantContext.TenantId);
            await CreateBlockedMessageAsync(command, "TENANT_NOT_FOUND", cancellationToken);
            return;
        }

        // Load policy pack JSON
        var policyPackPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "policy-packs",
            tenant.CountryCode,
            tenant.DefaultPolicyPackId,
            tenant.DefaultPolicyPackVersion,
            "consent.json");

        if (!File.Exists(policyPackPath))
        {
            _logger.LogWarning("Policy pack not found at {Path}", policyPackPath);
            // Default: allow SMS if not opted out
            var allowed = command.Channel == "SMS" && currentConsentState != "OPTED_OUT";
            if (!allowed)
            {
                await CreateBlockedMessageAsync(command, "CONSENT_BLOCKED", cancellationToken);
                return;
            }
        }
        else
        {
            var policyPackJson = await File.ReadAllTextAsync(policyPackPath, cancellationToken);
            var policyPackDoc = JsonDocument.Parse(policyPackJson);
            var allowed = _consentEvaluator.EvaluateConsent(
                policyPackDoc.RootElement,
                command.Channel,
                currentConsentState);

            if (!allowed)
            {
                _logger.LogInformation(
                    "Message blocked due to consent. Guardian {GuardianId}, Channel {Channel}, State {State}",
                    command.GuardianId,
                    command.Channel,
                    currentConsentState);
                await CreateBlockedMessageAsync(command, "CONSENT_BLOCKED", cancellationToken);
                return;
            }
        }

        // Route to channel-specific sender with fallback
        var channels = GetChannelPriority(command, GetPreferredChannels(contactPreference));
        foreach (var channel in channels)
        {
            var attempted = await TrySendWithRetry(async () =>
            {
                if (channel == "SMS")
                {
                    return await SendSmsAsync(command with { Channel = "SMS" }, guardian, contactPreference, cancellationToken);
                }
                if (channel == "EMAIL")
                {
                    return await SendEmailAsync(command with { Channel = "EMAIL" }, guardian, contactPreference, cancellationToken);
                }
                if (channel == "WHATSAPP")
                {
                    return await SendWhatsAppAsync(command with { Channel = "WHATSAPP" }, guardian, contactPreference, cancellationToken);
                }
                return false;
            }, channel);

            if (attempted)
            {
                return;
            }
        }

        _logger.LogWarning("All channels failed for command {CaseId}/{GuardianId}", command.CaseId, command.GuardianId);
        await CreateBlockedMessageAsync(command, "ALL_CHANNELS_FAILED", cancellationToken);
    }

    private async Task<bool> SendSmsAsync(SendMessageRequestedV1 command, Guardian guardian, ContactPreference? preference, CancellationToken cancellationToken)
    {
        if (_sendmodeSender == null)
        {
            _logger.LogWarning("SendmodeSender not configured. Cannot send SMS.");
            await CreateBlockedMessageAsync(command, "SMS_PROVIDER_NOT_CONFIGURED", cancellationToken);
            return false;
        }

        if (string.IsNullOrWhiteSpace(guardian.MobileE164))
        {
            _logger.LogWarning("Guardian {GuardianId} does not have mobile number", command.GuardianId);
            await CreateBlockedMessageAsync(command, "NO_PHONE_NUMBER", cancellationToken);
            return false;
        }

        // Generate message body from template
        var messageBody = await RenderBodyAsync(command, cancellationToken);
        var localizedTexts = await BuildLocalizedTextsAsync(messageBody, preference, cancellationToken);

        // Send via Sendmode
        var sendResult = await _sendmodeSender.SendSmsAsync(guardian.MobileE164, messageBody, cancellationToken);

        // Create message record
        var message = new Message
        {
            CaseId = command.CaseId,
            StudentId = command.StudentId,
            GuardianId = command.GuardianId,
            ThreadId = null,
            Direction = "OUTBOUND",
            Channel = command.Channel,
            MessageType = command.MessageType,
            Status = sendResult.Success ? "SENT" : "FAILED",
            ProviderMessageId = sendResult.ProviderMessageId,
            Provider = "SENDMODE",
            Body = messageBody,
            TemplateId = command.TemplateId,
            IdempotencyKey = BuildIdempotencyKey(command),
            ErrorCode = sendResult.Success ? null : "SENDMODE_ERROR",
            ErrorMessage = sendResult.ErrorMessage,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            SentAtUtc = sendResult.Success ? DateTimeOffset.UtcNow : null
        };
        foreach (var lt in localizedTexts)
        {
            lt.MessageId = message.MessageId;
            message.LocalizedTexts.Add(lt);
        }

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish delivery event
        if (sendResult.Success)
        {
            await PublishDeliveryEventAsync(message, sendResult.Status ?? "sent", cancellationToken);
        }

        _logger.LogInformation(
            "SMS message processed. MessageId: {MessageId}, Status: {Status}",
            message.MessageId,
            message.Status);

        return sendResult.Success;
    }

    private async Task<bool> SendEmailAsync(SendMessageRequestedV1 command, Guardian guardian, ContactPreference? preference, CancellationToken cancellationToken)
    {
        if (_sendGridEmailSender == null)
        {
            _logger.LogWarning("SendGridEmailSender not configured. Cannot send email.");
            await CreateBlockedMessageAsync(command, "EMAIL_PROVIDER_NOT_CONFIGURED", cancellationToken);
            return false;
        }

        if (string.IsNullOrWhiteSpace(guardian.Email))
        {
            _logger.LogWarning("Guardian {GuardianId} does not have email address", command.GuardianId);
            await CreateBlockedMessageAsync(command, "NO_EMAIL_ADDRESS", cancellationToken);
            return false;
        }

        // Generate email content (subject, HTML, plain text) from template
        var (subject, htmlBody, plainTextBody) = await RenderEmailAsync(command, cancellationToken);
        var localizedTexts = await BuildLocalizedTextsAsync(htmlBody, preference, cancellationToken);

        // Send via SendGrid
        var sendResult = await _sendGridEmailSender.SendEmailAsync(
            guardian.Email,
            subject,
            htmlBody,
            plainTextBody,
            cancellationToken);

        // Create message record
        var message = new Message
        {
            CaseId = command.CaseId,
            StudentId = command.StudentId,
            GuardianId = command.GuardianId,
            ThreadId = null,
            Direction = "OUTBOUND",
            Channel = command.Channel,
            MessageType = command.MessageType,
            Status = sendResult.Success ? "SENT" : "FAILED",
            ProviderMessageId = sendResult.ProviderMessageId,
            Provider = "SENDGRID",
            Body = htmlBody, // Store HTML body in Body field
            TemplateId = command.TemplateId,
            IdempotencyKey = BuildIdempotencyKey(command),
            ErrorCode = sendResult.Success ? null : "SENDGRID_ERROR",
            ErrorMessage = sendResult.ErrorMessage,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            SentAtUtc = sendResult.Success ? DateTimeOffset.UtcNow : null
        };
        foreach (var lt in localizedTexts)
        {
            lt.MessageId = message.MessageId;
            message.LocalizedTexts.Add(lt);
        }

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Publish delivery event
        if (sendResult.Success)
        {
            await PublishDeliveryEventAsync(message, sendResult.Status ?? "sent", cancellationToken);
        }

        _logger.LogInformation(
            "Email message processed. MessageId: {MessageId}, Status: {Status}",
            message.MessageId,
            message.Status);

        return sendResult.Success;
    }

    private async Task<bool> SendWhatsAppAsync(SendMessageRequestedV1 command, Guardian guardian, ContactPreference? preference, CancellationToken cancellationToken)
    {
        if (_twilioWhatsAppSender == null)
        {
            _logger.LogWarning("TwilioWhatsAppSender not configured. Cannot send WhatsApp.");
            await CreateBlockedMessageAsync(command, "WHATSAPP_PROVIDER_NOT_CONFIGURED", cancellationToken);
            return false;
        }

        if (string.IsNullOrWhiteSpace(guardian.MobileE164))
        {
            _logger.LogWarning("Guardian {GuardianId} does not have mobile number for WhatsApp", command.GuardianId);
            await CreateBlockedMessageAsync(command, "NO_PHONE_NUMBER", cancellationToken);
            return false;
        }

        var messageBody = await RenderBodyAsync(command, cancellationToken);
        var localizedTexts = await BuildLocalizedTextsAsync(messageBody, preference, cancellationToken);
        var sendResult = await _twilioWhatsAppSender.SendWhatsAppAsync(guardian.MobileE164, messageBody, cancellationToken);

        var message = new Message
        {
            CaseId = command.CaseId,
            StudentId = command.StudentId,
            GuardianId = command.GuardianId,
            Channel = "WHATSAPP",
            ThreadId = null,
            Direction = "OUTBOUND",
            MessageType = command.MessageType,
            Status = sendResult.Success ? "SENT" : "FAILED",
            ProviderMessageId = sendResult.ProviderMessageId,
            Provider = "TWILIO_WHATSAPP",
            Body = messageBody,
            TemplateId = command.TemplateId,
            IdempotencyKey = BuildIdempotencyKey(command),
            ErrorCode = sendResult.Success ? null : "WHATSAPP_ERROR",
            ErrorMessage = sendResult.ErrorMessage,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            SentAtUtc = sendResult.Success ? DateTimeOffset.UtcNow : null
        };
        foreach (var lt in localizedTexts)
        {
            lt.MessageId = message.MessageId;
            message.LocalizedTexts.Add(lt);
        }

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (sendResult.Success)
        {
            await PublishDeliveryEventAsync(message, sendResult.Status ?? "sent", cancellationToken);
        }

        _logger.LogInformation(
            "WhatsApp message processed. MessageId: {MessageId}, Status: {Status}",
            message.MessageId,
            message.Status);

        return sendResult.Success;
    }

    private static IReadOnlyList<string> GetChannelPriority(SendMessageRequestedV1 command, IReadOnlyList<string>? preferredOrder)
    {
        if (preferredOrder is { Count: > 0 })
        {
            return preferredOrder;
        }

        // Simple fallback ordering
        return command.Channel switch
        {
            "EMAIL" => new[] { "EMAIL", "SMS", "WHATSAPP" },
            "WHATSAPP" => new[] { "WHATSAPP", "SMS", "EMAIL" },
            _ => new[] { "SMS", "EMAIL", "WHATSAPP" }
        };
    }

    private async Task CreateBlockedMessageAsync(SendMessageRequestedV1 command, string reason, CancellationToken cancellationToken)
    {
        var message = new Message
        {
            CaseId = command.CaseId,
            StudentId = command.StudentId,
            GuardianId = command.GuardianId,
            ThreadId = null,
            Direction = "OUTBOUND",
            Channel = command.Channel,
            MessageType = command.MessageType,
            Status = "BLOCKED",
            IdempotencyKey = BuildIdempotencyKey(command),
            ErrorCode = reason,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Message blocked. MessageId: {MessageId}, Reason: {Reason}",
            message.MessageId,
            reason);
    }

    private static IReadOnlyList<string>? GetPreferredChannels(ContactPreference? preference)
    {
        if (preference?.PreferredChannelsJson is null)
        {
            return null;
        }

        try
        {
            var doc = JsonDocument.Parse(preference.PreferredChannelsJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.String)
                    {
                        list.Add(el.GetString() ?? string.Empty);
                    }
                }
                return list;
            }
        }
        catch
        {
            // ignore parse errors
        }

        return null;
    }

    private static bool IsWithinQuietHours(ContactPreference? preference)
    {
        if (preference?.QuietHoursJson is null)
        {
            return false;
        }

        try
        {
            var doc = JsonDocument.Parse(preference.QuietHoursJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var root = doc.RootElement;
            var start = root.TryGetProperty("start", out var startProp) ? startProp.GetString() : null;
            var end = root.TryGetProperty("end", out var endProp) ? endProp.GetString() : null;
            var tz = root.TryGetProperty("timezone", out var tzProp) ? tzProp.GetString() : "UTC";
            if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end))
            {
                return false;
            }

            var startTime = TimeSpan.Parse(start);
            var endTime = TimeSpan.Parse(end);
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tz ?? "UTC");
            var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tzInfo).TimeOfDay;

            return startTime <= endTime
                ? nowLocal >= startTime && nowLocal <= endTime
                : nowLocal >= startTime || nowLocal <= endTime; // overnight window
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> IsTranslationReviewRequiredAsync(CancellationToken ct)
    {
        if (_tenantContext.SchoolId.HasValue)
        {
            var settings = await _dbContext.SchoolSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SchoolId == _tenantContext.SchoolId.Value, ct);
            if (settings != null)
            {
                return settings.TranslationReviewRequired;
            }
        }

        return false;
    }

    private async Task<IReadOnlyList<MessageLocalizedText>> BuildLocalizedTextsAsync(string originalBody, ContactPreference? preference, CancellationToken ct)
    {
        var list = new List<MessageLocalizedText>
        {
            new()
            {
                LocalizedTextId = Guid.NewGuid(),
                LanguageCode = "en",
                Body = originalBody,
                IsOriginal = true
            }
        };

        var preferredLanguage = preference?.PreferredLanguage;
        if (!string.IsNullOrWhiteSpace(preferredLanguage) && !string.Equals(preferredLanguage, "en", StringComparison.OrdinalIgnoreCase))
        {
            var translated = await _translationService.TranslateAsync(originalBody, "en", preferredLanguage!, ct);
            list.Add(new MessageLocalizedText
            {
                LocalizedTextId = Guid.NewGuid(),
                LanguageCode = preferredLanguage!,
                Body = translated,
                IsOriginal = false
            });
        }

        return list;
    }

    private async Task<string> RenderBodyAsync(SendMessageRequestedV1 command, CancellationToken cancellationToken)
    {
        var (policyPackDoc, data) = await LoadTemplateContextAsync(command, cancellationToken);
        var templateId = command.TemplateId ?? "ABSENCE_FIRST_CONTACT_SMS";
        var rendered = _templateEngine.Render(policyPackDoc.RootElement, templateId, data);
        return rendered.Body;
    }

    private async Task<(string Subject, string HtmlBody, string PlainTextBody)> RenderEmailAsync(
        SendMessageRequestedV1 command,
        CancellationToken cancellationToken)
    {
        var (policyPackDoc, data) = await LoadTemplateContextAsync(command, cancellationToken);
        var templateId = command.TemplateId ?? "ABSENCE_FIRST_CONTACT_EMAIL";
        var rendered = _templateEngine.Render(policyPackDoc.RootElement, templateId, data);

        var plainText = HtmlToPlainText(rendered.Body);
        return (rendered.Subject ?? "Message from your school", rendered.Body, plainText);
    }

    private async Task<(JsonDocument PolicyPackDoc, Dictionary<string, string> Data)> LoadTemplateContextAsync(
        SendMessageRequestedV1 command,
        CancellationToken cancellationToken)
    {
        var tenant = await _dbContext.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantId == _tenantContext.TenantId, cancellationToken);

        var policyPackPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "policy-packs",
            tenant?.CountryCode ?? "ie",
            tenant?.DefaultPolicyPackId ?? "IE-ANSEO-DEFAULT",
            tenant?.DefaultPolicyPackVersion ?? "1.3.0",
            "templates.json");

        var policyPackJson = await File.ReadAllTextAsync(policyPackPath, cancellationToken);
        var policyPackDoc = JsonDocument.Parse(policyPackJson);

        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (command.TemplateData != null)
        {
            foreach (var kvp in command.TemplateData)
            {
                if (kvp.Value is string s)
                {
                    data[kvp.Key] = s;
                }
            }
        }

        // Common fields
        if (!data.ContainsKey("StudentName") && command.TemplateData != null && command.TemplateData.TryGetValue("StudentName", out var sn))
        {
            data["StudentName"] = sn;
        }
        if (!data.ContainsKey("Date"))
        {
            data["Date"] = DateTime.UtcNow.ToString("d");
        }

        return (policyPackDoc, data);
    }

    private static string HtmlToPlainText(string html)
    {
        // Minimal HTML strip for v0.1
        return html
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<p>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("</p>", "\n\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<strong>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("</strong>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("<html>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("</html>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("<body>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("</body>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private async Task PublishDeliveryEventAsync(Message message, string status, CancellationToken cancellationToken)
    {
        var payload = new MessageDeliveryUpdatedV1(
            MessageId: message.MessageId,
            Provider: message.Provider ?? "UNKNOWN",
            Status: status,
            ProviderMessageId: message.ProviderMessageId,
            ErrorCode: message.ErrorCode,
            ErrorMessage: message.ErrorMessage
        );

        var envelope = new MessageEnvelope<MessageDeliveryUpdatedV1>(
            MessageType: MessageTypes.MessageDeliveryUpdatedV1,
            Version: MessageVersions.V1,
            TenantId: _tenantContext.TenantId,
            SchoolId: _tenantContext.SchoolId ?? Guid.Empty,
            CorrelationId: Guid.NewGuid().ToString(),
            OccurredAtUtc: DateTimeOffset.UtcNow,
            Payload: payload
        );

        await _messageBus.PublishAsync(envelope, cancellationToken);
    }

    private async Task<bool> TrySendWithRetry(Func<Task<bool>> send, string channel)
    {
        try
        {
            return await RetryPolicy.ExecuteAsync(async () => await send());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send failed after retries for channel {Channel}", channel);
            return false;
        }
    }

    private static string BuildIdempotencyKey(SendMessageRequestedV1 command)
    {
        return $"{command.GuardianId}:{command.StudentId}:{command.CaseId}:{command.Channel}:{command.TemplateId}:{command.MessageType}";
    }
}
