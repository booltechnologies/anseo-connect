using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Generated letter artifacts with integrity hash and snapshot metadata.
/// </summary>
public sealed class LetterArtifact : SchoolEntity
{
    public Guid ArtifactId { get; set; }
    public Guid InstanceId { get; set; }
    public Guid StageId { get; set; }
    public Guid TemplateId { get; set; }
    public int TemplateVersion { get; set; }
    public Guid GuardianId { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string StoragePath { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string MergeDataJson { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

