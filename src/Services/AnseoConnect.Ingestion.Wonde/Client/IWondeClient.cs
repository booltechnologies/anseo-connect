namespace AnseoConnect.Ingestion.Wonde.Client;

/// <summary>
/// Client interface for Wonde API operations.
/// </summary>
public interface IWondeClient
{
    /// <summary>
    /// Gets school information including region/domain.
    /// </summary>
    Task<WondeSchoolResponse?> GetSchoolAsync(string schoolId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated list of students.
    /// </summary>
    Task<WondePagedResponse<WondeStudent>> GetStudentsAsync(
        string schoolId,
        DateTimeOffset? updatedAfter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated list of contacts (guardians).
    /// </summary>
    Task<WondePagedResponse<WondeContact>> GetContactsAsync(
        string schoolId,
        DateTimeOffset? updatedAfter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets attendance marks for a specific date.
    /// </summary>
    Task<WondePagedResponse<WondeAttendance>> GetAttendanceAsync(
        string schoolId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets student absence records.
    /// </summary>
    Task<WondePagedResponse<WondeStudentAbsence>> GetStudentAbsencesAsync(
        string schoolId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);
}
