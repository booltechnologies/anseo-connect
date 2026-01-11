using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Base class for entities that belong to a tenant and school.
/// </summary>
public abstract class SchoolEntity : ISchoolScoped
{
    public Guid TenantId { get; set; }
    public Guid SchoolId { get; set; }
}
