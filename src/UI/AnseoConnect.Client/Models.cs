using AnseoConnect.Contracts.DTOs;

namespace AnseoConnect.Client.Models;

public sealed record LoginRequest(string Username, string Password, Guid? TenantId = null);

public sealed record LoginResponse(
    string Token,
    Guid UserId,
    Guid TenantId,
    Guid SchoolId,
    string? Username,
    string? Email);

public sealed record WhoAmIResponse(
    Guid UserId,
    Guid TenantId,
    Guid? SchoolId,
    string? Username,
    string? Email,
    string FirstName,
    string LastName);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Skip, int Take, bool HasMore);

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

public sealed record StudentSummary(
    Guid StudentId,
    string Name,
    string ExternalId,
    string YearGroup,
    string Risk,
    double AttendanceRate);

public sealed record GuardianContact(
    Guid GuardianId,
    string Name,
    string Relationship,
    string Mobile,
    string Email,
    string ConsentSms,
    string ConsentEmail);

public sealed record StudentProfile(
    StudentSummary Summary,
    IReadOnlyList<GuardianContact> Guardians,
    IReadOnlyList<CaseDto> Cases,
    IReadOnlyList<MessageSummary> Messages);

public sealed record SchoolSettingsDto(
    string Timezone,
    string AmCutoff,
    string PmCutoff,
    string ChannelOrder,
    string PolicyPackVersion);

public sealed record IntegrationStatusDto(
    string Name,
    string Status,
    string? Detail,
    DateTimeOffset? LastSyncAtUtc);

public sealed record PolicyPackAssignmentDto(string PackName, string Version, string? Note);

public sealed record ChecklistItemDto(
    string Id,
    string Title,
    bool Required,
    string Status,
    string? Notes = null);

public sealed record CaseDetailDto(
    CaseDto Case,
    IReadOnlyList<ChecklistItemDto> Checklist);

public sealed record MessageComposeRequest(
    Guid StudentId,
    IReadOnlyList<Guid> GuardianIds,
    string Channel,
    string Template,
    string BodyPreview,
    string? OtherGuardian = null);

public sealed record TodayDashboardDto(
    DateOnly Date,
    IReadOnlyList<AbsenceDto> Absences,
    IReadOnlyList<TaskSummary> Tasks,
    IReadOnlyList<SafeguardingAlertSummary> SafeguardingAlerts,
    IReadOnlyList<MessageSummary>? Failures = null,
    IReadOnlyList<GuardianContact>? MissingContacts = null);
