using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;

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

        // Default cutoff: 10:00 AM
        var cutoff = cutoffTime ?? TimeOnly.FromTimeSpan(TimeSpan.FromHours(10));
        var cutoffDateTime = date.ToDateTime(cutoff);

        // Query attendance marks for the date where:
        // - Status is ABSENT or UNKNOWN
        // - ReasonCode is null or not in accepted reasons (would be configured from policy pack)
        // - Created/updated after cutoff time
        var unexplainedAbsences = await _dbContext.AttendanceMarks
            .Where(am => am.Date == date &&
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
