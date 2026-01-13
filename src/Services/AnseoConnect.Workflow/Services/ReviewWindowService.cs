using AnseoConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Provides per-tier review window handling for cases.
/// </summary>
public sealed class ReviewWindowService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<ReviewWindowService> _logger;

    public ReviewWindowService(AnseoConnectDbContext dbContext, ILogger<ReviewWindowService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SetReviewWindowAsync(Guid caseId, int daysFromNow, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Cases.FirstOrDefaultAsync(c => c.CaseId == caseId, cancellationToken);
        if (entity == null)
        {
            return;
        }

        entity.ReviewDueAtUtc = DateTimeOffset.UtcNow.AddDays(daysFromNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetOverdueCaseIdsAsync(DateTimeOffset asOfUtc, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Cases
            .AsNoTracking()
            .Where(c => c.Status == "OPEN" && c.ReviewDueAtUtc != null && c.ReviewDueAtUtc < asOfUtc)
            .Select(c => c.CaseId)
            .ToListAsync(cancellationToken);
    }
}
