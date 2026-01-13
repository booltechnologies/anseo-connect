using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Absence reason taxonomy entry (e.g., TUSLA codes).
/// </summary>
public sealed class ReasonCode : ITenantScoped
{
    public Guid ReasonCodeId { get; set; }
    public Guid TenantId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "OTHER"; // AUTHORISED / UNAUTHORISED / OTHER
    public string Scheme { get; set; } = "TUSLA_TESS";
    public string Version { get; set; } = "2026";

    /// <summary>
    /// Marks whether this is part of the default taxonomy (vs school override).
    /// </summary>
    public bool IsDefault { get; set; } = true;

    /// <summary>
    /// Source of the code: POLICY_PACK (default) or OVERRIDE.
    /// </summary>
    public string Source { get; set; } = "POLICY_PACK";

    /// <summary>
    /// When true, this record is an override/custom entry outside the policy pack.
    /// </summary>
    public bool IsOverride { get; set; } = false;
}
