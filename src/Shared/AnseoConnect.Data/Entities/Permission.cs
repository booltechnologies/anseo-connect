namespace AnseoConnect.Data.Entities;

/// <summary>
/// System permission definition. Permissions are global and not tenant-scoped.
/// </summary>
public sealed class Permission
{
    public Guid PermissionId { get; set; }
    public string Code { get; set; } = ""; // e.g., "students:view"
    public string DisplayName { get; set; } = "";
    public string Category { get; set; } = ""; // e.g., "Students"
    public string Description { get; set; } = "";
    public bool IsSystemPermission { get; set; } = true; // Cannot be deleted if true
}
