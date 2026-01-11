namespace AnseoConnect.Data.MultiTenancy;

/// <summary>
/// Per-request (or per-message) tenant context.
/// IMPORTANT: Set this before using AnseoConnectDbContext.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public Guid? SchoolId { get; private set; }

    public void Set(Guid tenantId, Guid? schoolId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("tenantId cannot be empty.");
        TenantId = tenantId;
        SchoolId = schoolId;
    }
}
