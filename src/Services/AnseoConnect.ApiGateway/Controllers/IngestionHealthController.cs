using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/ingestion")]
[Authorize]
public sealed class IngestionHealthController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;

    public IngestionHealthController(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetIngestionHealth(CancellationToken ct)
    {
        var latestLogs = await _dbContext.IngestionSyncLogs
            .AsNoTracking()
            .GroupBy(x => x.SchoolId)
            .Select(g => g.OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc).First())
            .ToListAsync(ct);

        var schoolIds = latestLogs.Select(x => x.SchoolId).ToList();
        var schools = await _dbContext.Schools
            .AsNoTracking()
            .Where(s => schoolIds.Contains(s.SchoolId))
            .Select(s => new { s.SchoolId, s.Name, s.SyncStatus, s.SyncErrorCount })
            .ToDictionaryAsync(s => s.SchoolId, ct);

        var response = latestLogs
            .Select(log => new IngestionHealthDto(
                log.SchoolId,
                schools.TryGetValue(log.SchoolId, out var school) ? school.Name : "Unknown",
                schools.TryGetValue(log.SchoolId, out var school2) ? school2.SyncStatus.ToString() : "Unknown",
                schools.TryGetValue(log.SchoolId, out var school3) ? school3.SyncErrorCount : 0,
                log.Status,
                log.StartedAtUtc,
                log.CompletedAtUtc,
                log.RecordsProcessed,
                log.ErrorCount,
                log.MismatchCount,
                log.Notes))
            .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc)
            .ToList();

        return Ok(response);
    }

    [HttpPut("health/{schoolId:guid}/resume")]
    public async Task<IActionResult> ResumeMessaging(Guid schoolId, CancellationToken ct)
    {
        var school = await _dbContext.Schools.FirstOrDefaultAsync(s => s.SchoolId == schoolId, ct);
        if (school == null)
        {
            return NotFound();
        }

        school.SyncStatus = SyncStatus.Healthy;
        school.SyncErrorCount = 0;
        await _dbContext.SaveChangesAsync(ct);
        return Ok();
    }
}

public sealed record IngestionHealthDto(
    Guid SchoolId,
    string SchoolName,
    string SyncStatus,
    int SyncErrorCount,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    int RecordsProcessed,
    int ErrorCount,
    int MismatchCount,
    string? Notes);
