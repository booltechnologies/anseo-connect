using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Services;

public sealed class PlaybookEvaluator
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<PlaybookEvaluator> _logger;

    public PlaybookEvaluator(AnseoConnectDbContext dbContext, ILogger<PlaybookEvaluator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<StopConditionResult> EvaluateStopConditionsAsync(PlaybookRun run, CancellationToken cancellationToken = default)
    {
        // 1) Case closed
        var instance = await _dbContext.StudentInterventionInstances
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InstanceId == run.InstanceId, cancellationToken);

        if (instance?.CaseId != null && instance.CaseId != Guid.Empty)
        {
            var closed = await _dbContext.Cases
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.CaseId == instance.CaseId && c.TenantId == run.TenantId)
                .Select(c => c.Status)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(closed) && closed.Equals("CLOSED", StringComparison.OrdinalIgnoreCase))
            {
                return new StopConditionResult(true, "CASE_CLOSED");
            }
        }

        // 2) Guardian replied
        if (run.GuardianId.HasValue)
        {
            var replied = await _dbContext.EngagementEvents
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(e => e.GuardianId == run.GuardianId.Value &&
                            e.TenantId == run.TenantId &&
                            e.EventType == "REPLIED" &&
                            e.OccurredAtUtc >= run.TriggeredAtUtc)
                .AnyAsync(cancellationToken);

            if (replied)
            {
                return new StopConditionResult(true, "GUARDIAN_REPLIED");
            }
        }

        // 3) Attendance improved (simple heuristic >= 90%)
        var latestSummary = await _dbContext.AttendanceDailySummaries
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.StudentId == run.StudentId && s.TenantId == run.TenantId)
            .OrderByDescending(s => s.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSummary?.AttendancePercent >= 90m)
        {
            return new StopConditionResult(true, "ATTENDANCE_IMPROVED");
        }

        return new StopConditionResult(false, null);
    }

    public Task<bool> ShouldEscalateAsync(PlaybookRun run, PlaybookDefinition playbook, CancellationToken cancellationToken = default)
    {
        // Escalate if configured and enough days elapsed without stop
        if (playbook.EscalationAfterDays <= 0)
        {
            return Task.FromResult(false);
        }

        var elapsedDays = (DateTimeOffset.UtcNow - run.TriggeredAtUtc).TotalDays;
        var shouldEscalate = elapsedDays >= playbook.EscalationAfterDays && run.CurrentStepOrder > 0 && run.Status == "ACTIVE";
        return Task.FromResult(shouldEscalate);
    }
}

public sealed record StopConditionResult(bool ShouldStop, string? Reason);
