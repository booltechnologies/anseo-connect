using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Aggregated reachability metrics per guardian and channel.
/// </summary>
public sealed class GuardianReachability : SchoolEntity
{
    public Guid ReachabilityId { get; set; }
    public Guid GuardianId { get; set; }
    public string Channel { get; set; } = "SMS";
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalFailed { get; set; }
    public int TotalReplied { get; set; }
    public decimal ReachabilityScore { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
