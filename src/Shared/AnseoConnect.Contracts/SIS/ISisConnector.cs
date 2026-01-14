namespace AnseoConnect.Contracts.SIS;

/// <summary>
/// Interface for SIS connector implementations.
/// </summary>
public interface ISisConnector
{
    /// <summary>
    /// Gets the unique provider identifier (e.g., "WONDE", "TYRO", "VSWARE").
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Gets the set of capabilities this connector supports.
    /// </summary>
    IReadOnlySet<SisCapability> Capabilities { get; }

    /// <summary>
    /// Syncs student roster data.
    /// </summary>
    Task<SyncRunResult> SyncRosterAsync(Guid schoolId, SyncOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs guardian/contact data.
    /// </summary>
    Task<SyncRunResult> SyncContactsAsync(Guid schoolId, SyncOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs attendance marks for a specific date.
    /// </summary>
    Task<SyncRunResult> SyncAttendanceAsync(Guid schoolId, DateOnly date, SyncOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs class/group information.
    /// </summary>
    Task<SyncRunResult> SyncClassesAsync(Guid schoolId, SyncOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs timetable/period information.
    /// </summary>
    Task<SyncRunResult> SyncTimetableAsync(Guid schoolId, SyncOptions options, CancellationToken cancellationToken = default);
}
