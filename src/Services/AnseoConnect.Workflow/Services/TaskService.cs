using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Task lifecycle management for cases and operational tasks.
/// </summary>
public sealed class TaskService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<TaskService> _logger;

    public TaskService(AnseoConnectDbContext dbContext, ILogger<TaskService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<WorkTask> CreateTaskAsync(
        Guid? caseId,
        string title,
        StaffRole? assignedRole,
        DateTimeOffset? dueAtUtc,
        string? checklistId = null,
        CancellationToken cancellationToken = default)
    {
        var task = new WorkTask
        {
            CaseId = caseId,
            Title = title,
            AssignedRole = assignedRole,
            DueAtUtc = dueAtUtc,
            ChecklistId = checklistId
        };

        _dbContext.WorkTasks.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created task {TaskId} (case: {CaseId})", task.WorkTaskId, caseId);
        return task;
    }

    public async Task<bool> CompleteTaskAsync(Guid taskId, string? notes, CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.WorkTasks.FirstOrDefaultAsync(t => t.WorkTaskId == taskId, cancellationToken);
        if (task == null || task.Status == "COMPLETED")
        {
            return false;
        }

        task.Status = "COMPLETED";
        task.CompletedAtUtc = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            task.Notes = notes;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Completed task {TaskId}", taskId);
        return true;
    }

    public async Task<(IReadOnlyList<WorkTask> Tasks, int Total)> GetTasksDueAsync(
        DateTimeOffset asOfUtc,
        bool overdueOnly,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WorkTasks
            .AsNoTracking()
            .Where(t => t.Status == "OPEN");

        if (overdueOnly)
        {
            query = query.Where(t => t.DueAtUtc != null && t.DueAtUtc < asOfUtc);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(t => t.DueAtUtc ?? DateTimeOffset.MaxValue)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<WorkTask>> GetOverdueTasksAsync(DateTimeOffset asOfUtc, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkTasks
            .AsNoTracking()
            .Where(t => t.Status == "OPEN" && t.DueAtUtc != null && t.DueAtUtc < asOfUtc)
            .OrderBy(t => t.DueAtUtc)
            .ToListAsync(cancellationToken);
    }
}
