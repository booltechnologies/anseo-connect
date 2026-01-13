using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AnseoConnect.Workflow.Tests.Services;

public class TierEvaluatorTests
{
    [Fact]
    public async Task MeetsEntryCriteriaAsync_WithValidCriteria_ReturnsTrue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AnseoConnectDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new AnseoConnectDbContext(options, new Mock<AnseoConnect.Data.MultiTenancy.ITenantContext>().Object);
        var logger = new Mock<ILogger<TierEvaluator>>();
        var evaluator = new TierEvaluator(dbContext, logger.Object);

        var studentId = Guid.NewGuid();
        var tier = new MtssTierDefinition
        {
            TierDefinitionId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            TierNumber = 2,
            EntryCriteriaJson = """{"attendancePercentBelow": 90, "absenceCountAbove": 10}"""
        };

        var summary = new AttendanceDailySummary
        {
            SummaryId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            SchoolId = Guid.NewGuid(),
            StudentId = studentId,
            Date = DateOnly.FromDateTime(DateTime.Today),
            AttendancePercent = 85,
            TotalAbsenceDaysYTD = 15
        };

        dbContext.AttendanceDailySummaries.Add(summary);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await evaluator.MeetsEntryCriteriaAsync(studentId, tier);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void BuildRationale_WithEvaluationResult_ReturnsFormattedString()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AnseoConnectDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new AnseoConnectDbContext(options, new Mock<AnseoConnect.Data.MultiTenancy.ITenantContext>().Object);
        var logger = new Mock<ILogger<TierEvaluator>>();
        var evaluator = new TierEvaluator(dbContext, logger.Object);

        var result = new TierEvaluationResult
        {
            StudentId = Guid.NewGuid(),
            MeetsCriteria = true,
            TriggeredConditions = new List<string> { "ATTENDANCE_THRESHOLD" },
            AttendancePercent = 85.5m,
            AbsenceCount = 12
        };

        // Act
        var rationale = evaluator.BuildRationale(result);

        // Assert
        Assert.Contains("Attendance: 85.5%", rationale);
        Assert.Contains("Total absences: 12", rationale);
        Assert.Contains("ATTENDANCE_THRESHOLD", rationale);
    }
}
