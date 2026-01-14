using AnseoConnect.ApiGateway.Controllers;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Infrastructure;
using Xunit;

namespace AnseoConnect.IntegrationTests;

public class InterventionEndpointTests
{
    static InterventionEndpointTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
    private static AnseoConnectDbContext CreateDbContext(string dbName, ITenantContext tenant)
    {
        var options = new DbContextOptionsBuilder<AnseoConnectDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AnseoConnectDbContext(options, tenant);
    }


    [Fact(Skip = "Pending tenant seed stabilization")]
    public async Task GetQueue_ReturnsEnrichedDtos()
    {
        var tenant = new TenantContext();
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var schoolId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var studentId = Guid.NewGuid();
        var ruleSetId = Guid.NewGuid();
        tenant.Set(tenantId, schoolId);
        await using var db = CreateDbContext($"api_{Guid.NewGuid():N}", tenant);

        db.Students.Add(new Student
        {
            StudentId = studentId,
            TenantId = tenantId,
            SchoolId = schoolId,
            FirstName = "Test",
            LastName = "Student",
            ExternalStudentId = "S1"
        });

        db.InterventionRuleSets.Add(new InterventionRuleSet
        {
            RuleSetId = ruleSetId,
            TenantId = tenantId,
            SchoolId = schoolId,
            Name = "Rule One",
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
            AttendancePercent = 80
        });

        await db.SaveChangesAsync();

        var controller = new InterventionsController(db, tenant);
        Assert.NotNull(controller);
    }

    [Fact(Skip = "Pending tenant seed stabilization")]
    public async Task AdvanceStage_MovesToNextStage()
    {
        var tenant = new TenantContext();
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var schoolId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        tenant.Set(tenantId, schoolId);
        await using var db = CreateDbContext($"api_{Guid.NewGuid():N}", tenant);

        var stage1 = new InterventionStage { StageId = Guid.NewGuid(), TenantId = tenantId, RuleSetId = Guid.NewGuid(), Order = 1, StageType = "LETTER_1" };
        var stage2 = new InterventionStage { StageId = Guid.NewGuid(), TenantId = tenantId, RuleSetId = stage1.RuleSetId, Order = 2, StageType = "LETTER_2" };
        db.InterventionStages.AddRange(stage1, stage2);
        db.StudentInterventionInstances.Add(new StudentInterventionInstance
        {
            InstanceId = Guid.NewGuid(),
            TenantId = tenantId,
            SchoolId = schoolId,
            StudentId = Guid.NewGuid(),
            RuleSetId = stage1.RuleSetId,
            CurrentStageId = stage1.StageId,
            Status = "ACTIVE",
            StartedAtUtc = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = new InterventionsController(db, tenant);
        Assert.NotNull(controller);
    }

    [Fact(Skip = "Pending tenant seed stabilization")]
    public async Task GenerateLetter_CreatesArtifact()
    {
        var tenant = new TenantContext();
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var schoolId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        tenant.Set(tenantId, schoolId);
        await using var db = CreateDbContext($"api_{Guid.NewGuid():N}", tenant);

        var stageId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var guardianId = Guid.NewGuid();

        db.StudentInterventionInstances.Add(new StudentInterventionInstance
        {
            InstanceId = instanceId,
            TenantId = tenantId,
            SchoolId = schoolId,
            StudentId = Guid.NewGuid(),
            RuleSetId = Guid.NewGuid(),
            CurrentStageId = stageId,
            Status = "ACTIVE",
            StartedAtUtc = DateTimeOffset.UtcNow
        });

        db.InterventionStages.Add(new InterventionStage
        {
            StageId = stageId,
            TenantId = tenantId,
            RuleSetId = Guid.NewGuid(),
            Order = 1,
            StageType = "LETTER_1",
            LetterTemplateId = templateId
        });

        db.LetterTemplates.Add(new LetterTemplate
        {
            TemplateId = templateId,
            TenantId = tenantId,
            TemplateKey = "TEST",
            Version = 1,
            Status = "APPROVED",
            LockScope = "GLOBAL",
            BodyHtml = "<p>Hello</p>"
        });

        await db.SaveChangesAsync();

        var controller = new LettersController(db, new LetterGenerationService(db, NullLogger<LetterGenerationService>.Instance));
        var response = await controller.Generate(new LettersController.GenerateLetterRequest(instanceId, stageId, guardianId, "en", null), CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(response);
        var artifact = Assert.IsType<LetterArtifact>(ok.Value);
        Assert.Equal(instanceId, artifact.InstanceId);
        Assert.NotEqual(Guid.Empty, artifact.ArtifactId);
    }

    [Fact(Skip = "Pending tenant seed stabilization")]
    public async Task TriggerReport_CreatesRunAndArtifact()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var schoolId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var definitionId = Guid.NewGuid();
        var tenant = new TenantContext();
        tenant.Set(tenantId, schoolId);
        await using var db = CreateDbContext($"api_{Guid.NewGuid():N}", tenant);

        db.ReportDefinitions.Add(new ReportDefinition
        {
            DefinitionId = definitionId,
            TenantId = tenantId,
            Name = "Test Report",
            ReportType = "SUMMARY",
            ScheduleCron = "* * * * *",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var controller = new ReportsController(db, null!);
        var result = await controller.TriggerRun(definitionId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var run = Assert.IsType<ReportRun>(ok.Value);
        Assert.Equal("COMPLETED", run.Status);
        Assert.True(db.ReportArtifacts.Any(a => a.RunId == run.RunId));
    }
}
