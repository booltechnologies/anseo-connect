using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Timeline;

/// <summary>
/// Projects WorkTask entities into timeline events.
/// </summary>
public sealed class TaskTimelineProjector : TimelineProjectorBase
{
    private readonly AnseoConnectDbContext _dbContext;
    
    public TaskTimelineProjector(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public override string SourceEntityType => "WorkTask";
    public override string Category => "TASK";
    
    public override async Task<IReadOnlyList<TimelineEvent>> ProjectAsync(object sourceEntity, CancellationToken cancellationToken = default)
    {
        if (sourceEntity is not WorkTask task)
        {
            return Array.Empty<TimelineEvent>();
        }
        
        // Get case to find student ID
        Guid? studentId = null;
        if (task.CaseId.HasValue)
        {
            var caseEntity = await _dbContext.Cases
                .AsNoTracking()
                .Where(c => c.CaseId == task.CaseId.Value)
                .Select(c => new { c.StudentId })
                .FirstOrDefaultAsync(cancellationToken);
                
            if (caseEntity != null)
            {
                studentId = caseEntity.StudentId;
            }
        }
        
        if (!studentId.HasValue)
        {
            return Array.Empty<TimelineEvent>();
        }
        
        var events = new List<TimelineEvent>();
        
        // Project task created event
        var createdEvt = CreateEvent(
            studentId.Value,
            task.CaseId,
            "TASK_CREATED",
            task.CreatedAtUtc,
            actorId: task.AssignedToUserId?.ToString(),
            title: $"Task: {task.Title}",
            summary: task.Notes,
            metadata: new { task.Status, task.DueAtUtc, task.AssignedToUserId }
        );
        SetSourceEntityId(createdEvt, task.WorkTaskId);
        events.Add(createdEvt);
        
        // Project task completed event if completed
        if (task.Status == "COMPLETED" && task.CompletedAtUtc.HasValue)
        {
            var completedEvt = CreateEvent(
                studentId.Value,
                task.CaseId,
                "TASK_COMPLETED",
                task.CompletedAtUtc.Value,
                actorId: task.AssignedToUserId?.ToString(),
                title: $"Task completed: {task.Title}",
                summary: task.Notes,
                metadata: new { task.Status }
            );
            SetSourceEntityId(completedEvt, task.WorkTaskId);
            events.Add(completedEvt);
        }
        
        return events;
    }
}
