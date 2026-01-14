using AnseoConnect.ApiGateway.Models;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/admin/audit")]
[Authorize(Policy = "StaffOnly")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Searches audit events with filters.
    /// GET /api/admin/audit?actorId=...&action=...&entityType=...&fromUtc=...&toUtc=...&skip=0&take=50
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditEvent>>> SearchAuditEvents(
        [FromQuery] Guid? schoolId = null,
        [FromQuery] string? actorId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (take > 100)
        {
            take = 100; // Limit to 100 per page
        }

        var request = new AuditSearchRequest
        {
            SchoolId = schoolId,
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Skip = skip,
            Take = take
        };

        var (events, totalCount) = await _auditService.SearchAsync(request, cancellationToken);

        return Ok(new PagedResult<AuditEvent>(events, totalCount, skip, take, (skip + take) < totalCount));
    }
}
