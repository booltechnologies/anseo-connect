using AnseoConnect.Comms.Services;
using AnseoConnect.Contracts.Commands;
using AnseoConnect.Contracts.Common;
using AnseoConnect.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AnseoConnect.Comms.Consumers;

/// <summary>
/// Consumer for SendMessageRequestedV1 commands from the comms topic.
/// </summary>
public sealed class SendMessageRequestedConsumer : ServiceBusMessageConsumer
{
    public SendMessageRequestedConsumer(
        string connectionString,
        IServiceProvider serviceProvider,
        ILogger<SendMessageRequestedConsumer> logger)
        : base(connectionString, "comms", "comms-send-message", serviceProvider, logger)
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
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SendMessageRequestedConsumer>>();

        logger.LogInformation(
            "Received message {MessageType} v{Version} for tenant {TenantId}, school {SchoolId}, correlation {CorrelationId}",
            messageType,
            version,
            tenantId,
            schoolId,
            correlationId);

        if (messageType == MessageTypes.SendMessageRequestedV1 && version == MessageVersions.V1)
        {
            try
            {
                var command = JsonSerializer.Deserialize<SendMessageRequestedV1>(payloadJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (command == null)
                {
                    logger.LogWarning("Failed to deserialize SendMessageRequestedV1 payload. CorrelationId: {CorrelationId}", correlationId);
                    return;
                }

                var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();
                await messageService.ProcessMessageRequestAsync(command, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing SendMessageRequestedV1 message. CorrelationId: {CorrelationId}", correlationId);
                throw; // Will be retried by Service Bus
            }
        }
        else
        {
            logger.LogWarning("Unknown message type {MessageType} v{Version}. CorrelationId: {CorrelationId}", messageType, version, correlationId);
        }
    }
}
