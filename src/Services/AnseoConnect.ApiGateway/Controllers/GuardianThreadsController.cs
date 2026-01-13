using System.Security.Claims;
using AnseoConnect.ApiGateway.Services;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/guardian/threads")]
[Authorize(Roles = "Guardian")]
public sealed class GuardianThreadsController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly NotificationBroadcaster _broadcaster;

    public GuardianThreadsController(AnseoConnectDbContext dbContext, NotificationBroadcaster broadcaster)
    {
        _dbContext = dbContext;
        _broadcaster = broadcaster;
    }

    [HttpGet]
    public async Task<IActionResult> GetThreads(CancellationToken ct)
    {
        var guardianId = GetGuardianId();
        var threads = await _dbContext.Messages.AsNoTracking()
            .Where(m => m.GuardianId == guardianId)
            .GroupBy(m => m.ThreadId ?? Guid.Empty)
            .Select(g => new
            {
                ThreadId = g.Key,
                LastActivityUtc = g.Max(x => x.CreatedAtUtc),
                StudentId = g.Max(x => x.StudentId)
            })
            .OrderByDescending(x => x.LastActivityUtc)
            .ToListAsync(ct);

        return Ok(threads);
    }

    [HttpGet("{threadId:guid}/messages")]
    public async Task<IActionResult> GetThreadMessages(Guid threadId, CancellationToken ct)
    {
        var guardianId = GetGuardianId();
        var messages = await _dbContext.Messages.AsNoTracking()
            .Where(m => m.GuardianId == guardianId && (m.ThreadId == threadId || (m.ThreadId == null && threadId == Guid.Empty)))
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new
            {
                m.MessageId,
                m.Direction,
                m.Channel,
                m.Body,
                m.CreatedAtUtc,
                m.Status
            })
            .ToListAsync(ct);

        return Ok(messages);
    }

    [HttpPost("{threadId:guid}/reply")]
    public async Task<IActionResult> PostReply(Guid threadId, [FromBody] ReplyRequest request, CancellationToken ct)
    {
        var guardianId = GetGuardianId();
        var latest = await _dbContext.Messages
            .Where(m => m.ThreadId == threadId || (m.ThreadId == null && threadId == Guid.Empty))
            .OrderByDescending(m => m.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        var message = new Message
        {
            MessageId = Guid.NewGuid(),
            GuardianId = guardianId,
            StudentId = latest?.StudentId ?? Guid.Empty,
            TenantId = latest?.TenantId ?? Guid.Empty,
            SchoolId = latest?.SchoolId ?? Guid.Empty,
            ThreadId = threadId == Guid.Empty ? null : threadId,
            Direction = "INBOUND",
            Channel = "WHATSAPP",
            Body = request.Text,
            Status = "RECEIVED",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync(ct);

        await _broadcaster.BroadcastEngagementAsync(message.TenantId, new
        {
            guardianId,
            messageId = message.MessageId,
            eventType = "INBOUND"
        }, ct);

        return Accepted(new { message.MessageId });
    }

    private Guid GetGuardianId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(sub, out var id))
        {
            return id;
        }
        throw new UnauthorizedAccessException("Guardian id missing.");
    }

    public sealed record ReplyRequest(string Text);
}
