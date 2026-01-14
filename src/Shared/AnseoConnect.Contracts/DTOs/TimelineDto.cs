namespace AnseoConnect.Contracts.DTOs;

/// <summary>
/// DTO for TimelineEvent entity.
/// </summary>
public sealed record TimelineEventDto(
    Guid EventId,
    Guid StudentId,
    Guid? CaseId,
    string EventType,
    string Category,
    DateTimeOffset OccurredAtUtc,
    string? ActorName,
    string? Title,
    string? Summary,
    string? MetadataJson,
    string VisibilityScope);

/// <summary>
/// Filter options for timeline queries.
/// </summary>
public sealed record TimelineFilter(
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    IReadOnlyList<string>? Categories,
    IReadOnlyList<string>? EventTypes,
    Guid? CaseId,
    int Skip = 0,
    int Take = 50);

/// <summary>
/// Export options for timeline export.
/// </summary>
public sealed record ExportOptions(
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    IReadOnlyList<string>? Categories,
    bool IncludeRedacted = false,
    string Format = "PDF");
