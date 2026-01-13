using System.Text.Json;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/segments")]
[Authorize(Policy = "StaffOnly")]
public sealed class SegmentsController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;

    public SegmentsController(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var segments = await _dbContext.AudienceSegments.AsNoTracking()
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(ct);
        return Ok(segments);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSegment request, CancellationToken ct)
    {
        var segment = new AudienceSegment
        {
            SegmentId = Guid.NewGuid(),
            TenantId = request.TenantId,
            Name = request.Name,
            FilterDefinitionJson = request.FilterDefinitionJson,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _dbContext.AudienceSegments.Add(segment);
        await _dbContext.SaveChangesAsync(ct);
        return Ok(segment);
    }

    [HttpGet("{segmentId:guid}/preview")]
    public async Task<IActionResult> Preview(Guid segmentId, CancellationToken ct)
    {
        var segment = await _dbContext.AudienceSegments.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SegmentId == segmentId, ct);
        if (segment == null)
        {
            return NotFound();
        }

        var filter = ParseFilter(segment.FilterDefinitionJson);
        var query =
            from sg in _dbContext.StudentGuardians.AsNoTracking()
            join g in _dbContext.Guardians.AsNoTracking() on sg.GuardianId equals g.GuardianId
            join s in _dbContext.Students.AsNoTracking() on sg.StudentId equals s.StudentId
            where g.IsActive && s.IsActive
            select new { sg, g, s };

        if (filter.SchoolIds.Count > 0)
        {
            query = query.Where(x => filter.SchoolIds.Contains(x.s.SchoolId));
        }
        if (filter.YearGroups.Count > 0)
        {
            query = query.Where(x => x.s.YearGroup != null && filter.YearGroups.Contains(x.s.YearGroup));
        }

        var recipients = await query
            .Select(x => new { x.g.GuardianId, x.s.StudentId })
            .Distinct()
            .Take(200)
            .ToListAsync(ct);

        return Ok(new
        {
            count = recipients.Count,
            sample = recipients.Take(20)
        });
    }

    public sealed record CreateSegment(Guid TenantId, string Name, string FilterDefinitionJson, Guid CreatedByUserId);

    private static SegmentFilter ParseFilter(string? json)
    {
        var filter = new SegmentFilter();
        if (string.IsNullOrWhiteSpace(json))
        {
            return filter;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("schoolIds", out var schools) && schools.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in schools.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var id))
                    {
                        filter.SchoolIds.Add(id);
                    }
                }
            }
            if (root.TryGetProperty("yearGroups", out var years) && years.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in years.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(el.GetString()))
                    {
                        filter.YearGroups.Add(el.GetString()!);
                    }
                }
            }
        }
        catch
        {
            // ignore malformed filter
        }

        return filter;
    }

    private sealed class SegmentFilter
    {
        public List<Guid> SchoolIds { get; } = new();
        public List<string> YearGroups { get; } = new();
    }
}
