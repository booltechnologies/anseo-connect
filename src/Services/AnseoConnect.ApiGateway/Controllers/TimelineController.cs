using AnseoConnect.ApiGateway.Models;
using AnseoConnect.Contracts.DTOs;
using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/students/{studentId:guid}/timeline")]
[Authorize(Policy = "StaffOnly")]
public sealed class TimelineController : ControllerBase
{
    private readonly TimelineService _timelineService;
    private readonly IAuthorizationService _authorizationService;
    
    public TimelineController(
        TimelineService timelineService,
        IAuthorizationService authorizationService)
    {
        _timelineService = timelineService;
        _authorizationService = authorizationService;
    }
    
    [HttpGet]
    public async Task<ActionResult<PagedResult<TimelineEventDto>>> GetTimeline(
        Guid studentId,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        [FromQuery] string[]? categories = null,
        [FromQuery] string[]? eventTypes = null,
        [FromQuery] Guid? caseId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var filter = new TimelineFilter(
            fromUtc,
            toUtc,
            categories,
            eventTypes,
            caseId,
            skip,
            take);
        
        // Check permissions for safeguarding and admin-only events
        var canViewSafeguarding = (await _authorizationService.AuthorizeAsync(User, "SafeguardingAccess")).Succeeded;
        var canViewAdminOnly = (await _authorizationService.AuthorizeAsync(User, "TierManagement")).Succeeded; // Use TierManagement as proxy for admin access
            
        var (events, totalCount) = await _timelineService.GetStudentTimelineAsync(
            studentId, 
            filter, 
            canViewSafeguarding, 
            canViewAdminOnly, 
            ct);
        
        return Ok(new PagedResult<TimelineEventDto>(
            events,
            totalCount,
            skip,
            take,
            skip + take < totalCount));
    }
    
    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<TimelineEventDto>>> SearchTimeline(
        Guid studentId,
        [FromQuery] string q,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        // Check permissions for safeguarding and admin-only events
        var canViewSafeguarding = (await _authorizationService.AuthorizeAsync(User, "SafeguardingAccess")).Succeeded;
        var canViewAdminOnly = (await _authorizationService.AuthorizeAsync(User, "TierManagement")).Succeeded;
        
        var (events, totalCount) = await _timelineService.SearchTimelineAsync(
            studentId, 
            q, 
            skip, 
            take, 
            canViewSafeguarding, 
            canViewAdminOnly, 
            ct);
        
        return Ok(new PagedResult<TimelineEventDto>(
            events,
            totalCount,
            skip,
            take,
            skip + take < totalCount));
    }
    
    [HttpGet("export")]
    public async Task<IActionResult> ExportTimeline(
        Guid studentId,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        [FromQuery] string[]? categories = null,
        [FromQuery] bool includeRedacted = false,
        [FromQuery] string format = "PDF",
        CancellationToken ct = default)
    {
        var options = new ExportOptions(fromUtc, toUtc, categories, includeRedacted, format);
        var stream = await _timelineService.ExportTimelineAsync(studentId, options, ct);
        
        return File(stream, format == "PDF" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            $"timeline-{studentId}-{DateTimeOffset.UtcNow:yyyyMMdd}.{format.ToLower()}");
    }
}
