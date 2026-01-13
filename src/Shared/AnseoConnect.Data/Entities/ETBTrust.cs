using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// ETB/Trust grouping for multi-school reporting.
/// </summary>
public sealed class ETBTrust : ITenantScoped
{
    public Guid ETBTrustId { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<School> Schools { get; set; } = new List<School>();
}
