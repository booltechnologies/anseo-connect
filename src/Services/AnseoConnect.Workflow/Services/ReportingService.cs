using AnseoConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Reporting queries for dashboards.
/// </summary>
public sealed class ReportingService
{
    private readonly AnseoConnectDbContext _dbContext;

    public ReportingService(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SchoolDashboardMetrics> GetSchoolDashboardAsync(CancellationToken cancellationToken = default)
    {
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        var attendanceMarks = await _dbContext.AttendanceMarks
            .AsNoTracking()
            .Where(a => a.Date >= now.AddDays(-30))
            .ToListAsync(cancellationToken);

        var openCasesByTier = await _dbContext.Cases
            .AsNoTracking()
            .Where(c => c.Status == "OPEN")
            .GroupBy(c => c.Tier)
            .Select(g => new TierCount(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var openCases = await _dbContext.Cases.AsNoTracking().CountAsync(c => c.Status == "OPEN", cancellationToken);
        var safeguardingAlerts = await _dbContext.SafeguardingAlerts.AsNoTracking().CountAsync(cancellationToken);

        var since = DateTimeOffset.UtcNow.AddDays(-30);
        var messages = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.CreatedAtUtc >= since)
            .ToListAsync(cancellationToken);

        var sent = messages.Count(m => m.Status == "SENT" || m.Status == "DELIVERED");
        var delivered = messages.Count(m => m.Status == "DELIVERED");
        var failed = messages.Count(m => m.Status == "FAILED" || m.Status == "BLOCKED");
        var replyRate = messages.Count(m => m.MessageType == "GUARDIAN_REPLY");

        return new SchoolDashboardMetrics(
            TotalAttendanceMarks: attendanceMarks.Count,
            OpenCases: openCases,
            SafeguardingAlerts: safeguardingAlerts,
            OpenCasesByTier: openCasesByTier,
            SentMessages: sent,
            DeliveredMessages: delivered,
            FailedMessages: failed,
            ReplyCount: replyRate);
    }

    public async Task<EtbDashboardMetrics> GetEtbDashboardAsync(Guid etbTrustId, CancellationToken cancellationToken = default)
    {
        var schools = await _dbContext.Schools
            .AsNoTracking()
            .Where(s => s.ETBTrustId == etbTrustId)
            .Select(s => s.SchoolId)
            .ToListAsync(cancellationToken);

        var cases = await _dbContext.Cases.AsNoTracking().Where(c => schools.Contains(c.SchoolId)).ToListAsync(cancellationToken);
        var messages = await _dbContext.Messages.AsNoTracking().Where(m => schools.Contains(m.SchoolId)).ToListAsync(cancellationToken);

        var openCasesByTier = cases
            .Where(c => c.Status == "OPEN")
            .GroupBy(c => c.Tier)
            .Select(g => new TierCount(g.Key, g.Count()))
            .ToList();

        return new EtbDashboardMetrics(
            SchoolCount: schools.Count,
            OpenCases: cases.Count(c => c.Status == "OPEN"),
            SafeguardingAlerts: _dbContext.SafeguardingAlerts.Count(a => schools.Contains(a.SchoolId)),
            OpenCasesByTier: openCasesByTier,
            SentMessages: messages.Count(m => m.Status == "SENT" || m.Status == "DELIVERED"),
            DeliveredMessages: messages.Count(m => m.Status == "DELIVERED"),
            FailedMessages: messages.Count(m => m.Status == "FAILED" || m.Status == "BLOCKED"));
    }
}

public sealed record TierCount(int Tier, int Count);

public sealed record SchoolDashboardMetrics(
    int TotalAttendanceMarks,
    int OpenCases,
    int SafeguardingAlerts,
    IReadOnlyList<TierCount> OpenCasesByTier,
    int SentMessages,
    int DeliveredMessages,
    int FailedMessages,
    int ReplyCount);

public sealed record EtbDashboardMetrics(
    int SchoolCount,
    int OpenCases,
    int SafeguardingAlerts,
    IReadOnlyList<TierCount> OpenCasesByTier,
    int SentMessages,
    int DeliveredMessages,
    int FailedMessages);
