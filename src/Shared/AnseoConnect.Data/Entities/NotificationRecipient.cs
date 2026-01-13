namespace AnseoConnect.Data.Entities;

/// <summary>
/// Configured recipients for routed notifications (e.g., safeguarding).
/// </summary>
public sealed class NotificationRecipient : SchoolEntity
{
    public Guid NotificationRecipientId { get; set; }

    public string Route { get; set; } = string.Empty; // e.g., SAFEGUARDING_DEFAULT
    public StaffRole? Role { get; set; }
    public Guid? UserId { get; set; }
    public int Priority { get; set; } = 1; // Ordering for routing
}
