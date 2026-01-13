using AnseoConnect.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/health/jobs")]
[Authorize(Policy = "StaffOnly")]
public sealed class JobHealthController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;

    public JobHealthController(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var latestInterventionEvent = await _dbContext.InterventionEvents
            .AsNoTracking()
            .OrderByDescending(e => e.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var latestReportRun = await _dbContext.ReportRuns
            .AsNoTracking()
            .OrderByDescending(r => r.CompletedAtUtc ?? r.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var locks = await _dbContext.JobLocks
            .AsNoTracking()
            .Select(l => new
            {
                l.LockName,
                l.HolderInstanceId,
                l.AcquiredAtUtc,
                l.ExpiresAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            SchedulerLastEventUtc = latestInterventionEvent?.OccurredAtUtc,
            ReportLastRunUtc = latestReportRun?.CompletedAtUtc ?? latestReportRun?.StartedAtUtc,
            Locks = locks
        });
    }
}
