namespace AnseoConnect.Data.Entities;

/// <summary>
/// In-app notification for staff users.
/// </summary>
public sealed class Notification : SchoolEntity
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }

    public string Type { get; set; } = string.Empty; // e.g., SAFEGUARDING_ALERT
    public string Payload { get; set; } = string.Empty; // JSON payload
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAtUtc { get; set; }
}
