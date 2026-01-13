namespace AnseoConnect.Comms.Services;

/// <summary>
/// Enqueues work items into the transactional outbox.
/// </summary>
public interface IOutboxDispatcher
{
    Task EnqueueAsync<T>(T message, string type, string idempotencyKey, Guid tenantId, Guid? schoolId, CancellationToken ct);
}
