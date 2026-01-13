using AnseoConnect.Contracts.DTOs;

namespace AnseoConnect.ApiGateway.Models;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Skip, int Take, bool HasMore);

public sealed record MessageSummary(
    Guid MessageId,
    Guid StudentId,
    string StudentName,
    string Channel,
    string Status,
    string MessageType,
    DateTimeOffset CreatedAtUtc,
    string? ProviderMessageId,
    string? Subject);

public sealed record MessageTimelineEvent(
    DateTimeOffset TimestampUtc,
    string EventType,
    string? Detail);

public sealed record MessageDetail(
    MessageSummary Summary,
    string Body,
    string Template,
    IReadOnlyList<KeyValuePair<string, string>> Tokens,
    IReadOnlyList<MessageTimelineEvent> Timeline,
    IReadOnlyList<string>? Recipients = null,
    string? ConsentStatus = null,
    string? ConsentSource = null);

public sealed record GuardianContact(
    Guid GuardianId,
    string Name,
    string Relationship,
    string Mobile,
    string Email,
    string ConsentSms,
    string ConsentEmail,
    string? QuietHoursJson = null);

public sealed record StudentSummary(
    Guid StudentId,
    string Name,
    string ExternalId,
    string YearGroup,
    string Risk,
    double AttendanceRate);

public sealed record StudentProfile(
    StudentSummary Summary,
    IReadOnlyList<GuardianContact> Guardians,
    IReadOnlyList<CaseDto> Cases,
    IReadOnlyList<MessageSummary> Messages);

public sealed record TaskSummary(
    string Title,
    DateTimeOffset DueAtUtc,
    string Category,
    string Status,
    Guid? CaseId = null);

public sealed record SafeguardingAlertSummary(
    Guid AlertId,
    Guid StudentId,
    string StudentName,
    string Severity,
    string TriggerSummary,
    DateTimeOffset CreatedAtUtc,
    string? AcknowledgedBy,
    DateTimeOffset? AcknowledgedAtUtc);

public sealed record TodayDashboardDto(
    DateOnly Date,
    IReadOnlyList<AbsenceDto> Absences,
    IReadOnlyList<TaskSummary> Tasks,
    IReadOnlyList<SafeguardingAlertSummary> SafeguardingAlerts,
    IReadOnlyList<MessageSummary>? Failures = null,
    IReadOnlyList<GuardianContact>? MissingContacts = null);

public sealed class SchoolSettingsDto
{
    public string Timezone { get; set; } = string.Empty;
    public string AmCutoff { get; set; } = string.Empty;
    public string PmCutoff { get; set; } = string.Empty;
    public string ChannelOrder { get; set; } = string.Empty;
    public string PolicyPackVersion { get; set; } = string.Empty;
    public bool TranslationReviewRequired { get; set; }
}

public sealed record IntegrationStatusDto(
    string Name,
    string Status,
    string? Detail,
    DateTimeOffset? LastSyncAtUtc);

public sealed record PolicyPackAssignmentDto(string PackName, string Version, string? Note);

public sealed record MessageComposeRequest(
    Guid StudentId,
    IReadOnlyList<Guid> GuardianIds,
    string Channel,
    string Template,
    string BodyPreview,
    string? OtherGuardian = null);

public sealed record ChecklistItemDto(
    string Id,
    string Title,
    bool Required,
    string Status,
    string? Notes = null);

public sealed record CaseDetailDto(
    CaseDto Case,
    IReadOnlyList<ChecklistItemDto> Checklist);
