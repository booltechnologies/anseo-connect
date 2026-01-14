using AnseoConnect.ApiGateway.Services;
using AnseoConnect.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace AnseoConnect.ApiGateway.Authorization;

/// <summary>
/// Authorization requirement for permission-based access control.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionCode { get; }

    public PermissionRequirement(string permissionCode)
    {
        PermissionCode = permissionCode ?? throw new ArgumentNullException(nameof(permissionCode));
    }
}

/// <summary>
/// Authorization handler that checks if a user has a specific permission.
/// </summary>
public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionHandler> _logger;

    public PermissionHandler(
        IPermissionService permissionService,
        ILogger<PermissionHandler> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("Permission check failed: user not authenticated");
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier) 
            ?? context.User.FindFirst("sub")
            ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("Permission check failed: user ID not found in claims");
            return;
        }

        // Get school ID from claims or context
        var schoolIdClaim = context.User.FindFirst("school_id");
        if (schoolIdClaim == null || !Guid.TryParse(schoolIdClaim.Value, out var schoolId))
        {
            _logger.LogWarning("Permission check failed: school ID not found in claims");
            return;
        }

        try
        {
            // Check if user has the required permission
            var hasPermission = await _permissionService.HasPermissionAsync(
                userId,
                schoolId,
                requirement.PermissionCode,
                CancellationToken.None);

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation(
                    "Permission check failed: user {UserId} does not have permission {PermissionCode}",
                    userId,
                    requirement.PermissionCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {PermissionCode} for user {UserId}", requirement.PermissionCode, userId);
            // Fail open? Or fail closed? For security, we'll fail closed.
        }
    }
}
