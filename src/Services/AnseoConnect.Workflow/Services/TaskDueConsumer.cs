using AnseoConnect.Contracts.Common;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Shared;
using Microsoft.Extensions.Hosting;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Periodically checks for overdue tasks and publishes notifications.
/// Simplified timer-based implementation for v0.1.
/// </summary>
public sealed class TaskDueConsumer : BackgroundService
{
    private readonly TaskService _taskService;
    private readonly IMessageBus _messageBus;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TaskDueConsumer> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public TaskDueConsumer(
        TaskService taskService,
        IMessageBus messageBus,
        ITenantContext tenantContext,
        ILogger<TaskDueConsumer> logger)
    {
        _taskService = taskService;
        _messageBus = messageBus;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var overdue = await _taskService.GetOverdueTasksAsync(now, stoppingToken);
                foreach (var task in overdue)
                {
                    var envelope = new MessageEnvelope<object>(
                        MessageType: "TaskOverdue",
                        Version: "v1",
                        TenantId: _tenantContext.TenantId,
                        SchoolId: _tenantContext.SchoolId ?? Guid.Empty,
                        CorrelationId: Guid.NewGuid().ToString(),
                        OccurredAtUtc: DateTimeOffset.UtcNow,
                        Payload: new
                        {
                            TaskId = task.WorkTaskId,
                            task.Title,
                            task.CaseId,
                            task.DueAtUtc
                        });

                    await _messageBus.PublishAsync(envelope, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking overdue tasks");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
