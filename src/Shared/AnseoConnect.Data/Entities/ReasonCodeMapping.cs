namespace AnseoConnect.Data.Entities;

/// <summary>
/// Maps SIS provider-specific reason codes to normalized internal reason codes.
/// </summary>
public sealed class ReasonCodeMapping : SchoolEntity
{
    public Guid ReasonCodeMappingId { get; set; }

    /// <summary>
    /// SIS provider identifier (e.g., "WONDE", "TYRO").
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific reason code.
    /// </summary>
    public string ProviderCode { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific reason description (optional).
    /// </summary>
    public string? ProviderDescription { get; set; }

    /// <summary>
    /// Mapped internal reason code.
    /// </summary>
    public string InternalCode { get; set; } = string.Empty;

    /// <summary>
    /// Whether this mapping is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when this mapping was created or last updated.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
