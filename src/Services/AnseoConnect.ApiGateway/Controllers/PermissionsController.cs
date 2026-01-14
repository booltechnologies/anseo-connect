using AnseoConnect.ApiGateway.Services;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/admin/permissions")]
[Authorize(Policy = "SettingsAdmin")]
public sealed class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(
        IPermissionService permissionService,
        AnseoConnectDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<PermissionsController> logger)
    {
        _permissionService = permissionService;
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets all system permissions.
    /// GET /api/admin/permissions
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllPermissions(CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionService.GetAllPermissionsAsync(cancellationToken);
        return Ok(permissions);
    }

    /// <summary>
    /// Gets all permissions for a role.
    /// GET /api/admin/permissions/roles/{roleName}
    /// </summary>
    [HttpGet("roles/{roleName}")]
    public async Task<IActionResult> GetRolePermissions(
        string roleName,
        [FromQuery] Guid? schoolId = null,
        CancellationToken cancellationToken = default)
    {
        var rolePermissions = await _permissionService.GetRolePermissionsAsync(roleName, schoolId, cancellationToken);
        return Ok(rolePermissions);
    }

    /// <summary>
    /// Grants a permission to a role.
    /// POST /api/admin/permissions/roles/{roleName}/permissions/{permissionId}
    /// </summary>
    [HttpPost("roles/{roleName}/permissions/{permissionId:guid}")]
    public async Task<IActionResult> GrantRolePermission(
        string roleName,
        Guid permissionId,
        [FromQuery] Guid? schoolId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = User.Identity?.Name ?? "system";

        var existing = await _dbContext.RolePermissions
            .FirstOrDefaultAsync(rp => rp.TenantId == tenantId &&
                                      rp.RoleName == roleName &&
                                      rp.PermissionId == permissionId &&
                                      rp.SchoolId == schoolId,
                                 cancellationToken);

        if (existing != null)
        {
            return Conflict(new { error = "Permission already granted to role" });
        }

        var rolePermission = new RolePermission
        {
            RolePermissionId = Guid.NewGuid(),
            TenantId = tenantId,
            RoleName = roleName,
            PermissionId = permissionId,
            SchoolId = schoolId,
            GrantedAtUtc = DateTimeOffset.UtcNow,
            GrantedBy = userId
        };

        _dbContext.RolePermissions.Add(rolePermission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetRolePermissions), new { roleName }, rolePermission);
    }

    /// <summary>
    /// Revokes a permission from a role.
    /// DELETE /api/admin/permissions/roles/{roleName}/permissions/{permissionId}
    /// </summary>
    [HttpDelete("roles/{roleName}/permissions/{permissionId:guid}")]
    public async Task<IActionResult> RevokeRolePermission(
        string roleName,
        Guid permissionId,
        [FromQuery] Guid? schoolId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;

        var rolePermission = await _dbContext.RolePermissions
            .FirstOrDefaultAsync(rp => rp.TenantId == tenantId &&
                                      rp.RoleName == roleName &&
                                      rp.PermissionId == permissionId &&
                                      rp.SchoolId == schoolId,
                                 cancellationToken);

        if (rolePermission == null)
        {
            return NotFound();
        }

        _dbContext.RolePermissions.Remove(rolePermission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
