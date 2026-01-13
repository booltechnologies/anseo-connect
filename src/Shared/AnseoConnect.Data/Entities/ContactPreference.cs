using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Guardian contact preferences including language, channel priority, and quiet hours.
/// </summary>
public sealed class ContactPreference : SchoolEntity
{
    public Guid ContactPreferenceId { get; set; }
    public Guid GuardianId { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public string PreferredChannelsJson { get; set; } = "[\"SMS\",\"EMAIL\"]";
    public string? QuietHoursJson { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guardian? Guardian { get; set; }
}
