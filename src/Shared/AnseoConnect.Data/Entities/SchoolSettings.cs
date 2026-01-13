using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Per-school operational configuration.
/// </summary>
public sealed class SchoolSettings : SchoolEntity
{
    public Guid SchoolSettingsId { get; set; }

    public TimeOnly AMCutoffTime { get; set; } = new(10, 30);
    public TimeOnly PMCutoffTime { get; set; } = new(14, 30);

    public AutonomyLevel AutonomyLevel { get; set; } = AutonomyLevel.A0_Advisory;

    public string? PolicyPackIdOverride { get; set; }
    public string? PolicyPackVersionOverride { get; set; }

    public bool TranslationReviewRequired { get; set; } = false;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
