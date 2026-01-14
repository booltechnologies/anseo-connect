using AnseoConnect.Client.Models;
using AnseoConnect.Data.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class PermissionsClient : ApiClientBase
{
    public PermissionsClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<PermissionsClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public async Task<List<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<List<Permission>>("api/admin/permissions", cancellationToken);
        return result ?? new List<Permission>();
    }

    public async Task<List<RolePermission>> GetRolePermissionsAsync(
        string roleName,
        Guid? schoolId = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/admin/permissions/roles/{Uri.EscapeDataString(roleName)}";
        if (schoolId.HasValue)
        {
            endpoint += $"?schoolId={schoolId.Value}";
        }

        var result = await GetOrDefaultAsync<List<RolePermission>>(endpoint, cancellationToken);
        return result ?? new List<RolePermission>();
    }

    public async Task<RolePermission?> GrantRolePermissionAsync(
        string roleName,
        Guid permissionId,
        Guid? schoolId = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/admin/permissions/roles/{Uri.EscapeDataString(roleName)}/permissions/{permissionId}";
        if (schoolId.HasValue)
        {
            endpoint += $"?schoolId={schoolId.Value}";
        }

        return await PostOrDefaultAsync<object?, RolePermission>(endpoint, null, cancellationToken);
    }

    public async Task<bool> RevokeRolePermissionAsync(
        string roleName,
        Guid permissionId,
        Guid? schoolId = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/admin/permissions/roles/{Uri.EscapeDataString(roleName)}/permissions/{permissionId}";
        if (schoolId.HasValue)
        {
            endpoint += $"?schoolId={schoolId.Value}";
        }

        return await DeleteOrFalseAsync(endpoint, cancellationToken);
    }
}
