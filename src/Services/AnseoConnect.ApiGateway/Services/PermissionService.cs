using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AnseoConnect.ApiGateway.Services;

/// <summary>
/// Service for querying and caching user effective permissions (role permissions + user overrides).
/// </summary>
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, Guid schoolId, string permissionCode, CancellationToken cancellationToken = default);
    Task<HashSet<string>> GetEffectivePermissionsAsync(Guid userId, Guid schoolId, CancellationToken cancellationToken = default);
    Task<List<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
    Task<List<RolePermission>> GetRolePermissionsAsync(string roleName, Guid? schoolId = null, CancellationToken cancellationToken = default);
    Task<List<UserPermissionOverride>> GetUserOverridesAsync(Guid userId, Guid schoolId, CancellationToken cancellationToken = default);
}

public sealed class PermissionService : IPermissionService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        AnseoConnectDbContext dbContext,
        UserManager<AppUser> userManager,
        ITenantContext tenantContext,
        IMemoryCache cache,
        ILogger<PermissionService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _tenantContext = tenantContext;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a user has a specific permission.
    /// </summary>
    public async Task<bool> HasPermissionAsync(Guid userId, Guid schoolId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var permissions = await GetEffectivePermissionsAsync(userId, schoolId, cancellationToken);
        return permissions.Contains(permissionCode);
    }

    /// <summary>
    /// Gets all effective permissions for a user (role permissions + user overrides).
    /// User overrides (deny) take precedence over role permissions.
    /// </summary>
    public async Task<HashSet<string>> GetEffectivePermissionsAsync(Guid userId, Guid schoolId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"permissions:user:{userId}:school:{schoolId}";
        if (_cache.TryGetValue(cacheKey, out HashSet<string>? cached))
        {
            return cached!;
        }

        // Get user roles
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return new HashSet<string>();
        }

        var roleNames = await _userManager.GetRolesAsync(user);
        var tenantId = _tenantContext.TenantId;

        // Get role permissions (tenant-wide and school-specific)
        var rolePermissions = await _dbContext.RolePermissions
            .AsNoTracking()
            .Include(rp => rp.Permission)
            .Where(rp => rp.TenantId == tenantId &&
                        roleNames.Contains(rp.RoleName) &&
                        (rp.SchoolId == null || rp.SchoolId == schoolId))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Get user overrides (deny takes precedence over grant)
        var userOverrides = await _dbContext.UserPermissionOverrides
            .AsNoTracking()
            .Include(upo => upo.Permission)
            .Where(upo => upo.TenantId == tenantId &&
                         upo.SchoolId == schoolId &&
                         upo.UserId == userId)
            .ToListAsync(cancellationToken);

        var effectivePermissions = new HashSet<string>(rolePermissions);

        // Apply user overrides (deny removes, grant adds)
        foreach (var override_ in userOverrides)
        {
            if (override_.IsGrant)
            {
                effectivePermissions.Add(override_.Permission.Code);
            }
            else
            {
                effectivePermissions.Remove(override_.Permission.Code);
            }
        }

        // Cache for 5 minutes
        _cache.Set(cacheKey, effectivePermissions, TimeSpan.FromMinutes(5));

        return effectivePermissions;
    }

    /// <summary>
    /// Gets all system permissions.
    /// </summary>
    public async Task<List<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        // Permission is NOT tenant-scoped, so it won't be filtered by tenant filters
        var permissions = await _dbContext.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Code)
            .ToListAsync(cancellationToken);

        return permissions;
    }

    /// <summary>
    /// Gets all permissions for a role (tenant-scoped).
    /// </summary>
    public async Task<List<RolePermission>> GetRolePermissionsAsync(string roleName, Guid? schoolId = null, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var query = _dbContext.RolePermissions
            .AsNoTracking()
            .Include(rp => rp.Permission)
            .Where(rp => rp.TenantId == tenantId && rp.RoleName == roleName);

        if (schoolId.HasValue)
        {
            query = query.Where(rp => rp.SchoolId == null || rp.SchoolId == schoolId);
        }
        else
        {
            query = query.Where(rp => rp.SchoolId == null); // Tenant-wide only
        }

        return await query
            .OrderBy(rp => rp.Permission.Category)
            .ThenBy(rp => rp.Permission.Code)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all user permission overrides for a user in a school.
    /// </summary>
    public async Task<List<UserPermissionOverride>> GetUserOverridesAsync(Guid userId, Guid schoolId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        return await _dbContext.UserPermissionOverrides
            .AsNoTracking()
            .Include(upo => upo.Permission)
            .Where(upo => upo.TenantId == tenantId &&
                         upo.SchoolId == schoolId &&
                         upo.UserId == userId)
            .OrderBy(upo => upo.Permission.Category)
            .ThenBy(upo => upo.Permission.Code)
            .ToListAsync(cancellationToken);
    }
}
