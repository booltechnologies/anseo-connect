using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Builds and reads intervention analytics snapshots.
/// </summary>
public sealed class InterventionAnalyticsService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<InterventionAnalyticsService> _logger;

    public InterventionAnalyticsService(
        AnseoConnectDbContext dbContext,
        ILogger<InterventionAnalyticsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<InterventionAnalytics> BuildSnapshotAsync(Guid schoolId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var tenantId = _dbContext.AttendanceMarks.AsNoTracking().Where(x => x.SchoolId == schoolId).Select(x => x.TenantId).FirstOrDefault();

        var totalStudents = await _dbContext.Students.AsNoTracking().CountAsync(s => s.SchoolId == schoolId, cancellationToken);
        var activeInstances = await _dbContext.StudentInterventionInstances.AsNoTracking()
            .Where(i => i.SchoolId == schoolId && i.Status == "ACTIVE")
            .ToListAsync(cancellationToken);

        var letterArtifacts = await _dbContext.LetterArtifacts.AsNoTracking()
            .Where(l => l.SchoolId == schoolId && l.GeneratedAtUtc.Date == date.ToDateTime(TimeOnly.MinValue).Date)
            .ToListAsync(cancellationToken);

        var meetings = await _dbContext.InterventionMeetings.AsNoTracking()
            .Where(m => m.SchoolId == schoolId)
            .ToListAsync(cancellationToken);

        var analytics = await _dbContext.InterventionAnalytics
            .FirstOrDefaultAsync(a => a.SchoolId == schoolId && a.Date == date, cancellationToken);

        if (analytics == null)
        {
            analytics = new InterventionAnalytics
            {
                AnalyticsId = Guid.NewGuid(),
                TenantId = tenantId,
                SchoolId = schoolId,
                Date = date
            };
            _dbContext.InterventionAnalytics.Add(analytics);
        }

        analytics.TotalStudents = totalStudents;
        analytics.StudentsInIntervention = activeInstances.Count;
        analytics.Letter1Sent = letterArtifacts.Count(a => a.TemplateVersion == 1);
        analytics.Letter2Sent = letterArtifacts.Count(a => a.TemplateVersion == 2);
        analytics.MeetingsScheduled = meetings.Count(m => m.Status == "SCHEDULED");
        analytics.MeetingsHeld = meetings.Count(m => m.Status == "HELD");
        analytics.Escalated = activeInstances.Count(i => i.Status == "ESCALATED");
        analytics.Resolved = activeInstances.Count(i => i.Status == "COMPLETED");

        analytics.PreInterventionAttendanceAvg = await CalculateAttendanceAsync(schoolId, date.AddDays(-30), date, cancellationToken);
        analytics.PostInterventionAttendanceAvg = await CalculateAttendanceAsync(schoolId, date.AddDays(-7), date, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Built analytics snapshot for school {SchoolId} on {Date}", schoolId, date);
        return analytics;
    }

    public async Task<InterventionAnalytics?> GetAsync(Guid schoolId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _dbContext.InterventionAnalytics
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.SchoolId == schoolId && a.Date == date, cancellationToken);
    }

    public async Task<IReadOnlyList<DailyAnalyticsPoint>> BuildTrendAsync(Guid schoolId, int days, CancellationToken cancellationToken = default)
    {
        var end = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = end.AddDays(-(days - 1));
        var points = new List<DailyAnalyticsPoint>();

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var snapshot = await BuildSnapshotAsync(schoolId, date, cancellationToken);
            points.Add(new DailyAnalyticsPoint(
                snapshot.Date,
                snapshot.PostInterventionAttendanceAvg,
                snapshot.StudentsInIntervention,
                snapshot.Letter1Sent,
                snapshot.Letter2Sent,
                snapshot.MeetingsHeld));
        }

        return points;
    }

    private async Task<decimal> CalculateAttendanceAsync(Guid schoolId, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        var summaries = await _dbContext.AttendanceDailySummaries
            .AsNoTracking()
            .Where(s => s.SchoolId == schoolId && s.Date >= start && s.Date <= end)
            .ToListAsync(cancellationToken);

        if (summaries.Count == 0)
        {
            return 0;
        }

        return Math.Round(summaries.Average(s => s.AttendancePercent), 2);
    }
}

public sealed record DailyAnalyticsPoint(
    DateOnly Date,
    decimal AttendanceRate,
    int StudentsInIntervention,
    int Letter1Sent,
    int Letter2Sent,
    int MeetingsHeld);

