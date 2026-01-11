namespace AnseoConnect.Contracts.DTOs;

/// <summary>
/// DTO for unexplained absence.
/// </summary>
public sealed record AbsenceDto(
    Guid StudentId,
    string StudentName,
    DateOnly Date,
    string Session,
    string? ReasonCode
);
