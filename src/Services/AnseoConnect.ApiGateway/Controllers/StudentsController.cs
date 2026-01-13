using AnseoConnect.ApiGateway.Models;
using AnseoConnect.Contracts.DTOs;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/students")]
[Authorize(Policy = "StaffOnly")]
public sealed class StudentsController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public StudentsController(AnseoConnectDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<StudentSummary>>> Search(
        [FromQuery] string? query = null,
        [FromQuery] string? year = null,
        [FromQuery] string? externalId = null,
        [FromQuery] string? risk = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var students = _dbContext.Students.AsNoTracking();

        if (_tenantContext.TenantId != Guid.Empty)
        {
            students = students.Where(s => s.TenantId == _tenantContext.TenantId);
        }
        if (_tenantContext.SchoolId.HasValue)
        {
            students = students.Where(s => s.SchoolId == _tenantContext.SchoolId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            students = students.Where(s => (s.FirstName + " " + s.LastName).Contains(query));
        }

        if (!string.IsNullOrWhiteSpace(year))
        {
            students = students.Where(s => s.YearGroup != null && s.YearGroup.Contains(year));
        }

        if (!string.IsNullOrWhiteSpace(externalId))
        {
            students = students.Where(s => s.ExternalStudentId == externalId);
        }

        var total = await students.CountAsync(ct);
        var items = await students
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Skip(skip)
            .Take(Math.Min(take, 200))
            .ToListAsync(ct);

        var summaries = items.Select(s => new StudentSummary(
            s.StudentId,
            $"{s.FirstName} {s.LastName}".Trim(),
            s.ExternalStudentId,
            s.YearGroup ?? "",
            risk ?? "Medium",
            0.0)).ToList();

        return Ok(new PagedResult<StudentSummary>(summaries, total, skip, take, (skip + take) < total));
    }

    [HttpGet("{studentId:guid}")]
    public async Task<ActionResult<StudentProfile>> GetStudent(Guid studentId, CancellationToken ct = default)
    {
        var student = await _dbContext.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StudentId == studentId, ct);

        if (student == null)
        {
            return NotFound();
        }

        var guardianLinks = await _dbContext.StudentGuardians
            .AsNoTracking()
            .Where(g => g.StudentId == studentId)
            .Include(g => g.Guardian)
            .ToListAsync(ct);

        var guardianIds = guardianLinks.Select(g => g.GuardianId).ToList();
        var consentStates = await _dbContext.ConsentStates
            .AsNoTracking()
            .Where(c => guardianIds.Contains(c.GuardianId))
            .ToListAsync(ct);
        var contactPrefs = await _dbContext.ContactPreferences
            .AsNoTracking()
            .Where(p => guardianIds.Contains(p.GuardianId))
            .ToListAsync(ct);

        var guardians = guardianLinks
            .Select(link =>
            {
                var guardian = link.Guardian ?? new Guardian { GuardianId = link.GuardianId, FullName = "Unknown" };
                var preference = contactPrefs.FirstOrDefault(p => p.GuardianId == guardian.GuardianId);
                var consentSms = consentStates.FirstOrDefault(c => c.GuardianId == guardian.GuardianId && c.Channel == "SMS")?.State ?? "UNKNOWN";
                var consentEmail = consentStates.FirstOrDefault(c => c.GuardianId == guardian.GuardianId && c.Channel == "EMAIL")?.State ?? "UNKNOWN";
                return new GuardianContact(
                    guardian.GuardianId,
                    guardian.FullName,
                    link.Relationship ?? "Guardian",
                    guardian.MobileE164 ?? "",
                    guardian.Email ?? "",
                    consentSms,
                    consentEmail,
                    preference?.QuietHoursJson);
            })
            .ToList();

        var cases = await _dbContext.Cases
            .AsNoTracking()
            .Where(c => c.StudentId == studentId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(10)
            .Select(c => new CaseDto(
                CaseId: c.CaseId,
                StudentId: c.StudentId,
                StudentName: $"{student.FirstName} {student.LastName}".Trim(),
                CaseType: c.CaseType,
                Tier: c.Tier,
                Status: c.Status,
                CreatedAtUtc: c.CreatedAtUtc,
                ResolvedAtUtc: c.ResolvedAtUtc,
                TimelineEvents: new List<CaseTimelineEventDto>()))
            .ToListAsync(ct);

        var messages = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.StudentId == studentId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(20)
            .Select(m => new MessageSummary(
                m.MessageId,
                m.StudentId,
                $"{student.FirstName} {student.LastName}".Trim(),
                m.Channel,
                m.Status,
                m.MessageType,
                m.CreatedAtUtc,
                m.ProviderMessageId,
                null))
            .ToListAsync(ct);

        var profile = new StudentProfile(
            new StudentSummary(
                student.StudentId,
                $"{student.FirstName} {student.LastName}".Trim(),
                student.ExternalStudentId,
                student.YearGroup ?? "",
                "Medium",
                0.0),
            guardians,
            cases,
            messages);

        return Ok(profile);
    }
}
