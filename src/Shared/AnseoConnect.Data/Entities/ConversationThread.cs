using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Represents a conversation between staff and guardians, optionally scoped to a student.
/// </summary>
public sealed class ConversationThread : SchoolEntity
{
    public Guid ThreadId { get; set; }
    public Guid? StudentId { get; set; }
    public string Status { get; set; } = "OPEN"; // OPEN, CLOSED, ARCHIVED
    public string? Tags { get; set; } // JSON array of tags/labels
    public Guid? OwnerUserId { get; set; }
    public DateTimeOffset LastActivityUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Student? Student { get; set; }
    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
