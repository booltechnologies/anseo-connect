using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// User-level permission override (grant or deny) that takes precedence over role permissions.
/// </summary>
public sealed class UserPermissionOverride : ITenantScoped, ISchoolScoped
{
    public Guid OverrideId { get; set; }
    public Guid TenantId { get; set; }
    public Guid SchoolId { get; set; }
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public bool IsGrant { get; set; } // true = grant, false = deny
    public DateTimeOffset ModifiedAtUtc { get; set; }
    public string ModifiedBy { get; set; } = ""; // User ID
    public string Reason { get; set; } = ""; // Optional reason for override
}
