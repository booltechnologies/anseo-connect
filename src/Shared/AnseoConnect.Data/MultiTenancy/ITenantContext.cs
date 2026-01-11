namespace AnseoConnect.Data.MultiTenancy;

public interface ITenantContext
{
    Guid TenantId { get; }
    Guid? SchoolId { get; }
}
