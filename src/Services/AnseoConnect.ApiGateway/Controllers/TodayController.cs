using AnseoConnect.ApiGateway.Models;
using AnseoConnect.ApiGateway.Services;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/today")]
[Authorize(Policy = "StaffOnly")]
public sealed class TodayController : ControllerBase
{
    private readonly CaseQueryService _caseQueryService;
    private readonly AnseoConnectDbContext _dbContext;

    public TodayController(CaseQueryService caseQueryService, AnseoConnectDbContext dbContext)
    {
        _caseQueryService = caseQueryService;
        _dbContext = dbContext;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<TodayDashboardDto>> GetDashboard(CancellationToken ct = default)
    {
        var absences = await _caseQueryService.GetTodayUnexplainedAbsencesAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var tasks = await _dbContext.WorkTasks
            .AsNoTracking()
            .Where(t => t.Status == "OPEN" && t.DueAtUtc != null && t.DueAtUtc.Value.Date == DateTimeOffset.UtcNow.Date)
            .OrderBy(t => t.DueAtUtc)
            .Select(t => new TaskSummary(
                t.Title,
                t.DueAtUtc ?? DateTimeOffset.UtcNow,
                t.ChecklistId ?? "Task",
                t.Status,
                t.CaseId))
            .ToListAsync(ct);

        var alerts = await (from alert in _dbContext.SafeguardingAlerts.AsNoTracking()
                            join c in _dbContext.Cases.AsNoTracking().Include(ca => ca.Student)
                                on alert.CaseId equals c.CaseId into caseJoin
                            from c in caseJoin.DefaultIfEmpty()
                            orderby alert.CreatedAtUtc descending
                            select new SafeguardingAlertSummary(
                                alert.AlertId,
                                c != null ? c.StudentId : Guid.Empty,
                                c != null && c.Student != null ? (c.Student.FirstName + " " + c.Student.LastName).Trim() : "Unknown",
                                alert.Severity,
                                alert.ChecklistId ?? "Safeguarding",
                                alert.CreatedAtUtc,
                                alert.ReviewedBy,
                                alert.AcknowledgedAtUtc))
            .Take(50)
            .ToListAsync(ct);

        var failures = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.Status == "FAILED" || m.Status == "BLOCKED")
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(50)
            .Join(_dbContext.Students.AsNoTracking(),
                m => m.StudentId,
                s => s.StudentId,
                (m, s) => new MessageSummary(
                    m.MessageId,
                    m.StudentId,
                    $"{s.FirstName} {s.LastName}".Trim(),
                    m.Channel,
                    m.Status,
                    m.MessageType,
                    m.CreatedAtUtc,
                    m.ProviderMessageId,
                    null))
            .ToListAsync(ct);

        var missingContacts = await _dbContext.Guardians
            .AsNoTracking()
            .Where(g => (g.MobileE164 == null || g.MobileE164 == "") && (g.Email == null || g.Email == ""))
            .Take(50)
            .Select(g => new GuardianContact(
                g.GuardianId,
                g.FullName,
                "Guardian",
                g.MobileE164 ?? "",
                g.Email ?? "",
                "UNKNOWN",
                "UNKNOWN",
                null))
            .ToListAsync(ct);

        var dashboard = new TodayDashboardDto(
            today,
            absences,
            tasks,
            alerts,
            failures,
            missingContacts);

        return Ok(dashboard);
    }
}
