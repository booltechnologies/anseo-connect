using System.Text.Json;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Routes events to configured recipients and stores in-app notifications.
/// </summary>
public sealed class NotificationRoutingService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<NotificationRoutingService> _logger;

    public NotificationRoutingService(
        AnseoConnectDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<NotificationRoutingService> logger)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Routes a payload to recipients defined for a route, creating Notification records.
    /// </summary>
    public async Task RouteAsync(string route, string type, object payload, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var schoolId = _tenantContext.SchoolId ?? Guid.Empty;

        var recipients = await _dbContext.NotificationRecipients
            .AsNoTracking()
            .Where(r => r.Route == route && r.TenantId == tenantId && r.SchoolId == schoolId)
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

        if (!recipients.Any())
        {
            _logger.LogWarning("No notification recipients configured for route {Route}", route);
            return;
        }

        var userIds = new HashSet<Guid>();

        foreach (var recipient in recipients)
        {
            if (recipient.UserId.HasValue)
            {
                userIds.Add(recipient.UserId.Value);
            }

            if (recipient.Role.HasValue)
            {
                var roleName = recipient.Role.Value.ToString();
                var roleId = await _dbContext.Roles
                    .Where(r => r.Name == roleName)
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (roleId != Guid.Empty)
                {
                    var roleUsers = await _dbContext.Users
                        .AsNoTracking()
                        .Where(u => u.TenantId == tenantId && u.SchoolId == schoolId && u.IsActive)
                        .Join(_dbContext.UserRoles,
                            u => u.Id,
                            ur => ur.UserId,
                            (u, ur) => new { u, ur })
                        .Where(x => x.ur.RoleId == roleId)
                        .Select(x => x.u.Id)
                        .ToListAsync(cancellationToken);

                    foreach (var uid in roleUsers)
                    {
                        userIds.Add(uid);
                    }
                }

            }
        }

        if (!userIds.Any())
        {
            _logger.LogWarning("Notification routing resolved zero users for route {Route}", route);
            return;
        }

        var payloadJson = JsonSerializer.Serialize(payload);
        var now = DateTimeOffset.UtcNow;

        foreach (var userId in userIds)
        {
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                TenantId = tenantId,
                SchoolId = schoolId,
                UserId = userId,
                Type = type,
                Payload = payloadJson,
                CreatedAtUtc = now
            };

            _dbContext.Notifications.Add(notification);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created {Count} notifications for route {Route}", userIds.Count, route);
    }
}
