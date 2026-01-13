using System.Text.Json;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Comms.Services;

/// <summary>
/// Writes work items into the transactional outbox.
/// </summary>
public sealed class OutboxDispatcher : IOutboxDispatcher
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(AnseoConnectDbContext dbContext, ILogger<OutboxDispatcher> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task EnqueueAsync<T>(T message, string type, string idempotencyKey, Guid tenantId, Guid? schoolId, CancellationToken ct)
    {
        // Deduplicate on idempotency key per tenant
        var exists = await _dbContext.OutboxMessages
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.IdempotencyKey == idempotencyKey, ct);
        if (exists)
        {
            _logger.LogInformation("Outbox message with idempotency key {IdempotencyKey} already exists for tenant {TenantId}", idempotencyKey, tenantId);
            return;
        }

        var payloadJson = JsonSerializer.Serialize(message);
        var outbox = new OutboxMessage
        {
            OutboxMessageId = Guid.NewGuid(),
            TenantId = tenantId,
            SchoolId = schoolId,
            Type = type,
            PayloadJson = payloadJson,
            IdempotencyKey = idempotencyKey,
            Status = "PENDING",
            AttemptCount = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            NextAttemptUtc = DateTimeOffset.UtcNow
        };

        _dbContext.OutboxMessages.Add(outbox);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Queued outbox message {OutboxId} type {Type} for tenant {TenantId}", outbox.OutboxMessageId, type, tenantId);
    }
}
