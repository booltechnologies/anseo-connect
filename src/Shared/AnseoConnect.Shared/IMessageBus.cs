using AnseoConnect.Contracts.Common;

namespace AnseoConnect.Shared;

/// <summary>
/// Abstraction for message bus publishing.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes a message to the service bus.
    /// </summary>
    Task PublishAsync<T>(MessageEnvelope<T> envelope, CancellationToken cancellationToken = default) where T : notnull;
}
