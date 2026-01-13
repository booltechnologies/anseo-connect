using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Compares external SIS marks vs internal (e.g., RFID) marks to flag mismatches.
/// Minimal v0.1: compares by student/date/session and reports counts.
/// </summary>
public sealed class AttendanceReconciliationService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<AttendanceReconciliationService> _logger;

    public AttendanceReconciliationService(AnseoConnectDbContext dbContext, ILogger<AttendanceReconciliationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ReconciliationResult> ReconcileAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        // Placeholder: in a future version, RFID marks would be a separate source.
        // For now, treat AttendanceMarks.Source != 'WONDE' as "RFID/other" and compare to WONDE.
        var wondeMarks = await _dbContext.AttendanceMarks
            .AsNoTracking()
            .Where(m => m.Date == date && m.Source == "WONDE")
            .Select(m => new { m.StudentId, m.Session, m.Status })
            .ToListAsync(cancellationToken);

        var otherMarks = await _dbContext.AttendanceMarks
            .AsNoTracking()
            .Where(m => m.Date == date && m.Source != "WONDE")
            .Select(m => new { m.StudentId, m.Session, m.Status, m.Source })
            .ToListAsync(cancellationToken);

        var mismatches = new List<ReconciliationMismatch>();
        var wondeLookup = wondeMarks.ToDictionary(
            k => (k.StudentId, k.Session),
            v => v.Status,
            comparer: new ValueTupleComparer<Guid, string>());

        foreach (var other in otherMarks)
        {
            var key = (other.StudentId, other.Session);
            if (wondeLookup.TryGetValue(key, out var wondeStatus))
            {
                if (!string.Equals(wondeStatus, other.Status, StringComparison.OrdinalIgnoreCase))
                {
                    mismatches.Add(new ReconciliationMismatch(other.StudentId, other.Session, wondeStatus, other.Status, other.Source));
                }
            }
            else
            {
                mismatches.Add(new ReconciliationMismatch(other.StudentId, other.Session, "MISSING_WONDE", other.Status, other.Source));
            }
        }

        _logger.LogInformation("Reconciliation for {Date}: {MismatchCount} mismatches", date, mismatches.Count);

        return new ReconciliationResult(date, wondeMarks.Count, otherMarks.Count, mismatches.Count, mismatches);
    }

    private sealed class ValueTupleComparer<T1, T2> : IEqualityComparer<(T1, T2)>
        where T1 : notnull where T2 : notnull
    {
        public bool Equals((T1, T2) x, (T1, T2) y) => EqualityComparer<T1>.Default.Equals(x.Item1, y.Item1) && EqualityComparer<T2>.Default.Equals(x.Item2, y.Item2);
        public int GetHashCode((T1, T2) obj) => HashCode.Combine(obj.Item1, obj.Item2);
    }
}

public sealed record ReconciliationMismatch(Guid StudentId, string Session, string ExpectedStatus, string ActualStatus, string Source);
public sealed record ReconciliationResult(DateOnly Date, int WonDeCount, int OtherCount, int MismatchCount, IReadOnlyList<ReconciliationMismatch> Mismatches);
