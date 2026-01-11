using Microsoft.AspNetCore.Identity;
using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Local staff user account for non-SSO authentication.
/// Scoped to TenantId and SchoolId.
/// </summary>
public sealed class AppUser : IdentityUser<Guid>, ITenantScoped, ISchoolScoped
{
    public Guid TenantId { get; set; }
    public Guid SchoolId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
