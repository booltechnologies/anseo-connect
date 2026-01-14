namespace AnseoConnect.Data.Entities;

/// <summary>
/// Append-only audit log entry for sensitive operations (GDPR compliance).
/// </summary>
public sealed class AuditEvent
{
    public Guid AuditEventId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? SchoolId { get; set; }
    
    // Actor information
    public string ActorId { get; set; } = ""; // User ID or "system"
    public string ActorName { get; set; } = "";
    public string ActorType { get; set; } = ""; // Staff, Guardian, System
    public string IpAddress { get; set; } = "";
    public string UserAgent { get; set; } = "";
    
    // Action details
    public string Action { get; set; } = ""; // e.g., "student:viewed", "evidence:exported"
    public string EntityType { get; set; } = ""; // Student, Case, Message, EvidencePack
    public string EntityId { get; set; } = "";
    public string EntityDisplayName { get; set; } = ""; // For display in UI
    
    // Additional context
    public string MetadataJson { get; set; } = "{}"; // Extra details (filters used, export params)
    public DateTimeOffset OccurredAtUtc { get; set; }
    
    // Integrity (optional - for tamper evidence)
    public string? PreviousEventHash { get; set; }
    public string? EventHash { get; set; }
}
