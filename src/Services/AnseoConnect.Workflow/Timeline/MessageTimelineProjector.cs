using AnseoConnect.Data;
using AnseoConnect.Data.Entities;

namespace AnseoConnect.Workflow.Timeline;

/// <summary>
/// Projects Message entities into timeline events.
/// </summary>
public sealed class MessageTimelineProjector : TimelineProjectorBase
{
    private readonly AnseoConnectDbContext _dbContext;
    
    public MessageTimelineProjector(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public override string SourceEntityType => "Message";
    public override string Category => "COMMS";
    
    public override Task<IReadOnlyList<TimelineEvent>> ProjectAsync(object sourceEntity, CancellationToken cancellationToken = default)
    {
        if (sourceEntity is not Message message)
        {
            return Task.FromResult<IReadOnlyList<TimelineEvent>>(Array.Empty<TimelineEvent>());
        }
        
        var events = new List<TimelineEvent>();
        
        // Project message sent event
        var evt = CreateEvent(
            message.StudentId,
            message.CaseId,
            $"MESSAGE_{message.Direction}_{message.Status}",
            message.CreatedAtUtc,
            title: $"Message sent via {message.Channel}",
            summary: message.Direction == "INBOUND" 
                ? $"Received message from guardian" 
                : $"Sent message to guardian via {message.Channel}",
            metadata: new { message.Channel, message.MessageType, message.Status, message.Direction }
        );
        SetSourceEntityId(evt, message.MessageId);
        events.Add(evt);
        
        // Project delivery event if delivered
        if (message.DeliveredAtUtc.HasValue)
        {
            var deliveryEvt = CreateEvent(
                message.StudentId,
                message.CaseId,
                "MESSAGE_DELIVERED",
                message.DeliveredAtUtc.Value,
                title: $"Message delivered via {message.Channel}",
                summary: $"Message was successfully delivered",
                metadata: new { message.Channel, message.Provider, message.ProviderMessageId }
            );
            SetSourceEntityId(deliveryEvt, message.MessageId);
            events.Add(deliveryEvt);
        }
        
        return Task.FromResult<IReadOnlyList<TimelineEvent>>(events);
    }
}
