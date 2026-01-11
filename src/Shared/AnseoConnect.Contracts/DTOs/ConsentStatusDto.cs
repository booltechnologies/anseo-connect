namespace AnseoConnect.Contracts.DTOs;

/// <summary>
/// DTO for guardian consent status.
/// </summary>
public sealed record ConsentStatusDto(
    Guid GuardianId,
    string GuardianName,
    string Channel,
    string State,
    DateTimeOffset LastUpdatedUtc,
    string Source
);
