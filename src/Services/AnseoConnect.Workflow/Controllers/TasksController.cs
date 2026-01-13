using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnseoConnect.Workflow.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize(Policy = "StaffOnly")]
public sealed class TasksController : ControllerBase
{
    private readonly TaskService _taskService;

    public TasksController(TaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("due")]
    public async Task<IActionResult> GetDue([FromQuery] bool overdueOnly = false, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var (tasks, total) = await _taskService.GetTasksDueAsync(DateTimeOffset.UtcNow, overdueOnly, skip, take, ct);
        return Ok(new { items = tasks, total });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct = default)
    {
        var task = await _taskService.CreateTaskAsync(request.CaseId, request.Title, request.AssignedRole, request.DueAtUtc, request.ChecklistId, ct);
        return Ok(task);
    }

    [HttpPost("{taskId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid taskId, [FromBody] CompleteTaskRequest request, CancellationToken ct = default)
    {
        var ok = await _taskService.CompleteTaskAsync(taskId, request.Notes, ct);
        if (!ok) return NotFound();
        return Ok();
    }
}

public sealed record CreateTaskRequest(
    Guid? CaseId,
    string Title,
    string? ChecklistId,
    DateTimeOffset? DueAtUtc,
    Data.Entities.StaffRole? AssignedRole);

public sealed record CompleteTaskRequest(string? Notes);
