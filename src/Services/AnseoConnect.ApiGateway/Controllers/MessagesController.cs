using AnseoConnect.ApiGateway.Models;
using AnseoConnect.Contracts.Commands;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize(Policy = "StaffOnly")]
public sealed class MessagesController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        AnseoConnectDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<MessagesController> logger)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MessageSummary>>> GetMessages(
        [FromQuery] string? channel,
        [FromQuery] string? status,
        [FromQuery] string? messageType,
        [FromQuery] DateTimeOffset? start,
        [FromQuery] DateTimeOffset? end,
        [FromQuery] bool? failedOnly,
        [FromQuery] Guid? studentId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var query = _dbContext.Messages.AsNoTracking();

        if (_tenantContext.TenantId != Guid.Empty)
        {
            query = query.Where(m => m.TenantId == _tenantContext.TenantId);
        }

        if (_tenantContext.SchoolId.HasValue)
        {
            query = query.Where(m => m.SchoolId == _tenantContext.SchoolId);
        }

        if (!string.IsNullOrWhiteSpace(channel))
        {
            query = query.Where(m => m.Channel == channel);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(m => m.Status == status);
        }

        if (failedOnly.GetValueOrDefault())
        {
            query = query.Where(m => m.Status == "FAILED" || m.Status == "BLOCKED");
        }

        if (!string.IsNullOrWhiteSpace(messageType))
        {
            query = query.Where(m => m.MessageType == messageType);
        }

        if (studentId.HasValue)
        {
            query = query.Where(m => m.StudentId == studentId.Value);
        }

        if (start.HasValue)
        {
            query = query.Where(m => m.CreatedAtUtc >= start.Value);
        }

        if (end.HasValue)
        {
            query = query.Where(m => m.CreatedAtUtc <= end.Value);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip(skip)
            .Take(Math.Min(take, 200))
            .Join(_dbContext.Students.AsNoTracking(),
                m => m.StudentId,
                s => s.StudentId,
                (m, s) => new
                {
                    m.MessageId,
                    m.StudentId,
                    StudentName = $"{s.FirstName} {s.LastName}".Trim(),
                    m.Channel,
                    m.Status,
                    m.MessageType,
                    m.CreatedAtUtc,
                    m.ProviderMessageId
                })
            .ToListAsync(ct);

        var summaries = items
            .Select(m => new MessageSummary(
                m.MessageId,
                m.StudentId,
                m.StudentName,
                m.Channel,
                m.Status,
                m.MessageType,
                m.CreatedAtUtc,
                m.ProviderMessageId,
                null))
            .ToList();

        return Ok(new PagedResult<MessageSummary>(summaries, total, skip, take, (skip + take) < total));
    }

    [HttpGet("{messageId:guid}")]
    public async Task<ActionResult<MessageDetail>> GetMessage(Guid messageId, CancellationToken ct = default)
    {
        var message = await _dbContext.Messages
            .AsNoTracking()
            .Include(m => m.Guardian)
            .Include(m => m.DeliveryAttempts)
            .FirstOrDefaultAsync(m => m.MessageId == messageId, ct);

        if (message == null)
        {
            return NotFound();
        }

        var student = await _dbContext.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == message.StudentId, ct);
        var studentName = student != null ? $"{student.FirstName} {student.LastName}".Trim() : "Unknown";

        var summary = new MessageSummary(
            message.MessageId,
            message.StudentId,
            studentName,
            message.Channel,
            message.Status,
            message.MessageType,
            message.CreatedAtUtc,
            message.ProviderMessageId,
            null);

        var timeline = message.DeliveryAttempts
            .OrderBy(a => a.AttemptedAtUtc)
            .Select(a => new MessageTimelineEvent(
                a.AttemptedAtUtc,
                a.Status,
                a.ErrorMessage))
            .ToList();

        var recipients = new List<string>();
        if (!string.IsNullOrWhiteSpace(message.Guardian?.MobileE164))
        {
            recipients.Add(message.Guardian.MobileE164!);
        }
        if (!string.IsNullOrWhiteSpace(message.Guardian?.Email))
        {
            recipients.Add(message.Guardian.Email!);
        }

        var detail = new MessageDetail(
            summary,
            message.Body,
            message.TemplateId ?? "Unknown",
            Array.Empty<KeyValuePair<string, string>>(),
            timeline,
            recipients,
            null,
            null);

        return Ok(detail);
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] MessageComposeRequest request, CancellationToken ct = default)
    {
        if (request.GuardianIds == null || request.GuardianIds.Count == 0)
        {
            return BadRequest(new { error = "At least one guardianId is required." });
        }

        var now = DateTimeOffset.UtcNow;
        var messages = new List<Message>();

        foreach (var guardianId in request.GuardianIds)
        {
            var message = new Message
            {
                MessageId = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                SchoolId = _tenantContext.SchoolId ?? Guid.Empty,
                StudentId = request.StudentId,
                GuardianId = guardianId,
                Channel = request.Channel,
                Status = "QUEUED",
                MessageType = request.Template,
                Body = request.BodyPreview,
                TemplateId = request.Template,
                CreatedAtUtc = now
            };
            messages.Add(message);
        }

        _dbContext.Messages.AddRange(messages);
        await _dbContext.SaveChangesAsync(ct);

        // Enqueue outbox command placeholders for future dispatcher
        var outboxItems = messages.Select(m => new OutboxMessage
        {
            OutboxMessageId = Guid.NewGuid(),
            TenantId = m.TenantId,
            SchoolId = _tenantContext.SchoolId,
            Type = "SEND_MESSAGE",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new SendMessageRequestedV1(
                CaseId: Guid.Empty,
                StudentId: m.StudentId,
                GuardianId: m.GuardianId,
                Channel: m.Channel,
                MessageType: m.MessageType,
                TemplateId: m.TemplateId ?? request.Template,
                TemplateData: new Dictionary<string, string> { ["Body"] = request.BodyPreview })),
            IdempotencyKey = m.MessageId.ToString(),
            Status = "PENDING",
            AttemptCount = 0,
            NextAttemptUtc = now,
            CreatedAtUtc = now
        });

        _dbContext.OutboxMessages.AddRange(outboxItems);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Queued {Count} messages for student {StudentId}", messages.Count, request.StudentId);
        return Accepted(new { count = messages.Count });
    }
}
