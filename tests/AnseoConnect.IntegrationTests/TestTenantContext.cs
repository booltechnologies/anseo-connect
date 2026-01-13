using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.IntegrationTests;

/// <summary>
/// Simple ITenantContext for tests without empty-guid validation.
/// </summary>
public sealed class TestTenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public Guid? SchoolId { get; private set; }

    public void Set(Guid tenantId, Guid? schoolId)
    {
        TenantId = tenantId;
        SchoolId = schoolId;
    }
}
