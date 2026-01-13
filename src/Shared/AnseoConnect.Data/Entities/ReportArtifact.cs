using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Generated report artifact (PDF/XLSX) with snapshot hash.
/// </summary>
public sealed class ReportArtifact : SchoolEntity
{
    public Guid ArtifactId { get; set; }
    public Guid RunId { get; set; }
    public string Format { get; set; } = "PDF";
    public string StoragePath { get; set; } = string.Empty;
    public string DataSnapshotHash { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

