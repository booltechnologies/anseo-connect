using System.Text.Json;
using AnseoConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Comms.Services;

/// <summary>
/// Resolves audience segments into recipient guardian IDs (stub implementation).
/// </summary>
public sealed class SegmentQueryEngine
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<SegmentQueryEngine> _logger;

    public SegmentQueryEngine(AnseoConnectDbContext dbContext, ILogger<SegmentQueryEngine> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SegmentRecipient>> ResolveRecipientsAsync(Guid segmentId, CancellationToken ct)
    {
        var segment = await _dbContext.AudienceSegments.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SegmentId == segmentId, ct);
        if (segment == null)
        {
            _logger.LogWarning("Segment {SegmentId} not found", segmentId);
            return Array.Empty<SegmentRecipient>();
        }

        var filter = ParseFilter(segment.FilterDefinitionJson);

        var query =
            from sg in _dbContext.StudentGuardians.AsNoTracking()
            join g in _dbContext.Guardians.AsNoTracking() on sg.GuardianId equals g.GuardianId
            join s in _dbContext.Students.AsNoTracking() on sg.StudentId equals s.StudentId
            join c in _dbContext.Cases.AsNoTracking() on s.StudentId equals c.StudentId into caseJoin
            from c in caseJoin.DefaultIfEmpty()
            where g.IsActive && s.IsActive
            select new { sg, g, s, c };

        if (filter.SchoolIds.Count > 0)
        {
            query = query.Where(x => filter.SchoolIds.Contains(x.s.SchoolId));
        }
        if (filter.YearGroups.Count > 0)
        {
            query = query.Where(x => x.s.YearGroup != null && filter.YearGroups.Contains(x.s.YearGroup));
        }
        if (!string.IsNullOrWhiteSpace(filter.CaseStatus))
        {
            query = query.Where(x => x.c != null && x.c.Status == filter.CaseStatus);
        }
        if (!string.IsNullOrWhiteSpace(filter.CaseType))
        {
            query = query.Where(x => x.c != null && x.c.CaseType == filter.CaseType);
        }
        if (filter.Tags.Count > 0)
        {
            query = query.Where(x => x.c != null && x.c.BarrierCodes != null && filter.Tags.Any(tag => x.c.BarrierCodes!.Contains(tag)));
        }

        var results = await query
            .Select(x => new SegmentRecipient(x.g.GuardianId, x.s.StudentId))
            .Distinct()
            .ToListAsync(ct);

        _logger.LogInformation("Segment {SegmentId} resolved {Count} recipients", segmentId, results.Count);
        return results;
    }

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
            if (root.TryGetProperty("caseStatus", out var cs) && cs.ValueKind == JsonValueKind.String)
            {
                filter.CaseStatus = cs.GetString();
            }
            if (root.TryGetProperty("caseType", out var ctProp) && ctProp.ValueKind == JsonValueKind.String)
            {
                filter.CaseType = ctProp.GetString();
            }
            if (root.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in tags.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(el.GetString()))
                    {
                        filter.Tags.Add(el.GetString()!);
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
}

public sealed record SegmentRecipient(Guid GuardianId, Guid StudentId);

internal sealed class SegmentFilter
{
    public List<Guid> SchoolIds { get; } = new();
    public List<string> YearGroups { get; } = new();
    public string? CaseStatus { get; set; }
    public string? CaseType { get; set; }
    public List<string> Tags { get; } = new();
}
