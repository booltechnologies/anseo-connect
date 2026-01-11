using AnseoConnect.Contracts.Common;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AnseoConnect.Shared;

/// <summary>
/// Azure Service Bus implementation of IMessageBus.
/// </summary>
public sealed class ServiceBusMessageBus : IMessageBus, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly Dictionary<string, ServiceBusSender> _senders = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<ServiceBusMessageBus> _logger;

    public ServiceBusMessageBus(string connectionString, ILogger<ServiceBusMessageBus> logger)
    {
        _client = new ServiceBusClient(connectionString);
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task PublishAsync<T>(MessageEnvelope<T> envelope, CancellationToken cancellationToken = default) where T : notnull
    {
        var topicName = GetTopicName(envelope.MessageType);
        var sender = GetOrCreateSender(topicName);

        var payloadJson = JsonSerializer.Serialize(envelope.Payload, _jsonOptions);
        var messageBody = Encoding.UTF8.GetBytes(payloadJson);

        var message = new ServiceBusMessage(messageBody)
        {
            MessageId = Guid.NewGuid().ToString(),
            ContentType = "application/json",
            Subject = envelope.MessageType,
            CorrelationId = envelope.CorrelationId,
            ApplicationProperties =
            {
                { "MessageType", envelope.MessageType },
                { "Version", envelope.Version },
                { "TenantId", envelope.TenantId.ToString() },
                { "SchoolId", envelope.SchoolId.ToString() },
                { "CorrelationId", envelope.CorrelationId },
                { "OccurredAtUtc", envelope.OccurredAtUtc.ToString("O") }
            }
        };

        try
        {
            await sender.SendMessageAsync(message, cancellationToken);
            _logger.LogInformation(
                "Published message {MessageType} with correlation {CorrelationId} to topic {Topic}",
                envelope.MessageType,
                envelope.CorrelationId,
                topicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish message {MessageType} with correlation {CorrelationId} to topic {Topic}",
                envelope.MessageType,
                envelope.CorrelationId,
                topicName);
            throw;
        }
    }

    private string GetTopicName(string messageType)
    {
        // Map message types to topics
        return messageType switch
        {
            MessageTypes.AttendanceMarksIngestedV1 => "attendance",
            MessageTypes.SendMessageRequestedV1 => "comms",
            MessageTypes.MessageDeliveryUpdatedV1 or
            MessageTypes.GuardianReplyReceivedV1 or
            MessageTypes.GuardianOptOutRecordedV1 or
            MessageTypes.CaseCreatedV1 or
            MessageTypes.SafeguardingAlertCreatedV1 => "workflow",
            _ => throw new ArgumentException($"Unknown message type: {messageType}", nameof(messageType))
        };
    }

    private ServiceBusSender GetOrCreateSender(string topicName)
    {
        if (!_senders.TryGetValue(topicName, out var sender))
        {
            sender = _client.CreateSender(topicName);
            _senders[topicName] = sender;
        }
        return sender;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }
        _senders.Clear();
        await _client.DisposeAsync();
    }
}
