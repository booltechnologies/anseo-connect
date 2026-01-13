using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Service for detecting unexplained absences after school cutoff time.
/// </summary>
public sealed class AbsenceDetectionService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<AbsenceDetectionService> _logger;
    private readonly ITenantContext _tenantContext;

    public AbsenceDetectionService(
        AnseoConnectDbContext dbContext,
        ILogger<AbsenceDetectionService> logger,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Detects unexplained absences for a specific date after cutoff time.
    /// </summary>
    public async Task<List<UnexplainedAbsence>> DetectUnexplainedAbsencesAsync(
        Guid schoolId,
        DateOnly date,
        TimeOnly? cutoffTime = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detecting unexplained absences for school {SchoolId}, date {Date}", schoolId, date);

        // Get school to determine cutoff time and timezone
        var school = await _dbContext.Schools
            .AsNoTracking()
            .Where(s => s.SchoolId == schoolId)
            .FirstOrDefaultAsync(cancellationToken);

        if (school == null)
        {
            throw new InvalidOperationException($"School {schoolId} not found.");
        }

        // Resolve timezone
        var tz = "UTC";
        if (!string.IsNullOrWhiteSpace(school.Timezone))
        {
            tz = school.Timezone;
        }

        TimeZoneInfo tzInfo;
        try
        {
            tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
        }
        catch
        {
            _logger.LogWarning("Invalid timezone {Timezone} for school {SchoolId}, defaulting to UTC", tz, schoolId);
            tzInfo = TimeZoneInfo.Utc;
        }

        // Load per-school cutoffs
        var settings = await _dbContext.SchoolSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SchoolId == schoolId, cancellationToken);

        var amCutoff = cutoffTime ?? settings?.AMCutoffTime ?? new TimeOnly(10, 30);
        var pmCutoff = settings?.PMCutoffTime ?? new TimeOnly(14, 30);

        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tzInfo);
        if (localNow.Date < date.ToDateTime(TimeOnly.MinValue).Date)
        {
            // Do not run detection before the target date in school local time
            return new List<UnexplainedAbsence>();
        }

        var allowedSessions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (localNow.Date > date.ToDateTime(TimeOnly.MinValue).Date)
        {
            // Past the target date: include both sessions
            allowedSessions.Add("AM");
            allowedSessions.Add("PM");
        }
        else
        {
            if (localNow.TimeOfDay >= amCutoff.ToTimeSpan()) allowedSessions.Add("AM");
            if (localNow.TimeOfDay >= pmCutoff.ToTimeSpan()) allowedSessions.Add("PM");
        }

        if (allowedSessions.Count == 0)
        {
            _logger.LogInformation("Cutoff not reached for date {Date} in school {SchoolId}; skipping detection.", date, schoolId);
            return new List<UnexplainedAbsence>();
        }

        // Query attendance marks for the date where:
        // - Status is ABSENT or UNKNOWN
        // - ReasonCode is null or not in accepted reasons (would be configured from policy pack)
        // - Created/updated after cutoff time
        var unexplainedAbsences = await _dbContext.AttendanceMarks
            .Where(am => am.Date == date &&
                        allowedSessions.Contains(am.Session) &&
                        (am.Status == "ABSENT" || am.Status == "UNKNOWN") &&
                        (am.ReasonCode == null || am.ReasonCode == ""))
            .Select(am => new { am.StudentId, am.Session })
            .Distinct()
            .ToListAsync(cancellationToken);

        var result = new List<UnexplainedAbsence>();

        foreach (var absence in unexplainedAbsences)
        {
            var student = await _dbContext.Students
                .Where(s => s.StudentId == absence.StudentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (student == null || !student.IsActive)
            {
                continue;
            }

            result.Add(new UnexplainedAbsence
            {
                StudentId = absence.StudentId,
                StudentName = $"{student.FirstName} {student.LastName}".Trim(),
                Date = date,
                Session = absence.Session
            });
        }

        _logger.LogInformation(
            "Detected {Count} unexplained absences for school {SchoolId}, date {Date}",
            result.Count,
            schoolId,
            date);

        return result;
    }
}

public sealed record UnexplainedAbsence
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public DateOnly Date { get; set; }
    public string Session { get; set; } = "";
}
