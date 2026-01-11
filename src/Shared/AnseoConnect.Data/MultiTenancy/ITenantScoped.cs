namespace AnseoConnect.Data.MultiTenancy;

public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
