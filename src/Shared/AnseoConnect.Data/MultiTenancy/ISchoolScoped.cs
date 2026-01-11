namespace AnseoConnect.Data.MultiTenancy;

public interface ISchoolScoped : ITenantScoped
{
    Guid SchoolId { get; set; }
}
