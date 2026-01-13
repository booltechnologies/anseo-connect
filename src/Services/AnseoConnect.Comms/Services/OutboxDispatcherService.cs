using System.Text.Json;
using AnseoConnect.Contracts.Commands;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AnseoConnect.Comms.Services;

/// <summary>
/// Background service that dispatches pending outbox messages to providers with retries.
/// </summary>
public sealed class OutboxDispatcherService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OutboxDispatcherService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private const int MaxAttempts = 5;

    public OutboxDispatcherService(IServiceProvider services, ILogger<OutboxDispatcherService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxDispatcherService started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatcher batch failed");
            }

            try
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }
    }

    private async Task DispatchBatchAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnseoConnectDbContext>();
        var tenantContext = scope.ServiceProvider.GetService<ITenantContext>() as TenantContext;
        var now = DateTimeOffset.UtcNow;

        var items = await db.OutboxMessages
            .Where(x => x.Status == "PENDING" && (x.NextAttemptUtc == null || x.NextAttemptUtc <= now))
            .OrderBy(x => x.CreatedAtUtc)
            .Take(10)
            .ToListAsync(ct);

        foreach (var item in items)
        {
            _logger.LogInformation("Dispatching outbox item {OutboxId} type {Type}", item.OutboxMessageId, item.Type);
            item.Status = "PROCESSING";
            item.AttemptCount += 1;
            item.NextAttemptUtc = now.AddSeconds(Math.Pow(2, Math.Min(item.AttemptCount, 5)));
            await db.SaveChangesAsync(ct);

            try
            {
                tenantContext?.Set(item.TenantId, item.SchoolId);
                await HandleItemAsync(scope.ServiceProvider, item, ct);
                item.Status = "COMPLETED";
                item.LastError = null;
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing outbox item {OutboxId} attempt {Attempt}", item.OutboxMessageId, item.AttemptCount);
                item.LastError = ex.Message;
                if (item.AttemptCount >= MaxAttempts)
                {
                    item.Status = "FAILED";
                    await MoveToDeadLetterAsync(db, item, ex.Message, ct);
                }
                else
                {
                    item.Status = "PENDING";
                }

                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static async Task HandleItemAsync(IServiceProvider serviceProvider, OutboxMessage item, CancellationToken ct)
    {
        switch (item.Type)
        {
            case "SEND_MESSAGE":
                {
                    var command = JsonSerializer.Deserialize<SendMessageRequestedV1>(item.PayloadJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? throw new InvalidOperationException("Outbox payload could not be deserialized.");
                    var messageService = serviceProvider.GetRequiredService<MessageService>();
                    await messageService.SendViaProvidersAsync(command, ct);
                    break;
                }
            default:
                throw new InvalidOperationException($"Unknown outbox type {item.Type}");
        }
    }

    private static async Task MoveToDeadLetterAsync(AnseoConnectDbContext db, OutboxMessage item, string reason, CancellationToken ct)
    {
        var dead = new DeadLetterMessage
        {
            DeadLetterId = Guid.NewGuid(),
            TenantId = item.TenantId,
            OriginalOutboxId = item.OutboxMessageId,
            Type = item.Type,
            PayloadJson = item.PayloadJson,
            FailureReason = reason,
            FailedAtUtc = DateTimeOffset.UtcNow
        };

        db.DeadLetterMessages.Add(dead);
        await db.SaveChangesAsync(ct);
    }
}
