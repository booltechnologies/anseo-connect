using AnseoConnect.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;

    public NotificationsController(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var notifications = await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(100)
            .ToListAsync(ct);

        return Ok(notifications.Select(n => new NotificationDto(
            n.NotificationId,
            n.Type,
            n.Payload,
            n.CreatedAtUtc,
            n.ReadAtUtc)));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var notification = await _dbContext.Notifications
            .Where(n => n.NotificationId == id && n.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (notification == null)
        {
            return NotFound();
        }

        if (notification.ReadAtUtc == null)
        {
            notification.ReadAtUtc = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
        }

        return Ok();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(sub, out var parsed))
        {
            userId = parsed;
            return true;
        }

        userId = Guid.Empty;
        return false;
    }
}

public sealed record NotificationDto(
    Guid NotificationId,
    string Type,
    string Payload,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);
