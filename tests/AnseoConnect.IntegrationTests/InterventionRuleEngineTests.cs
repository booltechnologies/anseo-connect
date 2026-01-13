using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AnseoConnect.IntegrationTests;

public class InterventionRuleEngineTests
{
    private static AnseoConnectDbContext CreateDbContext(string dbName, Guid tenantId, Guid schoolId)
    {
        var options = new DbContextOptionsBuilder<AnseoConnectDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var tenant = new TestTenantContext();
        tenant.Set(tenantId, schoolId);
        return new AnseoConnectDbContext(options, tenant);
    }

    [Fact(Skip = "Pending tenant seed stabilization")]
    public async Task EvaluateAsync_AttendanceThreshold_ReturnsEligible()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var schoolId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var studentId = Guid.NewGuid();
        var ruleSetId = Guid.NewGuid();
        await using var db = CreateDbContext($"rules_{Guid.NewGuid():N}", tenantId, schoolId);

        db.InterventionRuleSets.Add(new InterventionRuleSet
        {
            RuleSetId = ruleSetId,
            TenantId = tenantId,
            SchoolId = schoolId,
            Name = "Test Rule",
            IsActive = true,
            RulesJson = "[{\"type\":\"AttendancePercentThreshold\",\"thresholdPercentage\":90}]"
        });

        db.AttendanceDailySummaries.Add(new AttendanceDailySummary
        {
            SummaryId = Guid.NewGuid(),
            TenantId = tenantId,
            SchoolId = schoolId,
            StudentId = studentId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            AttendancePercent = 80,
            ConsecutiveAbsenceDays = 0,
            TotalAbsenceDaysYTD = 0
        });

        await db.SaveChangesAsync();

        var engine = new InterventionRuleEngine(db, NullLogger<InterventionRuleEngine>.Instance);
        var results = await engine.EvaluateAsync(schoolId, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.Single(results);
        Assert.Equal(studentId, results[0].StudentId);
        Assert.Equal(ruleSetId, results[0].RuleSetId);
        Assert.Contains("ATTENDANCEPERCENTTHRESHOLD", results[0].TriggeredConditions, StringComparer.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Pending tenant seed stabilization")]
    public async Task EvaluateAsync_NoActiveRules_ReturnsEmpty()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var schoolId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        await using var db = CreateDbContext($"rules_{Guid.NewGuid():N}", tenantId, schoolId);

        db.InterventionRuleSets.Add(new InterventionRuleSet
        {
            RuleSetId = Guid.NewGuid(),
            TenantId = tenantId,
            SchoolId = schoolId,
            Name = "Inactive Rule",
            IsActive = false
        });

        await db.SaveChangesAsync();

        var engine = new InterventionRuleEngine(db, NullLogger<InterventionRuleEngine>.Instance);
        var results = await engine.EvaluateAsync(schoolId, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.Empty(results);
    }

    [Fact(Skip = "Pending tenant seed stabilization")]
    public async Task SimulateAsync_StudentMeetsCriteria_ReturnsTrue()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var schoolId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var studentId = Guid.NewGuid();
        var ruleSetId = Guid.NewGuid();
        await using var db = CreateDbContext($"rules_{Guid.NewGuid():N}", tenantId, schoolId);

        var ruleSet = new InterventionRuleSet
        {
            RuleSetId = ruleSetId,
            TenantId = tenantId,
            SchoolId = schoolId,
            Name = "Consecutive Absence",
            IsActive = true,
            RulesJson = "[{\"type\":\"ConsecutiveAbsenceDays\",\"consecutiveDays\":3}]"
        };

        db.InterventionRuleSets.Add(ruleSet);

        db.AttendanceDailySummaries.Add(new AttendanceDailySummary
        {
            SummaryId = Guid.NewGuid(),
            TenantId = tenantId,
            SchoolId = schoolId,
            StudentId = studentId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ConsecutiveAbsenceDays = 4,
            TotalAbsenceDaysYTD = 4,
            AttendancePercent = 70
        });

        await db.SaveChangesAsync();

        var engine = new InterventionRuleEngine(db, NullLogger<InterventionRuleEngine>.Instance);
        var result = await engine.SimulateAsync(studentId, ruleSet, DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.True(result.IsEligible);
        Assert.Contains(result.TriggeredConditions, c => string.Equals(c, "CONSECUTIVEABSENCEDAYS", StringComparison.OrdinalIgnoreCase));
    }
}
