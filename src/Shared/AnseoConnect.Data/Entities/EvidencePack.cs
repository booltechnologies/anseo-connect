namespace AnseoConnect.Data.Entities;

/// <summary>
/// Generated evidence pack for a case (e.g., PDF/JSON export).
/// </summary>
public sealed class EvidencePack : SchoolEntity
{
    public Guid EvidencePackId { get; set; }
    public Guid CaseId { get; set; }
    public Guid StudentId { get; set; }

    // Scope configuration
    public DateOnly DateRangeStart { get; set; }
    public DateOnly DateRangeEnd { get; set; }
    public string IncludedSectionsJson { get; set; } = "[]"; // ["ATTENDANCE", "COMMUNICATIONS", "LETTERS", "MEETINGS", "TASKS"]

    // Outputs
    public string Format { get; set; } = "PDF"; // "PDF", "PDF_WITH_ZIP"
    public string StoragePath { get; set; } = string.Empty; // PDF blob path
    public string? ZipStoragePath { get; set; } // ZIP blob path (raw artifacts)
    public string IndexJson { get; set; } = "{}"; // table of contents with page/artifact references

    // Integrity
    public string ContentHash { get; set; } = string.Empty; // SHA-256 of the PDF
    public string ManifestHash { get; set; } = string.Empty; // SHA-256 of the manifest/index

    // Audit
    public Guid GeneratedByUserId { get; set; }
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string GenerationPurpose { get; set; } = string.Empty; // INSPECTION, AGENCY_REFERRAL, PARENT_REQUEST, ARCHIVE

    public Case? Case { get; set; }
}
