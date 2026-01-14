using AnseoConnect.Contracts.DTOs;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Service for querying and managing timeline events for students.
/// </summary>
public sealed class TimelineService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<TimelineService> _logger;
    private readonly ITenantContext _tenantContext;
    
    public TimelineService(
        AnseoConnectDbContext dbContext,
        ILogger<TimelineService> logger,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _logger = logger;
        _tenantContext = tenantContext;
    }
    
    /// <summary>
    /// Gets paginated timeline events for a student with filters.
    /// </summary>
    public async Task<(IReadOnlyList<TimelineEventDto> Events, int TotalCount)> GetStudentTimelineAsync(
        Guid studentId,
        TimelineFilter filter,
        bool includeSafeguarding = false,
        bool includeAdminOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TimelineEvents
            .AsNoTracking()
            .Where(e => e.StudentId == studentId);
        
        // Apply filters
        if (filter.FromUtc.HasValue)
        {
            query = query.Where(e => e.OccurredAtUtc >= filter.FromUtc.Value);
        }
        
        if (filter.ToUtc.HasValue)
        {
            query = query.Where(e => e.OccurredAtUtc <= filter.ToUtc.Value);
        }
        
        if (filter.Categories != null && filter.Categories.Count > 0)
        {
            query = query.Where(e => filter.Categories.Contains(e.Category));
        }
        
        if (filter.EventTypes != null && filter.EventTypes.Count > 0)
        {
            query = query.Where(e => filter.EventTypes.Contains(e.EventType));
        }
        
        if (filter.CaseId.HasValue)
        {
            query = query.Where(e => e.CaseId == filter.CaseId.Value);
        }
        
        // Apply visibility scope filter based on permissions
        var visibilityScopes = new List<string> { "STANDARD" };
        if (includeSafeguarding)
        {
            visibilityScopes.Add("SAFEGUARDING");
        }
        if (includeAdminOnly)
        {
            visibilityScopes.Add("ADMIN_ONLY");
        }
        query = query.Where(e => visibilityScopes.Contains(e.VisibilityScope));
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var events = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip(filter.Skip)
            .Take(filter.Take)
            .Select(e => new TimelineEventDto(
                e.EventId,
                e.StudentId,
                e.CaseId,
                e.EventType,
                e.Category,
                e.OccurredAtUtc,
                e.ActorName,
                e.Title,
                e.Summary,
                e.MetadataJson,
                e.VisibilityScope))
            .ToListAsync(cancellationToken);
        
        return (events, totalCount);
    }
    
    /// <summary>
    /// Searches timeline events for a student using full-text search.
    /// </summary>
    public async Task<(IReadOnlyList<TimelineEventDto> Events, int TotalCount)> SearchTimelineAsync(
        Guid studentId,
        string searchTerm,
        int skip = 0,
        int take = 50,
        bool includeSafeguarding = false,
        bool includeAdminOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return (Array.Empty<TimelineEventDto>(), 0);
        }
        
        var query = _dbContext.TimelineEvents
            .AsNoTracking()
            .Where(e => e.StudentId == studentId);
        
        // Apply visibility scope filter based on permissions
        var visibilityScopes = new List<string> { "STANDARD" };
        if (includeSafeguarding)
        {
            visibilityScopes.Add("SAFEGUARDING");
        }
        if (includeAdminOnly)
        {
            visibilityScopes.Add("ADMIN_ONLY");
        }
        query = query.Where(e => visibilityScopes.Contains(e.VisibilityScope));
        
        // Full-text search on SearchableText field
        // Note: For SQL Server, this would use CONTAINS, but for now we'll use EF.Functions.Like
        var searchLower = searchTerm.ToLower();
        query = query.Where(e => 
            (e.SearchableText != null && EF.Functions.Like(e.SearchableText.ToLower(), $"%{searchLower}%")) ||
            (e.Title != null && EF.Functions.Like(e.Title.ToLower(), $"%{searchLower}%")) ||
            (e.Summary != null && EF.Functions.Like(e.Summary.ToLower(), $"%{searchLower}%")) ||
            EF.Functions.Like(e.EventType.ToLower(), $"%{searchLower}%"));
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var events = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(e => new TimelineEventDto(
                e.EventId,
                e.StudentId,
                e.CaseId,
                e.EventType,
                e.Category,
                e.OccurredAtUtc,
                e.ActorName,
                e.Title,
                e.Summary,
                e.MetadataJson,
                e.VisibilityScope))
            .ToListAsync(cancellationToken);
        
        return (events, totalCount);
    }
    
    /// <summary>
    /// Exports timeline events as a stream (placeholder - full implementation would generate PDF/Excel).
    /// </summary>
    public async Task<Stream> ExportTimelineAsync(
        Guid studentId,
        ExportOptions options,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement PDF/Excel export
        // For now, return empty stream
        _logger.LogWarning("Timeline export not yet implemented");
        return new MemoryStream();
    }
}
