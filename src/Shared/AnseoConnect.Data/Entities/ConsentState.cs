using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Guardian consent state for a specific communication channel.
/// </summary>
public sealed class ConsentState : SchoolEntity
{
    public Guid ConsentStateId { get; set; }
    public Guid GuardianId { get; set; }

    public string Channel { get; set; } = "SMS"; // SMS, EMAIL, WHATSAPP, VOICE_AUTODIAL
    public string State { get; set; } = "UNKNOWN"; // UNKNOWN, OPTED_IN, OPTED_OUT
    public string Source { get; set; } = "PLATFORM"; // PLATFORM, API, WEBHOOK
    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; } // User ID or system identifier

    public Guardian? Guardian { get; set; }
}
