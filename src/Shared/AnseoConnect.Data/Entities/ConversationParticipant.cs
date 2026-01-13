using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Participant in a conversation thread (staff user or guardian).
/// </summary>
public sealed class ConversationParticipant : SchoolEntity
{
    public Guid ParticipantId { get; set; }
    public Guid ThreadId { get; set; }
    public string ParticipantType { get; set; } = "STAFF"; // STAFF, GUARDIAN
    public Guid ParticipantRefId { get; set; } // AppUserId or GuardianId
    public DateTimeOffset JoinedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ConversationThread? Thread { get; set; }
}
