using AnseoConnect.Contracts.Common;
using AnseoConnect.Contracts.Events;
using AnseoConnect.Data;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Shared;
using AnseoConnect.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AnseoConnect.Workflow.Consumers;

/// <summary>
/// Consumer for message-related events (delivery updates, replies, opt-outs) that need to update case timelines.
/// </summary>
public sealed class MessageEventConsumer : ServiceBusMessageConsumer
{
    public MessageEventConsumer(
        string connectionString,
        IServiceProvider serviceProvider,
        ILogger<MessageEventConsumer> logger)
        : base(connectionString, "comms", "workflow-message-events", serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(
        string messageType,
        string version,
        Guid tenantId,
        Guid? schoolId,
        string correlationId,
        DateTimeOffset occurredAtUtc,
        string payloadJson,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MessageEventConsumer>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnseoConnectDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var caseService = scope.ServiceProvider.GetRequiredService<CaseService>();

        logger.LogInformation(
            "Received message event {MessageType} v{Version} for tenant {TenantId}, school {SchoolId}, correlation {CorrelationId}",
            messageType,
            version,
            tenantId,
            schoolId,
            correlationId);

        // Set tenant context for this message
        if (tenantContext is TenantContext tc && schoolId.HasValue)
        {
            tc.Set(tenantId, schoolId.Value);
        }

        try
        {
            if (messageType == MessageTypes.MessageDeliveryUpdatedV1 && version == MessageVersions.V1)
            {
                var payload = JsonSerializer.Deserialize<MessageDeliveryUpdatedV1>(payloadJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payload != null && payload.MessageId != Guid.Empty)
                {
                    // Get message to find associated case
                    var message = await dbContext.Messages
                        .AsNoTracking()
                        .Where(m => m.MessageId == payload.MessageId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (message != null && message.CaseId.HasValue)
                    {
                        await caseService.AddTimelineEventAsync(
                            message.CaseId.Value,
                            $"MESSAGE_DELIVERY_{payload.Status}",
                            JsonSerializer.Serialize(new { payload.Provider, payload.Status, payload.ProviderMessageId }),
                            "SYSTEM",
                            cancellationToken);

                        logger.LogInformation(
                            "Added delivery event to case {CaseId}: {Status}",
                            message.CaseId.Value,
                            payload.Status);
                    }
                }
            }
            else if (messageType == MessageTypes.GuardianReplyReceivedV1 && version == MessageVersions.V1)
            {
                var payload = JsonSerializer.Deserialize<GuardianReplyReceivedV1>(payloadJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payload != null && payload.MessageId != Guid.Empty)
                {
                    // Get message to find associated case
                    var message = await dbContext.Messages
                        .AsNoTracking()
                        .Where(m => m.MessageId == payload.MessageId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (message != null && message.CaseId.HasValue)
                    {
                        await caseService.AddTimelineEventAsync(
                            message.CaseId.Value,
                            payload.IsOptOutKeyword ? "GUARDIAN_OPT_OUT_REPLY" : "GUARDIAN_REPLY_RECEIVED",
                            JsonSerializer.Serialize(new { payload.Channel, payload.Text, payload.IsOptOutKeyword }),
                            "SYSTEM",
                            cancellationToken);

                        logger.LogInformation(
                            "Added reply event to case {CaseId}: {EventType}",
                            message.CaseId.Value,
                            payload.IsOptOutKeyword ? "OPT_OUT" : "REPLY");
                    }
                }
            }
            else if (messageType == MessageTypes.GuardianOptOutRecordedV1 && version == MessageVersions.V1)
            {
                var payload = JsonSerializer.Deserialize<GuardianOptOutRecordedV1>(payloadJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payload != null)
                {
                    // Find all open cases for this guardian
                    var openCases = await dbContext.Cases
                        .AsNoTracking()
                        .Where(c => c.Status == "OPEN" &&
                                   dbContext.Messages.Any(m => m.CaseId == c.CaseId && m.GuardianId == payload.GuardianId))
                        .Select(c => c.CaseId)
                        .ToListAsync(cancellationToken);

                    foreach (var caseId in openCases)
                    {
                        await caseService.AddTimelineEventAsync(
                            caseId,
                            "GUARDIAN_OPT_OUT_RECORDED",
                            JsonSerializer.Serialize(new { payload.GuardianId, payload.Channel, payload.Source }),
                            "SYSTEM",
                            cancellationToken);
                    }

                    logger.LogInformation(
                        "Added opt-out event to {CaseCount} cases for guardian {GuardianId}",
                        openCases.Count,
                        payload.GuardianId);
                }
            }
            else
            {
                logger.LogWarning("Unknown message type {MessageType} v{Version}. CorrelationId: {CorrelationId}", messageType, version, correlationId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message event {MessageType}. CorrelationId: {CorrelationId}", messageType, correlationId);
            throw; // Will be retried by Service Bus
        }
    }
}
