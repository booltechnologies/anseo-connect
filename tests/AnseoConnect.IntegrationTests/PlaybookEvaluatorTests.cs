using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AnseoConnect.IntegrationTests;

public class PlaybookEvaluatorTests
{
    private static AnseoConnectDbContext CreateDb(string name, Guid tenantId, Guid schoolId)
    {
        var options = new DbContextOptionsBuilder<AnseoConnectDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        var tenant = new TenantContext();
        tenant.Set(tenantId, schoolId);
        return new AnseoConnectDbContext(options, tenant);
    }

    [Fact]
    public async Task EvaluateStop_WhenCaseClosed_ReturnsStop()
    {
        var tenantId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var caseId = Guid.NewGuid();
        var run = new PlaybookRun
        {
            RunId = Guid.NewGuid(),
            TenantId = tenantId,
            SchoolId = schoolId,
            InstanceId = instanceId,
            PlaybookId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            TriggeredAtUtc = DateTimeOffset.UtcNow
        };

        await using var db = CreateDb($"playbook_eval_{Guid.NewGuid():N}", tenantId, schoolId);
        db.StudentInterventionInstances.Add(new StudentInterventionInstance
        {
            InstanceId = instanceId,
            TenantId = tenantId,
            SchoolId = schoolId,
            StudentId = run.StudentId,
            CaseId = caseId,
            RuleSetId = Guid.NewGuid(),
            CurrentStageId = Guid.NewGuid(),
            Status = "ACTIVE"
        });
        db.Cases.Add(new Case
        {
            CaseId = caseId,
            TenantId = tenantId,
            SchoolId = schoolId,
            StudentId = run.StudentId,
            Status = "CLOSED"
        });
        await db.SaveChangesAsync();

        var evaluator = new PlaybookEvaluator(db, NullLogger<PlaybookEvaluator>.Instance);
        var result = await evaluator.EvaluateStopConditionsAsync(run);

        Assert.True(result.ShouldStop);
        Assert.Equal("CASE_CLOSED", result.Reason);
    }

    [Fact]
    public async Task EvaluateStop_WhenGuardianReplied_ReturnsStop()
    {
        var tenantId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var guardianId = Guid.NewGuid();
        var run = new PlaybookRun
        {
            RunId = Guid.NewGuid(),
            TenantId = tenantId,
            SchoolId = schoolId,
            InstanceId = Guid.NewGuid(),
            PlaybookId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            GuardianId = guardianId,
            TriggeredAtUtc = DateTimeOffset.UtcNow.AddHours(-1)
        };

        await using var db = CreateDb($"playbook_eval_{Guid.NewGuid():N}", tenantId, schoolId);
        db.StudentInterventionInstances.Add(new StudentInterventionInstance
        {
            InstanceId = run.InstanceId,
            TenantId = tenantId,
            SchoolId = schoolId,
            StudentId = run.StudentId,
            RuleSetId = Guid.NewGuid(),
            CurrentStageId = Guid.NewGuid(),
            Status = "ACTIVE"
        });
        db.EngagementEvents.Add(new EngagementEvent
        {
            EventId = Guid.NewGuid(),
            TenantId = tenantId,
            GuardianId = guardianId,
            MessageId = Guid.NewGuid(),
            EventType = "REPLIED",
            OccurredAtUtc = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var evaluator = new PlaybookEvaluator(db, NullLogger<PlaybookEvaluator>.Instance);
        var result = await evaluator.EvaluateStopConditionsAsync(run);

        Assert.True(result.ShouldStop);
        Assert.Equal("GUARDIAN_REPLIED", result.Reason);
    }

    [Fact]
    public async Task EvaluateStop_WhenAttendanceImproved_ReturnsStop()
    {
        var tenantId = Guid.NewGuid();
        var schoolId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var run = new PlaybookRun
        {
            RunId = Guid.NewGuid(),
            TenantId = tenantId,
            SchoolId = schoolId,
            InstanceId = Guid.NewGuid(),
            PlaybookId = Guid.NewGuid(),
            StudentId = studentId,
            TriggeredAtUtc = DateTimeOffset.UtcNow.AddDays(-2)
        };

        await using var db = CreateDb($"playbook_eval_{Guid.NewGuid():N}", tenantId, schoolId);
        db.StudentInterventionInstances.Add(new StudentInterventionInstance
        {
            InstanceId = run.InstanceId,
            TenantId = tenantId,
            SchoolId = schoolId,
            StudentId = studentId,
            RuleSetId = Guid.NewGuid(),
            CurrentStageId = Guid.NewGuid(),
            Status = "ACTIVE"
        });
        db.AttendanceDailySummaries.Add(new AttendanceDailySummary
        {
            SummaryId = Guid.NewGuid(),
            TenantId = tenantId,
            SchoolId = schoolId,
            StudentId = studentId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            AttendancePercent = 95m
        });
        await db.SaveChangesAsync();

        var evaluator = new PlaybookEvaluator(db, NullLogger<PlaybookEvaluator>.Instance);
        var result = await evaluator.EvaluateStopConditionsAsync(run);

        Assert.True(result.ShouldStop);
        Assert.Equal("ATTENDANCE_IMPROVED", result.Reason);
    }
}
