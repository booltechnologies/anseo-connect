using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Grants a permission to a role for a tenant (optionally scoped to a school).
/// </summary>
public sealed class RolePermission : ITenantScoped
{
    public Guid RolePermissionId { get; set; }
    public Guid TenantId { get; set; }
    public string RoleName { get; set; } = ""; // Maps to IdentityRole.Name
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public Guid? SchoolId { get; set; } // null = all schools in tenant, otherwise school-scoped
    public DateTimeOffset GrantedAtUtc { get; set; }
    public string GrantedBy { get; set; } = ""; // User ID or "system"
}
