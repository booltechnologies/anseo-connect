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
    string ConsentEmail,
    string? QuietHoursJson = null);

public sealed record StudentProfile(
    StudentSummary Summary,
    IReadOnlyList<GuardianContact> Guardians,
    IReadOnlyList<CaseDto> Cases,
    IReadOnlyList<MessageSummary> Messages);

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

public sealed record IngestionHealthDto(
    Guid SchoolId,
    string SchoolName,
    string SyncStatus,
    int SyncErrorCount,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    int RecordsProcessed,
    int ErrorCount,
    int MismatchCount,
    string? Notes);

public sealed record NotificationDto(
    Guid NotificationId,
    string Type,
    string Payload,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);

public sealed record EligibleStudentDto(
    Guid StudentId,
    string StudentName,
    string YearGroup,
    Guid RuleSetId,
    string RuleSetName,
    IReadOnlyList<string> TriggeredConditions);

public sealed record InterventionRuleSetDto(Guid RuleSetId, string Name, string Jurisdiction, bool IsActive);

public sealed record InterventionMeetingDto(
    Guid MeetingId,
    Guid InstanceId,
    Guid StageId,
    string StageName,
    Guid StudentId,
    string StudentName,
    DateTimeOffset ScheduledAtUtc,
    DateTimeOffset? HeldAtUtc,
    string Status,
    string? AttendeesJson,
    string? OutcomeCode,
    string? OutcomeNotes);

public sealed record MeetingOutcomeRequest(string Status, string? OutcomeCode, string? OutcomeNotes, string? NotesJson);

public sealed record StudentInterventionInstanceDto(
    Guid InstanceId,
    Guid StudentId,
    Guid CaseId,
    Guid RuleSetId,
    Guid CurrentStageId,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? LastStageAtUtc);

public sealed record InterventionStageSummaryDto(Guid StageId, string StageType, Guid? LetterTemplateId);

public sealed record GuardianSummaryDto(Guid GuardianId, string FullName);

public sealed record InterventionEventDto(Guid EventId, Guid StageId, string StageName, string EventType, DateTimeOffset OccurredAtUtc);

public sealed record InterventionInstanceDetailDto(
    StudentInterventionInstanceDto Instance,
    string StudentName,
    InterventionStageSummaryDto? CurrentStage,
    IReadOnlyList<GuardianSummaryDto> Guardians,
    IReadOnlyList<InterventionEventDto> Events);

public sealed record PlaybookDefinitionDto(
    Guid PlaybookId,
    string Name,
    string Description,
    string TriggerStageType,
    bool IsActive,
    int EscalationAfterDays,
    string StopConditionsJson,
    string EscalationConditionsJson);

public sealed record PlaybookStepDto(
    Guid StepId,
    Guid PlaybookId,
    int Order,
    int OffsetDays,
    string Channel,
    string? TemplateKey,
    string? FallbackChannel,
    bool SkipIfPreviousReplied);

public sealed record PlaybookRunDto(
    Guid RunId,
    Guid PlaybookId,
    Guid InstanceId,
    Guid StudentId,
    Guid? GuardianId,
    string Status,
    DateTimeOffset TriggeredAtUtc,
    DateTimeOffset? StoppedAtUtc,
    string? StopReason,
    int CurrentStepOrder,
    DateTimeOffset? NextStepScheduledAtUtc);

public sealed record PlaybookExecutionLogDto(
    Guid LogId,
    Guid RunId,
    Guid StepId,
    string Channel,
    Guid? OutboxMessageId,
    string Status,
    string? SkipReason,
    DateTimeOffset ScheduledForUtc,
    DateTimeOffset? ExecutedAtUtc);

public sealed record AutomationMetricsDto(
    Guid MetricsId,
    Guid TenantId,
    Guid SchoolId,
    DateOnly Date,
    int PlaybooksStarted,
    int StepsScheduled,
    int StepsSent,
    int PlaybooksStoppedByReply,
    int PlaybooksStoppedByImprovement,
    int Escalations,
    decimal EstimatedMinutesSaved,
    decimal AttendanceImprovementDelta);

public sealed record RoiSummaryDto(
    int TotalPlaybooksRun,
    int TotalTouchesSent,
    decimal EstimatedHoursSaved,
    int StudentsReachedByAutomation,
    int StudentsImprovedAfterPlaybook,
    decimal AvgAttendanceChangePercent);

public sealed record PlaybookDetailDto(
    PlaybookDefinitionDto Playbook,
    IReadOnlyList<PlaybookStepDto> Steps);

public sealed record PlaybookRunDetailDto(
    PlaybookRunDto Run,
    IReadOnlyList<PlaybookExecutionLogDto> Logs);

public sealed record LetterTemplateDto(Guid TemplateId, string TemplateKey, int Version, string Status, string LockScope);

public sealed record GenerateLetterRequest(
    Guid InstanceId,
    Guid StageId,
    Guid GuardianId,
    string LanguageCode,
    Dictionary<string, string> MergeData);

public sealed record LetterArtifactDto(
    Guid ArtifactId,
    Guid InstanceId,
    Guid StageId,
    Guid TemplateId,
    int TemplateVersion,
    Guid GuardianId,
    string LanguageCode,
    string StoragePath,
    string ContentHash,
    string MergeDataJson,
    DateTimeOffset GeneratedAtUtc);

public sealed record InterventionAnalyticsDto(
    Guid AnalyticsId,
    DateOnly Date,
    int TotalStudents,
    int StudentsInIntervention,
    int Letter1Sent,
    int Letter2Sent,
    int MeetingsScheduled,
    int MeetingsHeld,
    int Escalated,
    int Resolved,
    decimal PreInterventionAttendanceAvg,
    decimal PostInterventionAttendanceAvg);

public sealed record AnalyticsTrendDto(IReadOnlyList<DailyAnalyticsPoint> Points);

public sealed record DailyAnalyticsPoint(
    DateOnly Date,
    decimal AttendanceRate,
    int StudentsInIntervention,
    int Letter1Sent,
    int Letter2Sent,
    int MeetingsHeld);

public sealed record ReportDefinitionDto(
    Guid DefinitionId,
    string Name,
    string ReportType,
    string ScheduleCron,
    bool IsActive);

public sealed record ReportRunDto(
    Guid RunId,
    Guid DefinitionId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string Status,
    string? ErrorMessage,
    Guid? ArtifactId = null);
