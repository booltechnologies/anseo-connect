using AnseoConnect.Contracts.Commands;
using AnseoConnect.Contracts.Common;
using AnseoConnect.Contracts.Events;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.PolicyRuntime;
using AnseoConnect.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AnseoConnect.Comms.Services;

/// <summary>
/// Service that processes message requests, evaluates consent, and sends messages.
/// </summary>
public sealed class MessageService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly SendmodeSender? _sendmodeSender;
    private readonly SendGridEmailSender? _sendGridEmailSender;
    private readonly IConsentEvaluator _consentEvaluator;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<MessageService> _logger;
    private readonly ITenantContext _tenantContext;

    public MessageService(
        AnseoConnectDbContext dbContext,
        SendmodeSender? sendmodeSender,
        SendGridEmailSender? sendGridEmailSender,
        IConsentEvaluator consentEvaluator,
        IMessageBus messageBus,
        ILogger<MessageService> logger,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _sendmodeSender = sendmodeSender;
        _sendGridEmailSender = sendGridEmailSender;
        _consentEvaluator = consentEvaluator;
        _messageBus = messageBus;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Processes a SendMessageRequestedV1 command.
    /// </summary>
    public async Task ProcessMessageRequestAsync(SendMessageRequestedV1 command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing message request for case {CaseId}, student {StudentId}, guardian {GuardianId}, channel {Channel}",
            command.CaseId,
            command.StudentId,
            command.GuardianId,
            command.Channel);

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

        // Route to channel-specific sender
        if (command.Channel == "SMS")
        {
            await SendSmsAsync(command, guardian, cancellationToken);
        }
        else if (command.Channel == "EMAIL")
        {
            await SendEmailAsync(command, guardian, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Channel {Channel} not yet supported in v0.1", command.Channel);
            await CreateBlockedMessageAsync(command, "CHANNEL_NOT_SUPPORTED", cancellationToken);
        }
    }

    private async Task SendSmsAsync(SendMessageRequestedV1 command, Guardian guardian, CancellationToken cancellationToken)
    {
        if (_sendmodeSender == null)
        {
            _logger.LogWarning("SendmodeSender not configured. Cannot send SMS.");
            await CreateBlockedMessageAsync(command, "SMS_PROVIDER_NOT_CONFIGURED", cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(guardian.MobileE164))
        {
            _logger.LogWarning("Guardian {GuardianId} does not have mobile number", command.GuardianId);
            await CreateBlockedMessageAsync(command, "NO_PHONE_NUMBER", cancellationToken);
            return;
        }

        // Generate message body from template
        var messageBody = GenerateMessageBody(command);

        // Send via Sendmode
        var sendResult = await _sendmodeSender.SendSmsAsync(guardian.MobileE164, messageBody, cancellationToken);

        // Create message record
        var message = new Message
        {
            CaseId = command.CaseId,
            StudentId = command.StudentId,
            GuardianId = command.GuardianId,
            Channel = command.Channel,
            MessageType = command.MessageType,
            Status = sendResult.Success ? "SENT" : "FAILED",
            ProviderMessageId = sendResult.ProviderMessageId,
            Provider = "SENDMODE",
            Body = messageBody,
            TemplateId = command.TemplateId,
            ErrorCode = sendResult.Success ? null : "SENDMODE_ERROR",
            ErrorMessage = sendResult.ErrorMessage,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            SentAtUtc = sendResult.Success ? DateTimeOffset.UtcNow : null
        };

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
    }

    private async Task SendEmailAsync(SendMessageRequestedV1 command, Guardian guardian, CancellationToken cancellationToken)
    {
        if (_sendGridEmailSender == null)
        {
            _logger.LogWarning("SendGridEmailSender not configured. Cannot send email.");
            await CreateBlockedMessageAsync(command, "EMAIL_PROVIDER_NOT_CONFIGURED", cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(guardian.Email))
        {
            _logger.LogWarning("Guardian {GuardianId} does not have email address", command.GuardianId);
            await CreateBlockedMessageAsync(command, "NO_EMAIL_ADDRESS", cancellationToken);
            return;
        }

        // Generate email content (subject, HTML, plain text) from template
        var (subject, htmlBody, plainTextBody) = GenerateEmailContent(command);

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
            Channel = command.Channel,
            MessageType = command.MessageType,
            Status = sendResult.Success ? "SENT" : "FAILED",
            ProviderMessageId = sendResult.ProviderMessageId,
            Provider = "SENDGRID",
            Body = htmlBody, // Store HTML body in Body field
            TemplateId = command.TemplateId,
            ErrorCode = sendResult.Success ? null : "SENDGRID_ERROR",
            ErrorMessage = sendResult.ErrorMessage,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            SentAtUtc = sendResult.Success ? DateTimeOffset.UtcNow : null
        };

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
    }

    private async Task CreateBlockedMessageAsync(SendMessageRequestedV1 command, string reason, CancellationToken cancellationToken)
    {
        var message = new Message
        {
            CaseId = command.CaseId,
            StudentId = command.StudentId,
            GuardianId = command.GuardianId,
            Channel = command.Channel,
            MessageType = command.MessageType,
            Status = "BLOCKED",
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

    private static string GenerateMessageBody(SendMessageRequestedV1 command)
    {
        // Simplified template for v0.1 - in production, load from policy pack or template store
        if (command.MessageType == "SERVICE_ATTENDANCE" && command.TemplateData != null)
        {
            var studentName = command.TemplateData.TryGetValue("StudentName", out var sn) ? sn : "your child";
            var date = command.TemplateData.TryGetValue("Date", out var d) ? d : DateTime.UtcNow.ToString("d");
            return $"Dear parent/guardian, {studentName} was absent on {date}. Please contact the school if you have any concerns.";
        }

        return "This is a message from your school. Please contact the school for more information.";
    }

    private static (string Subject, string HtmlBody, string PlainTextBody) GenerateEmailContent(SendMessageRequestedV1 command)
    {
        // Generate subject and body for email
        string subject;
        string htmlBody;
        string plainTextBody;

        if (command.MessageType == "SERVICE_ATTENDANCE" && command.TemplateData != null)
        {
            var studentName = command.TemplateData.TryGetValue("StudentName", out var sn) ? sn : "your child";
            var date = command.TemplateData.TryGetValue("Date", out var d) ? d : DateTime.UtcNow.ToString("d");
            
            subject = $"Attendance Notification - {studentName}";
            plainTextBody = $"Dear parent/guardian,\n\n{studentName} was absent on {date}. Please contact the school if you have any concerns.\n\nThank you.";
            htmlBody = $@"
<html>
<body>
    <p>Dear parent/guardian,</p>
    <p><strong>{studentName}</strong> was absent on <strong>{date}</strong>. Please contact the school if you have any concerns.</p>
    <p>Thank you.</p>
</body>
</html>";
        }
        else
        {
            subject = "Message from your school";
            plainTextBody = "This is a message from your school. Please contact the school for more information.";
            htmlBody = "<html><body><p>This is a message from your school. Please contact the school for more information.</p></body></html>";
        }

        return (subject, htmlBody, plainTextBody);
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
}
