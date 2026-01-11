using AnseoConnect.Contracts.Common;
using AnseoConnect.Data.MultiTenancy;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AnseoConnect.Shared;

/// <summary>
/// Background service for consuming Service Bus messages.
/// </summary>
public abstract class ServiceBusMessageConsumer : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly string _topicName;
    private readonly string _subscriptionName;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger _logger;
    protected readonly IServiceProvider ServiceProvider;

    protected ServiceBusMessageConsumer(
        string connectionString,
        string topicName,
        string subscriptionName,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        _client = new ServiceBusClient(connectionString);
        _topicName = topicName;
        _subscriptionName = subscriptionName;
        ServiceProvider = serviceProvider;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processor = _client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1,
            PrefetchCount = 10
        });

        processor.ProcessMessageAsync += HandleMessageAsync;
        processor.ProcessErrorAsync += HandleErrorAsync;

        await processor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation("Started consuming messages from topic {Topic}, subscription {Subscription}", _topicName, _subscriptionName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }

        await processor.StopProcessingAsync(cancellationToken: stoppingToken);
        await processor.CloseAsync(cancellationToken: stoppingToken);
        await processor.DisposeAsync();
        await _client.DisposeAsync();
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var messageType = args.Message.ApplicationProperties.TryGetValue("MessageType", out var mt) 
                ? mt?.ToString() 
                : args.Message.Subject;

            if (string.IsNullOrEmpty(messageType))
            {
                _logger.LogWarning("Message has no MessageType, skipping. MessageId: {MessageId}", args.Message.MessageId);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            var version = args.Message.ApplicationProperties.TryGetValue("Version", out var v) 
                ? v?.ToString() ?? "1.0"
                : "1.0";

            var tenantIdStr = args.Message.ApplicationProperties.TryGetValue("TenantId", out var tid) 
                ? tid?.ToString() 
                : null;
            var schoolIdStr = args.Message.ApplicationProperties.TryGetValue("SchoolId", out var sid) 
                ? sid?.ToString() 
                : null;

            if (!Guid.TryParse(tenantIdStr, out var tenantId) || tenantId == Guid.Empty)
            {
                _logger.LogError("Message has invalid TenantId, sending to dead-letter. MessageId: {MessageId}", args.Message.MessageId);
                await args.DeadLetterMessageAsync(args.Message, "InvalidTenantId", "TenantId is missing or invalid");
                return;
            }

            Guid? schoolId = null;
            if (!string.IsNullOrEmpty(schoolIdStr) && Guid.TryParse(schoolIdStr, out var parsedSchoolId))
            {
                schoolId = parsedSchoolId;
            }

            var correlationId = args.Message.CorrelationId ?? args.Message.MessageId ?? Guid.NewGuid().ToString();

            // Set TenantContext before processing
            using var scope = ServiceProvider.CreateScope();
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            if (tenantContext is TenantContext tc)
            {
                tc.Set(tenantId, schoolId);
            }

            var payloadJson = Encoding.UTF8.GetString(args.Message.Body);
            var occurredAtUtc = args.Message.ApplicationProperties.TryGetValue("OccurredAtUtc", out var oa) && 
                                DateTimeOffset.TryParse(oa?.ToString(), out var parsedDate)
                ? parsedDate
                : DateTimeOffset.UtcNow;

            await ProcessMessageAsync(messageType, version, tenantId, schoolId, correlationId, occurredAtUtc, payloadJson, scope, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message. MessageId: {MessageId}", args.Message.MessageId);
            
            // Dead-letter if max delivery count reached
            if (args.Message.DeliveryCount >= 10)
            {
                await args.DeadLetterMessageAsync(args.Message, "MaxDeliveryCountExceeded", ex.Message);
            }
            else
            {
                // Let Service Bus retry
                throw;
            }
        }
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            "Service Bus processor error. Source: {ErrorSource}, EntityPath: {EntityPath}",
            args.ErrorSource,
            args.EntityPath);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Process the message. Override in derived classes to handle specific message types.
    /// </summary>
    protected abstract Task ProcessMessageAsync(
        string messageType,
        string version,
        Guid tenantId,
        Guid? schoolId,
        string correlationId,
        DateTimeOffset occurredAtUtc,
        string payloadJson,
        IServiceScope scope,
        CancellationToken cancellationToken);
}
