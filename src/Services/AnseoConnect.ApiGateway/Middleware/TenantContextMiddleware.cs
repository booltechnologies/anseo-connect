using AnseoConnect.Data.MultiTenancy;
using System.Security.Claims;

namespace AnseoConnect.ApiGateway.Middleware;

/// <summary>
/// Middleware that sets TenantContext from JWT claims.
/// </summary>
public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
            var schoolIdClaim = context.User.FindFirst("school_id")?.Value;

            if (Guid.TryParse(tenantIdClaim, out var tenantId) && tenantId != Guid.Empty)
            {
                Guid? schoolId = null;
                if (Guid.TryParse(schoolIdClaim, out var parsedSchoolId))
                {
                    schoolId = parsedSchoolId;
                }

                if (tenantContext is TenantContext tc)
                {
                    tc.Set(tenantId, schoolId);
                }
            }
        }

        await _next(context);
    }
}
