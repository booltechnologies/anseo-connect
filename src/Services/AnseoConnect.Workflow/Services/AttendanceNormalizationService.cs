using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Normalizes raw attendance marks into daily summaries with rolling metrics used by the rule engine.
/// </summary>
public sealed class AttendanceNormalizationService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<AttendanceNormalizationService> _logger;

    public AttendanceNormalizationService(
        AnseoConnectDbContext dbContext,
        ILogger<AttendanceNormalizationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Normalize marks for a school and date into AttendanceDailySummary rows.
    /// </summary>
    public async Task NormalizeAsync(Guid schoolId, DateOnly date, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Normalizing attendance for school {SchoolId} on {Date}", schoolId, date);

        var marks = await _dbContext.AttendanceMarks
            .AsNoTracking()
            .Where(m => m.SchoolId == schoolId && m.Date == date)
            .ToListAsync(cancellationToken);

        if (marks.Count == 0)
        {
            _logger.LogInformation("No attendance marks to normalize for school {SchoolId} on {Date}", schoolId, date);
            return;
        }

        var studentIds = marks.Select(m => m.StudentId).Distinct().ToList();
        var startWindow = date.AddDays(-29); // rolling 30-day window
        var yearStart = new DateOnly(date.Year, 1, 1);

        var windowMarks = await _dbContext.AttendanceMarks
            .AsNoTracking()
            .Where(m => m.SchoolId == schoolId &&
                        studentIds.Contains(m.StudentId) &&
                        m.Date >= startWindow &&
                        m.Date <= date)
            .ToListAsync(cancellationToken);

        var ytdMarks = await _dbContext.AttendanceMarks
            .AsNoTracking()
            .Where(m => m.SchoolId == schoolId &&
                        studentIds.Contains(m.StudentId) &&
                        m.Date >= yearStart &&
                        m.Date <= date)
            .ToListAsync(cancellationToken);

        foreach (var studentId in studentIds)
        {
            var studentMarksForDay = marks.Where(m => m.StudentId == studentId).ToList();
            var am = studentMarksForDay.FirstOrDefault(m => string.Equals(m.Session, "AM", StringComparison.OrdinalIgnoreCase));
            var pm = studentMarksForDay.FirstOrDefault(m => string.Equals(m.Session, "PM", StringComparison.OrdinalIgnoreCase));

            var (attendancePercent, consecutiveAbsences, totalAbsenceYtd) =
                CalculateMetrics(studentId, windowMarks, ytdMarks, date);

            var existing = await _dbContext.AttendanceDailySummaries
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.Date == date, cancellationToken);

            if (existing == null)
            {
                existing = new AttendanceDailySummary
                {
                    SummaryId = Guid.NewGuid(),
                    StudentId = studentId,
                    Date = date,
                    AMStatus = am?.Status ?? "UNKNOWN",
                    PMStatus = pm?.Status ?? "UNKNOWN",
                    AMReasonCode = am?.ReasonCode,
                    PMReasonCode = pm?.ReasonCode,
                    AttendancePercent = attendancePercent,
                    ConsecutiveAbsenceDays = consecutiveAbsences,
                    TotalAbsenceDaysYTD = totalAbsenceYtd,
                    ComputedAtUtc = DateTimeOffset.UtcNow
                };
                _dbContext.AttendanceDailySummaries.Add(existing);
            }
            else
            {
                existing.AMStatus = am?.Status ?? "UNKNOWN";
                existing.PMStatus = pm?.Status ?? "UNKNOWN";
                existing.AMReasonCode = am?.ReasonCode;
                existing.PMReasonCode = pm?.ReasonCode;
                existing.AttendancePercent = attendancePercent;
                existing.ConsecutiveAbsenceDays = consecutiveAbsences;
                existing.TotalAbsenceDaysYTD = totalAbsenceYtd;
                existing.ComputedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static (decimal attendancePercent, int consecutiveAbsences, int totalAbsenceYtd) CalculateMetrics(
        Guid studentId,
        List<AttendanceMark> windowMarks,
        List<AttendanceMark> ytdMarks,
        DateOnly date)
    {
        var studentWindow = windowMarks
            .Where(m => m.StudentId == studentId)
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Session)
            .ToList();

        var studentYtd = ytdMarks
            .Where(m => m.StudentId == studentId)
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Session)
            .ToList();

        int attended = studentWindow.Count(m => IsPresent(m.Status));
        int totalSessions = studentWindow.Count(m => IsCountable(m.Status));
        decimal attendancePercent = totalSessions == 0
            ? 0
            : Math.Round((decimal)attended / totalSessions * 100, 2);

        int consecutiveAbsences = 0;
        var days = studentYtd
            .GroupBy(m => m.Date)
            .OrderByDescending(g => g.Key)
            .ToList();

        foreach (var day in days)
        {
            if (day.Key > date)
            {
                continue;
            }

            var dayAbsent = day.All(m => string.Equals(m.Status, "ABSENT", StringComparison.OrdinalIgnoreCase));
            if (dayAbsent)
            {
                consecutiveAbsences++;
                continue;
            }

            // Stop streak when a non-absence day is reached
            break;
        }

        var totalAbsenceDaysYtd = studentYtd
            .GroupBy(m => m.Date)
            .Count(g => g.All(m => string.Equals(m.Status, "ABSENT", StringComparison.OrdinalIgnoreCase)));

        return (attendancePercent, consecutiveAbsences, totalAbsenceDaysYtd);
    }

    private static bool IsPresent(string status) =>
        string.Equals(status, "PRESENT", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "LATE", StringComparison.OrdinalIgnoreCase);

    private static bool IsCountable(string status) =>
        !string.Equals(status, "UNKNOWN", StringComparison.OrdinalIgnoreCase);
}

