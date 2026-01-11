namespace AnseoConnect.Contracts.DTOs;

/// <summary>
/// DTO for Case entity.
/// </summary>
public sealed record CaseDto(
    Guid CaseId,
    Guid StudentId,
    string StudentName,
    string CaseType,
    int Tier,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    List<CaseTimelineEventDto> TimelineEvents
);

public sealed record CaseTimelineEventDto(
    Guid EventId,
    Guid CaseId,
    string EventType,
    string? EventData,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy
);
