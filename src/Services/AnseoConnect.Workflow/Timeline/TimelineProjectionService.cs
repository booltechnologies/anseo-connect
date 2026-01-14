using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.Extensions.Logging;

namespace AnseoConnect.Workflow.Timeline;

/// <summary>
/// Service for projecting source entities into TimelineEvent records.
/// </summary>
public sealed class TimelineProjectionService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<TimelineProjectionService> _logger;
    
    // Cache of projectors by source entity type
    private readonly Dictionary<string, ITimelineProjector> _projectors = new();
    
    public TimelineProjectionService(
        AnseoConnectDbContext dbContext,
        ILogger<TimelineProjectionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        
        // Register all projectors
        RegisterProjectors();
    }
    
    private void RegisterProjectors()
    {
        _projectors["Message"] = new MessageTimelineProjector(_dbContext);
        _projectors["InterventionEvent"] = new InterventionTimelineProjector(_dbContext);
        _projectors["InterventionMeeting"] = new MeetingTimelineProjector(_dbContext);
        _projectors["WorkTask"] = new TaskTimelineProjector(_dbContext);
        _projectors["LetterArtifact"] = new LetterTimelineProjector(_dbContext);
        _projectors["EvidencePack"] = new EvidenceTimelineProjector();
        _projectors["TierAssignmentHistory"] = new TierTimelineProjector();
        _projectors["SafeguardingAlert"] = new SafeguardingTimelineProjector(_dbContext);
        _projectors["CaseTimelineEvent"] = new CaseEventTimelineProjector(_dbContext);
        _projectors["AttendanceDailySummary"] = new AttendanceTimelineProjector();
    }
    
    /// <summary>
    /// Projects a source entity into timeline events and saves them to the database.
    /// </summary>
    public async Task ProjectAsync(object sourceEntity, CancellationToken cancellationToken = default)
    {
        if (sourceEntity == null)
        {
            return;
        }
        
        var sourceType = sourceEntity.GetType().Name;
        if (!_projectors.TryGetValue(sourceType, out var projector))
        {
            _logger.LogDebug("No projector found for source entity type {SourceType}", sourceType);
            return;
        }
        
        try
        {
            var events = await projector.ProjectAsync(sourceEntity, cancellationToken);
            
            if (events.Count > 0)
            {
                // Set tenant/school context from source entity if it's a SchoolEntity
                if (sourceEntity is SchoolEntity schoolEntity)
                {
                    foreach (var evt in events)
                    {
                        evt.TenantId = schoolEntity.TenantId;
                        evt.SchoolId = schoolEntity.SchoolId;
                    }
                }
                
                _dbContext.TimelineEvents.AddRange(events);
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                _logger.LogDebug(
                    "Projected {EventCount} timeline events from {SourceType}",
                    events.Count,
                    sourceType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error projecting timeline events from {SourceType}", sourceType);
            throw;
        }
    }
    
    /// <summary>
    /// Projects multiple source entities in a batch.
    /// </summary>
    public async Task ProjectBatchAsync(IEnumerable<object> sourceEntities, CancellationToken cancellationToken = default)
    {
        var allEvents = new List<TimelineEvent>();
        
        foreach (var sourceEntity in sourceEntities)
        {
            if (sourceEntity == null)
            {
                continue;
            }
            
            var sourceType = sourceEntity.GetType().Name;
            if (!_projectors.TryGetValue(sourceType, out var projector))
            {
                continue;
            }
            
            try
            {
                var events = await projector.ProjectAsync(sourceEntity, cancellationToken);
                
                if (events.Count > 0)
                {
                    // Set tenant/school context from source entity if it's a SchoolEntity
                    if (sourceEntity is SchoolEntity schoolEntity)
                    {
                        foreach (var evt in events)
                        {
                            evt.TenantId = schoolEntity.TenantId;
                            evt.SchoolId = schoolEntity.SchoolId;
                        }
                    }
                    
                    allEvents.AddRange(events);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error projecting timeline events from {SourceType}", sourceType);
                // Continue with other entities
            }
        }
        
        if (allEvents.Count > 0)
        {
            _dbContext.TimelineEvents.AddRange(allEvents);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Projected {EventCount} timeline events from batch", allEvents.Count);
        }
    }
}
