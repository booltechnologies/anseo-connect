namespace AnseoConnect.Data.MultiTenancy;

public interface ITenantContext
{
    Guid TenantId { get; }
    Guid? SchoolId { get; }

    void Set(Guid tenantId, Guid? schoolId);
}
