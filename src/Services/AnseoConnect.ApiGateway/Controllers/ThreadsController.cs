using AnseoConnect.Contracts.Commands;
using AnseoConnect.Contracts.Common;
using AnseoConnect.Data;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/threads")]
[Authorize(Policy = "StaffOnly")]
public sealed class ThreadsController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly IMessageBus _messageBus;
    private readonly ITenantContext _tenantContext;

    public ThreadsController(AnseoConnectDbContext dbContext, IMessageBus messageBus, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _messageBus = messageBus;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetThreads(CancellationToken ct)
    {
        var threads = await _dbContext.Messages.AsNoTracking()
            .GroupBy(m => m.ThreadId ?? Guid.Empty)
            .Select(g => new
            {
                ThreadId = g.Key,
                LastActivityUtc = g.Max(x => x.CreatedAtUtc),
                StudentId = g.Max(x => x.StudentId),
                GuardianId = g.Max(x => x.GuardianId)
            })
            .OrderByDescending(x => x.LastActivityUtc)
            .ToListAsync(ct);
        return Ok(threads);
    }

    [HttpGet("{threadId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid threadId, CancellationToken ct)
    {
        var messages = await _dbContext.Messages.AsNoTracking()
            .Where(m => m.ThreadId == threadId || (m.ThreadId == null && threadId == Guid.Empty))
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(ct);
        return Ok(messages);
    }

    [HttpPost("{threadId:guid}/reply")]
    public async Task<IActionResult> SendMessage(Guid threadId, [FromBody] SendRequest request, CancellationToken ct)
    {
        if (_messageBus == null)
        {
            return StatusCode(500, "Message bus not configured");
        }

        var envelope = new MessageEnvelope<SendMessageRequestedV1>(
            MessageType: MessageTypes.SendMessageRequestedV1,
            Version: MessageVersions.V1,
            TenantId: _tenantContext.TenantId,
            SchoolId: _tenantContext.SchoolId ?? Guid.Empty,
            CorrelationId: Guid.NewGuid().ToString(),
            OccurredAtUtc: DateTimeOffset.UtcNow,
            Payload: new SendMessageRequestedV1(
                CaseId: Guid.Empty,
                StudentId: request.StudentId,
                GuardianId: request.GuardianId,
                Channel: request.Channel,
                MessageType: "MANUAL",
                TemplateId: request.TemplateId ?? "CUSTOM",
                TemplateData: request.TemplateData ?? new Dictionary<string, string>()));

        await _messageBus.PublishAsync(envelope, ct);
        return Accepted();
    }

    public sealed record SendRequest(Guid GuardianId, Guid StudentId, string Channel, string? TemplateId, Dictionary<string, string>? TemplateData);
}
