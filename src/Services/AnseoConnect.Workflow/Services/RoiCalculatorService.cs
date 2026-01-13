using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Services;

public sealed class RoiCalculatorService
{
    private readonly AnseoConnectDbContext _dbContext;
    private const decimal MinutesPerManualTouch = 5m;

    public RoiCalculatorService(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RoiSummary> CalculateAsync(Guid schoolId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var metrics = await _dbContext.AutomationMetrics
            .Where(m => m.SchoolId == schoolId && m.Date >= from && m.Date <= to)
            .ToListAsync(cancellationToken);

        if (metrics.Count == 0)
        {
            return new RoiSummary(0, 0, 0, 0, 0, 0);
        }

        var totalTouches = metrics.Sum(m => m.StepsSent);
        var hoursSaved = (totalTouches * MinutesPerManualTouch) / 60m;

        return new RoiSummary(
            TotalPlaybooksRun: metrics.Sum(m => m.PlaybooksStarted),
            TotalTouchesSent: totalTouches,
            EstimatedHoursSaved: Math.Round(hoursSaved, 2),
            StudentsReachedByAutomation: metrics.Count, // proxy
            StudentsImprovedAfterPlaybook: metrics.Sum(m => m.PlaybooksStoppedByImprovement),
            AvgAttendanceChangePercent: metrics.Average(m => m.AttendanceImprovementDelta));
    }

    public sealed record RoiSummary(
        int TotalPlaybooksRun,
        int TotalTouchesSent,
        decimal EstimatedHoursSaved,
        int StudentsReachedByAutomation,
        int StudentsImprovedAfterPlaybook,
        decimal AvgAttendanceChangePercent);
}
