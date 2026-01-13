using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Audit trail of consent changes for a guardian and channel.
/// </summary>
public sealed class ConsentRecord : SchoolEntity
{
    public Guid ConsentRecordId { get; set; }
    public Guid GuardianId { get; set; }
    public string Channel { get; set; } = "SMS";
    public string Action { get; set; } = "OPTED_IN"; // OPTED_IN, OPTED_OUT
    public string Source { get; set; } = "PLATFORM"; // PORTAL, STAFF_OVERRIDE, WEBHOOK, IMPORT
    public string? Notes { get; set; }
    public DateTimeOffset CapturedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CapturedByUserId { get; set; }

    public Guardian? Guardian { get; set; }
}
