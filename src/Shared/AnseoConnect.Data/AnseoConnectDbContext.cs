using System.Linq.Expressions;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using AnseoConnect.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace AnseoConnect.Data;

public sealed class AnseoConnectDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    private readonly ITenantContext _tenant;

    public AnseoConnectDbContext(DbContextOptions<AnseoConnectDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
    public DbSet<AttendanceMark> AttendanceMarks => Set<AttendanceMark>();
    public DbSet<AttendanceDailySummary> AttendanceDailySummaries => Set<AttendanceDailySummary>();
    public DbSet<InterventionRuleSet> InterventionRuleSets => Set<InterventionRuleSet>();
    public DbSet<InterventionStage> InterventionStages => Set<InterventionStage>();
    public DbSet<StudentInterventionInstance> StudentInterventionInstances => Set<StudentInterventionInstance>();
    public DbSet<InterventionEvent> InterventionEvents => Set<InterventionEvent>();
    public DbSet<LetterTemplate> LetterTemplates => Set<LetterTemplate>();
    public DbSet<LetterArtifact> LetterArtifacts => Set<LetterArtifact>();
    public DbSet<InterventionMeeting> InterventionMeetings => Set<InterventionMeeting>();
    public DbSet<InterventionAnalytics> InterventionAnalytics => Set<InterventionAnalytics>();
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<ReportRun> ReportRuns => Set<ReportRun>();
    public DbSet<ReportArtifact> ReportArtifacts => Set<ReportArtifact>();
    public DbSet<JobLock> JobLocks => Set<JobLock>();
    public DbSet<ConsentState> ConsentStates => Set<ConsentState>();
    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();
    public DbSet<ContactPreference> ContactPreferences => Set<ContactPreference>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageLocalizedText> MessageLocalizedTexts => Set<MessageLocalizedText>();
    public DbSet<MessageDeliveryAttempt> MessageDeliveryAttempts => Set<MessageDeliveryAttempt>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ConversationThread> ConversationThreads => Set<ConversationThread>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseTimelineEvent> CaseTimelineEvents => Set<CaseTimelineEvent>();
    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();
    public DbSet<SafeguardingAlert> SafeguardingAlerts => Set<SafeguardingAlert>();
    public DbSet<SchoolSettings> SchoolSettings => Set<SchoolSettings>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<WorkTaskChecklist> WorkTaskChecklists => Set<WorkTaskChecklist>();
    public DbSet<ChecklistCompletion> ChecklistCompletions => Set<ChecklistCompletion>();
    public DbSet<ReasonCode> ReasonCodes => Set<ReasonCode>();
    public DbSet<EvidencePack> EvidencePacks => Set<EvidencePack>();
    public DbSet<IngestionSyncLog> IngestionSyncLogs => Set<IngestionSyncLog>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
    public DbSet<ETBTrust> ETBTrusts => Set<ETBTrust>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AudienceSegment> AudienceSegments => Set<AudienceSegment>();
    public DbSet<AudienceSnapshot> AudienceSnapshots => Set<AudienceSnapshot>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<EngagementEvent> EngagementEvents => Set<EngagementEvent>();
    public DbSet<GuardianReachability> GuardianReachabilities => Set<GuardianReachability>();
    public DbSet<TranslationCache> TranslationCaches => Set<TranslationCache>();
    public DbSet<PlaybookDefinition> PlaybookDefinitions => Set<PlaybookDefinition>();
    public DbSet<PlaybookStep> PlaybookSteps => Set<PlaybookStep>();
    public DbSet<PlaybookRun> PlaybookRuns => Set<PlaybookRun>();
    public DbSet<PlaybookExecutionLog> PlaybookExecutionLogs => Set<PlaybookExecutionLog>();
    public DbSet<TelemetryEvent> TelemetryEvents => Set<TelemetryEvent>();
    public DbSet<AutomationMetrics> AutomationMetrics => Set<AutomationMetrics>();
    public DbSet<MtssTierDefinition> MtssTierDefinitions => Set<MtssTierDefinition>();
    public DbSet<MtssIntervention> MtssInterventions => Set<MtssIntervention>();
    public DbSet<TierAssignment> TierAssignments => Set<TierAssignment>();
    public DbSet<TierAssignmentHistory> TierAssignmentHistories => Set<TierAssignmentHistory>();
    public DbSet<CaseIntervention> CaseInterventions => Set<CaseIntervention>();
    public DbSet<SyncRun> SyncRuns => Set<SyncRun>();
    public DbSet<SyncMetric> SyncMetrics => Set<SyncMetric>();
    public DbSet<SyncError> SyncErrors => Set<SyncError>();
    public DbSet<SyncPayloadArchive> SyncPayloadArchives => Set<SyncPayloadArchive>();
    public DbSet<ClassGroup> ClassGroups => Set<ClassGroup>();
    public DbSet<StudentClassEnrollment> StudentClassEnrollments => Set<StudentClassEnrollment>();
    public DbSet<ReasonCodeMapping> ReasonCodeMappings => Set<ReasonCodeMapping>();
    public DbSet<SchoolSyncState> SchoolSyncStates => Set<SchoolSyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Identity tables
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUsers");
            entity.HasIndex(x => new { x.TenantId, x.SchoolId, x.NormalizedUserName })
                .IsUnique()
                .HasDatabaseName("IX_AppUsers_Tenant_School_UserName");
            entity.HasIndex(x => new { x.TenantId, x.SchoolId, x.NormalizedEmail })
                .HasDatabaseName("IX_AppUsers_Tenant_School_Email");
        });

        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("AppRoles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("AppUserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("AppUserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("AppUserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("AppUserTokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("AppRoleClaims");

        ApplyTenantAndSchoolFilters(modelBuilder);
        
        modelBuilder.Entity<Tenant>().HasKey(x => x.TenantId);

        modelBuilder.Entity<School>().HasKey(x => x.SchoolId);
        modelBuilder.Entity<School>()
            .HasIndex(x => new { x.TenantId, x.WondeSchoolId })
            .HasDatabaseName("IX_Schools_Tenant_WondeSchoolId");
        modelBuilder.Entity<School>()
            .HasOne(x => x.ETBTrust)
            .WithMany(x => x.Schools)
            .HasForeignKey(x => x.ETBTrustId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Student>().HasKey(x => x.StudentId);
        modelBuilder.Entity<Student>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.ExternalStudentId })
            .IsUnique();

        modelBuilder.Entity<Guardian>().HasKey(x => x.GuardianId);
        modelBuilder.Entity<Guardian>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.ExternalGuardianId })
            .IsUnique();

        modelBuilder.Entity<StudentGuardian>().HasKey(x => new { x.StudentId, x.GuardianId });
        modelBuilder.Entity<StudentGuardian>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.StudentId });

        modelBuilder.Entity<JobLock>().HasKey(x => x.LockName);
        modelBuilder.Entity<JobLock>()
            .Property(x => x.LockName)
            .HasMaxLength(128);
        modelBuilder.Entity<JobLock>()
            .Property(x => x.HolderInstanceId)
            .HasMaxLength(128);

        modelBuilder.Entity<AttendanceMark>().HasKey(x => x.AttendanceMarkId);
        modelBuilder.Entity<AttendanceMark>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.StudentId, x.Date, x.Session })
            .IsUnique();
        modelBuilder.Entity<AttendanceMark>()
            .Property(x => x.RawPayloadJson)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<AttendanceDailySummary>().HasKey(x => x.SummaryId);
        modelBuilder.Entity<AttendanceDailySummary>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.StudentId, x.Date })
            .IsUnique()
            .HasDatabaseName("IX_AttendanceDailySummary_Student_Date");
        modelBuilder.Entity<AttendanceDailySummary>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.Date })
            .HasDatabaseName("IX_AttendanceDailySummary_Date");
        modelBuilder.Entity<AttendanceDailySummary>()
            .HasIndex(x => x.StudentId);
        modelBuilder.Entity<AttendanceDailySummary>()
            .Property(x => x.AMStatus)
            .HasMaxLength(32);
        modelBuilder.Entity<AttendanceDailySummary>()
            .Property(x => x.PMStatus)
            .HasMaxLength(32);
        modelBuilder.Entity<AttendanceDailySummary>()
            .Property(x => x.AMReasonCode)
            .HasMaxLength(64);
        modelBuilder.Entity<AttendanceDailySummary>()
            .Property(x => x.PMReasonCode)
            .HasMaxLength(64);
        modelBuilder.Entity<AttendanceDailySummary>()
            .Property(x => x.AttendancePercent)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AttendanceDailySummary>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InterventionRuleSet>().HasKey(x => x.RuleSetId);
        modelBuilder.Entity<InterventionRuleSet>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.IsActive })
            .HasDatabaseName("IX_InterventionRuleSets_Active");
        modelBuilder.Entity<InterventionRuleSet>()
            .Property(x => x.Name)
            .HasMaxLength(256);
        modelBuilder.Entity<InterventionRuleSet>()
            .Property(x => x.Jurisdiction)
            .HasMaxLength(32);

        modelBuilder.Entity<InterventionStage>().HasKey(x => x.StageId);
        modelBuilder.Entity<InterventionStage>()
            .HasIndex(x => new { x.TenantId, x.RuleSetId, x.Order })
            .IsUnique()
            .HasDatabaseName("IX_InterventionStages_RuleSet_Order");
        modelBuilder.Entity<InterventionStage>()
            .Property(x => x.StageType)
            .HasMaxLength(64);
        modelBuilder.Entity<InterventionStage>()
            .HasIndex(x => x.RuleSetId);
        modelBuilder.Entity<InterventionStage>()
            .HasOne(x => x.RuleSet)
            .WithMany()
            .HasForeignKey(x => x.RuleSetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentInterventionInstance>().HasKey(x => x.InstanceId);
        modelBuilder.Entity<StudentInterventionInstance>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.StudentId, x.Status })
            .HasDatabaseName("IX_InterventionInstances_Student_Status");
        modelBuilder.Entity<StudentInterventionInstance>()
            .HasIndex(x => new { x.TenantId, x.RuleSetId, x.Status })
            .HasDatabaseName("IX_InterventionInstances_RuleSet_Status");
        modelBuilder.Entity<StudentInterventionInstance>()
            .Property(x => x.Status)
            .HasMaxLength(32);
        modelBuilder.Entity<StudentInterventionInstance>()
            .HasOne(x => x.Case)
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InterventionEvent>().HasKey(x => x.EventId);
        modelBuilder.Entity<InterventionEvent>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.InstanceId, x.OccurredAtUtc })
            .HasDatabaseName("IX_InterventionEvents_Instance_Time");
        modelBuilder.Entity<InterventionEvent>()
            .Property(x => x.EventType)
            .HasMaxLength(64);

        modelBuilder.Entity<LetterTemplate>().HasKey(x => x.TemplateId);
        modelBuilder.Entity<LetterTemplate>()
            .HasIndex(x => new { x.TenantId, x.TemplateKey, x.Version })
            .IsUnique()
            .HasDatabaseName("IX_LetterTemplates_Key_Version");
        modelBuilder.Entity<LetterTemplate>()
            .Property(x => x.TemplateKey)
            .HasMaxLength(128);
        modelBuilder.Entity<LetterTemplate>()
            .Property(x => x.Status)
            .HasMaxLength(32);
        modelBuilder.Entity<LetterTemplate>()
            .Property(x => x.LockScope)
            .HasMaxLength(64);
        modelBuilder.Entity<LetterTemplate>()
            .Property(x => x.ApprovedBy)
            .HasMaxLength(256);

        modelBuilder.Entity<LetterArtifact>().HasKey(x => x.ArtifactId);
        modelBuilder.Entity<LetterArtifact>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.InstanceId, x.StageId })
            .HasDatabaseName("IX_LetterArtifacts_Instance_Stage");
        modelBuilder.Entity<LetterArtifact>()
            .Property(x => x.LanguageCode)
            .HasMaxLength(10);
        modelBuilder.Entity<LetterArtifact>()
            .Property(x => x.ContentHash)
            .HasMaxLength(256);

        modelBuilder.Entity<InterventionMeeting>().HasKey(x => x.MeetingId);
        modelBuilder.Entity<InterventionMeeting>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.InstanceId, x.Status })
            .HasDatabaseName("IX_InterventionMeetings_Instance_Status");
        modelBuilder.Entity<InterventionMeeting>()
            .Property(x => x.Status)
            .HasMaxLength(32);
        modelBuilder.Entity<InterventionMeeting>()
            .Property(x => x.OutcomeCode)
            .HasMaxLength(64);

        modelBuilder.Entity<InterventionAnalytics>().HasKey(x => x.AnalyticsId);
        modelBuilder.Entity<InterventionAnalytics>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.Date })
            .IsUnique()
            .HasDatabaseName("IX_InterventionAnalytics_Date");
        modelBuilder.Entity<InterventionAnalytics>()
            .Property(x => x.PreInterventionAttendanceAvg)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<InterventionAnalytics>()
            .Property(x => x.PostInterventionAttendanceAvg)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<ReportDefinition>().HasKey(x => x.DefinitionId);
        modelBuilder.Entity<ReportDefinition>()
            .HasIndex(x => new { x.TenantId, x.ReportType, x.IsActive })
            .HasDatabaseName("IX_ReportDefinitions_Type_Active");
        modelBuilder.Entity<ReportDefinition>()
            .Property(x => x.Name)
            .HasMaxLength(256);
        modelBuilder.Entity<ReportDefinition>()
            .Property(x => x.ReportType)
            .HasMaxLength(64);
        modelBuilder.Entity<ReportDefinition>()
            .Property(x => x.ScheduleCron)
            .HasMaxLength(64);

        modelBuilder.Entity<ReportRun>().HasKey(x => x.RunId);
        modelBuilder.Entity<ReportRun>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.DefinitionId, x.Status })
            .HasDatabaseName("IX_ReportRuns_Definition_Status");
        modelBuilder.Entity<ReportRun>()
            .Property(x => x.Status)
            .HasMaxLength(32);

        modelBuilder.Entity<ReportArtifact>().HasKey(x => x.ArtifactId);
        modelBuilder.Entity<ReportArtifact>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.RunId })
            .HasDatabaseName("IX_ReportArtifacts_Run");
        modelBuilder.Entity<ReportArtifact>()
            .Property(x => x.Format)
            .HasMaxLength(16);
        modelBuilder.Entity<ReportArtifact>()
            .Property(x => x.DataSnapshotHash)
            .HasMaxLength(256);

        modelBuilder.Entity<StudentGuardian>()
            .HasOne(x => x.Student)
            .WithMany(x => x.StudentGuardians)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade); // keep

        modelBuilder.Entity<StudentGuardian>()
            .HasOne(x => x.Guardian)
            .WithMany(x => x.StudentGuardians)
            .HasForeignKey(x => x.GuardianId)
            .OnDelete(DeleteBehavior.NoAction); // IMPORTANT

        modelBuilder.Entity<ConsentState>().HasKey(x => x.ConsentStateId);
        modelBuilder.Entity<ConsentState>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.GuardianId, x.Channel })
            .IsUnique()
            .HasDatabaseName("IX_ConsentStates_Tenant_School_Guardian_Channel");
        modelBuilder.Entity<ConsentRecord>().HasKey(x => x.ConsentRecordId);
        modelBuilder.Entity<ConsentRecord>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.GuardianId, x.Channel, x.CapturedAtUtc })
            .HasDatabaseName("IX_ConsentRecords_Guardian_Channel_Captured");
        modelBuilder.Entity<ContactPreference>().HasKey(x => x.ContactPreferenceId);
        modelBuilder.Entity<ContactPreference>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.GuardianId })
            .IsUnique()
            .HasDatabaseName("IX_ContactPreferences_Guardian");

        modelBuilder.Entity<Message>().HasKey(x => x.MessageId);
        modelBuilder.Entity<Message>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.CaseId, x.CreatedAtUtc })
            .HasDatabaseName("IX_Messages_Tenant_School_Case_Created");
        modelBuilder.Entity<Message>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.GuardianId, x.CreatedAtUtc })
            .HasDatabaseName("IX_Messages_Tenant_School_Guardian_Created");
        modelBuilder.Entity<Message>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.ThreadId, x.CreatedAtUtc })
            .HasDatabaseName("IX_Messages_Tenant_School_Thread_Created");
        modelBuilder.Entity<Message>()
            .HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .HasDatabaseName("IX_Messages_Tenant_Idempotency");
        modelBuilder.Entity<Message>()
            .HasOne(x => x.Thread)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ThreadId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<MessageLocalizedText>().HasKey(x => x.LocalizedTextId);
        modelBuilder.Entity<MessageLocalizedText>()
            .HasIndex(x => new { x.MessageId, x.LanguageCode, x.IsOriginal })
            .HasDatabaseName("IX_MessageLocalizedTexts_Message_Language");
        modelBuilder.Entity<MessageLocalizedText>()
            .HasOne(x => x.Message)
            .WithMany(x => x.LocalizedTexts)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MessageDeliveryAttempt>().HasKey(x => x.AttemptId);
        modelBuilder.Entity<MessageDeliveryAttempt>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.MessageId, x.AttemptedAtUtc })
            .HasDatabaseName("IX_MessageDeliveryAttempts_Message_Attempted");
        modelBuilder.Entity<MessageDeliveryAttempt>()
            .HasOne(x => x.Message)
            .WithMany(x => x.DeliveryAttempts)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Attachment>().HasKey(x => x.AttachmentId);
        modelBuilder.Entity<Attachment>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.MessageId })
            .HasDatabaseName("IX_Attachments_Message");
        modelBuilder.Entity<Attachment>()
            .HasOne(x => x.Message)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConversationThread>().HasKey(x => x.ThreadId);
        modelBuilder.Entity<ConversationThread>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.StudentId, x.LastActivityUtc })
            .HasDatabaseName("IX_ConversationThreads_Student_LastActivity");
        modelBuilder.Entity<ConversationParticipant>().HasKey(x => x.ParticipantId);
        modelBuilder.Entity<ConversationParticipant>()
            .HasIndex(x => new { x.ThreadId, x.ParticipantType, x.ParticipantRefId })
            .HasDatabaseName("IX_ConversationParticipants_Thread_Participant");
        modelBuilder.Entity<ConversationParticipant>()
            .HasOne(x => x.Thread)
            .WithMany(x => x.Participants)
            .HasForeignKey(x => x.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OutboxMessage>().HasKey(x => x.OutboxMessageId);
        modelBuilder.Entity<OutboxMessage>()
            .Property(x => x.RowVersion)
            .IsRowVersion();
        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(x => new { x.TenantId, x.Status, x.NextAttemptUtc })
            .HasDatabaseName("IX_OutboxMessages_Status_NextAttempt");
        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("IX_OutboxMessages_Idempotency");

        modelBuilder.Entity<DeadLetterMessage>().HasKey(x => x.DeadLetterId);
        modelBuilder.Entity<DeadLetterMessage>()
            .HasIndex(x => new { x.TenantId, x.FailedAtUtc })
            .HasDatabaseName("IX_DeadLetterMessages_FailedAt");

        modelBuilder.Entity<Case>().HasKey(x => x.CaseId);
        modelBuilder.Entity<Case>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.StudentId, x.Status })
            .HasDatabaseName("IX_Cases_Tenant_School_Student_Status");
        modelBuilder.Entity<Case>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.CaseType, x.Status })
            .HasDatabaseName("IX_Cases_Tenant_School_Type_Status");

        modelBuilder.Entity<CaseTimelineEvent>().HasKey(x => x.EventId);
        modelBuilder.Entity<CaseTimelineEvent>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.CaseId, x.CreatedAtUtc })
            .HasDatabaseName("IX_CaseTimelineEvents_Tenant_School_Case_Created");

        modelBuilder.Entity<TimelineEvent>().HasKey(x => x.EventId);
        modelBuilder.Entity<TimelineEvent>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.StudentId, x.OccurredAtUtc })
            .HasDatabaseName("IX_TimelineEvents_StudentId_OccurredAtUtc");
        modelBuilder.Entity<TimelineEvent>()
            .HasIndex(x => new { x.TenantId, x.CaseId, x.OccurredAtUtc })
            .HasDatabaseName("IX_TimelineEvents_CaseId_OccurredAtUtc");
        modelBuilder.Entity<TimelineEvent>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.StudentId, x.Category, x.OccurredAtUtc })
            .HasDatabaseName("IX_TimelineEvents_Category");
        modelBuilder.Entity<TimelineEvent>()
            .Property(x => x.EventType)
            .HasMaxLength(64);
        modelBuilder.Entity<TimelineEvent>()
            .Property(x => x.Category)
            .HasMaxLength(32);
        modelBuilder.Entity<TimelineEvent>()
            .Property(x => x.SourceEntityType)
            .HasMaxLength(64);
        modelBuilder.Entity<TimelineEvent>()
            .Property(x => x.ActorId)
            .HasMaxLength(256);
        modelBuilder.Entity<TimelineEvent>()
            .Property(x => x.ActorName)
            .HasMaxLength(256);
        modelBuilder.Entity<TimelineEvent>()
            .Property(x => x.Title)
            .HasMaxLength(512);
        modelBuilder.Entity<TimelineEvent>()
            .Property(x => x.VisibilityScope)
            .HasMaxLength(32);
        modelBuilder.Entity<TimelineEvent>()
            .Property(x => x.MetadataJson)
            .HasColumnType("nvarchar(max)");
        modelBuilder.Entity<TimelineEvent>()
            .Property(x => x.SearchableText)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<SafeguardingAlert>().HasKey(x => x.AlertId);
        modelBuilder.Entity<SafeguardingAlert>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.CaseId, x.RequiresHumanReview })
            .HasDatabaseName("IX_SafeguardingAlerts_Tenant_School_Case_Review");

        modelBuilder.Entity<SchoolSettings>().HasKey(x => x.SchoolSettingsId);
        modelBuilder.Entity<SchoolSettings>()
            .HasIndex(x => new { x.TenantId, x.SchoolId })
            .IsUnique()
            .HasDatabaseName("IX_SchoolSettings_Tenant_School");

        modelBuilder.Entity<WorkTask>().HasKey(x => x.WorkTaskId);
        modelBuilder.Entity<WorkTask>()
            .ToTable("WorkTasks");
        modelBuilder.Entity<WorkTask>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.Status, x.DueAtUtc })
            .HasDatabaseName("IX_WorkTasks_Status_Due");
        modelBuilder.Entity<WorkTask>()
            .HasOne(x => x.Case)
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WorkTaskChecklist>().HasKey(x => x.WorkTaskChecklistId);
        modelBuilder.Entity<WorkTaskChecklist>()
            .ToTable("WorkTaskChecklists");
        modelBuilder.Entity<WorkTaskChecklist>()
            .HasOne(x => x.WorkTask)
            .WithMany()
            .HasForeignKey(x => x.WorkTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChecklistCompletion>().HasKey(x => x.ChecklistCompletionId);
        modelBuilder.Entity<ChecklistCompletion>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.CaseId, x.ChecklistId, x.ItemId })
            .HasDatabaseName("IX_ChecklistCompletions_Case_Item");
        modelBuilder.Entity<ChecklistCompletion>()
            .HasOne(x => x.Case)
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ChecklistCompletion>()
            .HasOne(x => x.WorkTask)
            .WithMany()
            .HasForeignKey(x => x.WorkTaskId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ChecklistCompletion>()
            .HasOne(x => x.Alert)
            .WithMany()
            .HasForeignKey(x => x.AlertId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ReasonCode>().HasKey(x => x.ReasonCodeId);
        modelBuilder.Entity<ReasonCode>()
            .HasIndex(x => new { x.TenantId, x.Scheme, x.Version, x.Code })
            .IsUnique()
            .HasDatabaseName("IX_ReasonCodes_Tenant_Scheme_Version_Code");

        modelBuilder.Entity<EvidencePack>().HasKey(x => x.EvidencePackId);
        modelBuilder.Entity<EvidencePack>()
            .HasIndex(x => new { x.CaseId, x.GeneratedAtUtc })
            .HasDatabaseName("IX_EvidencePacks_Case_GeneratedAt");
        modelBuilder.Entity<EvidencePack>()
            .Property(x => x.Format)
            .HasMaxLength(32);
        modelBuilder.Entity<EvidencePack>()
            .Property(x => x.GenerationPurpose)
            .HasMaxLength(64);
        modelBuilder.Entity<EvidencePack>()
            .Property(x => x.ContentHash)
            .HasMaxLength(256);
        modelBuilder.Entity<EvidencePack>()
            .Property(x => x.ManifestHash)
            .HasMaxLength(256);
        modelBuilder.Entity<EvidencePack>()
            .Property(x => x.IncludedSectionsJson)
            .HasColumnType("nvarchar(max)");
        modelBuilder.Entity<EvidencePack>()
            .Property(x => x.IndexJson)
            .HasColumnType("nvarchar(max)");
        modelBuilder.Entity<EvidencePack>()
            .HasOne(x => x.Case)
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<IngestionSyncLog>().HasKey(x => x.IngestionSyncLogId);
        modelBuilder.Entity<IngestionSyncLog>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.StartedAtUtc })
            .HasDatabaseName("IX_IngestionSyncLogs_Tenant_School_Started");

        modelBuilder.Entity<MessageTemplate>().HasKey(x => x.MessageTemplateId);
        modelBuilder.Entity<MessageTemplate>()
            .HasIndex(x => new { x.TenantId, x.TemplateKey, x.Channel })
            .IsUnique()
            .HasDatabaseName("IX_MessageTemplates_Tenant_Key_Channel");

        modelBuilder.Entity<NotificationRecipient>().HasKey(x => x.NotificationRecipientId);
        modelBuilder.Entity<NotificationRecipient>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.Route, x.Priority })
            .HasDatabaseName("IX_NotificationRecipients_Route");

        modelBuilder.Entity<ETBTrust>().HasKey(x => x.ETBTrustId);
        modelBuilder.Entity<ETBTrust>()
            .HasIndex(x => new { x.TenantId, x.Name })
            .HasDatabaseName("IX_ETBTrust_Tenant_Name");

        modelBuilder.Entity<Notification>().HasKey(x => x.NotificationId);
        modelBuilder.Entity<Notification>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.UserId, x.CreatedAtUtc })
            .HasDatabaseName("IX_Notifications_User_Created");

        modelBuilder.Entity<AudienceSegment>().HasKey(x => x.SegmentId);
        modelBuilder.Entity<AudienceSegment>()
            .HasIndex(x => new { x.TenantId, x.Name })
            .HasDatabaseName("IX_AudienceSegments_Name");
        modelBuilder.Entity<AudienceSnapshot>().HasKey(x => x.SnapshotId);
        modelBuilder.Entity<AudienceSnapshot>()
            .HasIndex(x => new { x.TenantId, x.SegmentId })
            .HasDatabaseName("IX_AudienceSnapshots_Segment");
        modelBuilder.Entity<Campaign>().HasKey(x => x.CampaignId);
        modelBuilder.Entity<Campaign>()
            .HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_Campaigns_Status");
        modelBuilder.Entity<Campaign>()
            .HasIndex(x => new { x.TenantId, x.SegmentId })
            .HasDatabaseName("IX_Campaigns_Segment");

        modelBuilder.Entity<EngagementEvent>().HasKey(x => x.EventId);
        modelBuilder.Entity<EngagementEvent>()
            .HasIndex(x => new { x.TenantId, x.GuardianId, x.MessageId, x.EventType, x.OccurredAtUtc })
            .HasDatabaseName("IX_EngagementEvents_Guardian_Message");

        modelBuilder.Entity<GuardianReachability>().HasKey(x => x.ReachabilityId);
        modelBuilder.Entity<GuardianReachability>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.GuardianId, x.Channel })
            .IsUnique()
            .HasDatabaseName("IX_GuardianReachability_Channel");
        modelBuilder.Entity<GuardianReachability>()
            .Property(x => x.ReachabilityScore)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<TranslationCache>().HasKey(x => x.TranslationCacheId);
        modelBuilder.Entity<TranslationCache>()
            .HasIndex(x => new { x.TenantId, x.Hash })
            .IsUnique()
            .HasDatabaseName("IX_TranslationCache_Hash");

        modelBuilder.Entity<PlaybookDefinition>().HasKey(x => x.PlaybookId);
        modelBuilder.Entity<PlaybookDefinition>()
            .HasIndex(x => new { x.TenantId, x.TriggerStageType, x.IsActive })
            .HasDatabaseName("IX_PlaybookDefinitions_TriggerStage");
        modelBuilder.Entity<PlaybookDefinition>()
            .Property(x => x.TriggerStageType)
            .HasMaxLength(64);

        modelBuilder.Entity<PlaybookStep>().HasKey(x => x.StepId);
        modelBuilder.Entity<PlaybookStep>()
            .HasIndex(x => new { x.PlaybookId, x.Order })
            .IsUnique()
            .HasDatabaseName("IX_PlaybookSteps_Playbook_Order");
        modelBuilder.Entity<PlaybookStep>()
            .Property(x => x.Channel)
            .HasMaxLength(32);
        modelBuilder.Entity<PlaybookStep>()
            .Property(x => x.TemplateKey)
            .HasMaxLength(256);
        modelBuilder.Entity<PlaybookStep>()
            .Property(x => x.FallbackChannel)
            .HasMaxLength(32);

        modelBuilder.Entity<PlaybookRun>().HasKey(x => x.RunId);
        modelBuilder.Entity<PlaybookRun>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.Status, x.NextStepScheduledAtUtc })
            .HasDatabaseName("IX_PlaybookRuns_Status_NextStep");
        modelBuilder.Entity<PlaybookRun>()
            .HasIndex(x => x.InstanceId)
            .HasDatabaseName("IX_PlaybookRuns_Instance");
        modelBuilder.Entity<PlaybookRun>()
            .Property(x => x.Status)
            .HasMaxLength(32);

        modelBuilder.Entity<PlaybookExecutionLog>().HasKey(x => x.LogId);
        modelBuilder.Entity<PlaybookExecutionLog>()
            .HasIndex(x => new { x.RunId, x.ScheduledForUtc })
            .HasDatabaseName("IX_PlaybookExecutionLogs_Run");
        modelBuilder.Entity<PlaybookExecutionLog>()
            .HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("IX_PlaybookExecutionLogs_Idempotency");
        modelBuilder.Entity<PlaybookExecutionLog>()
            .Property(x => x.IdempotencyKey)
            .HasMaxLength(450);
        modelBuilder.Entity<PlaybookExecutionLog>()
            .Property(x => x.Channel)
            .HasMaxLength(32);
        modelBuilder.Entity<PlaybookExecutionLog>()
            .Property(x => x.Status)
            .HasMaxLength(32);

        modelBuilder.Entity<TelemetryEvent>().HasKey(x => x.TelemetryEventId);
        modelBuilder.Entity<TelemetryEvent>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.OccurredAtUtc })
            .HasDatabaseName("IX_TelemetryEvents_Date");

        modelBuilder.Entity<AutomationMetrics>().HasKey(x => x.MetricsId);
        modelBuilder.Entity<AutomationMetrics>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.Date })
            .IsUnique()
            .HasDatabaseName("IX_AutomationMetrics_Date");
        modelBuilder.Entity<AutomationMetrics>()
            .Property(x => x.EstimatedMinutesSaved)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AutomationMetrics>()
            .Property(x => x.AttendanceImprovementDelta)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<MtssTierDefinition>().HasKey(x => x.TierDefinitionId);
        modelBuilder.Entity<MtssTierDefinition>()
            .HasIndex(x => new { x.TenantId, x.TierNumber })
            .HasDatabaseName("IX_MtssTierDefinitions_Tenant_TierNumber");
        modelBuilder.Entity<MtssTierDefinition>()
            .Property(x => x.Name)
            .HasMaxLength(256);
        modelBuilder.Entity<MtssTierDefinition>()
            .Property(x => x.EntryCriteriaJson)
            .HasColumnType("nvarchar(max)");
        modelBuilder.Entity<MtssTierDefinition>()
            .Property(x => x.ExitCriteriaJson)
            .HasColumnType("nvarchar(max)");
        modelBuilder.Entity<MtssTierDefinition>()
            .Property(x => x.EscalationCriteriaJson)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<MtssIntervention>().HasKey(x => x.InterventionId);
        modelBuilder.Entity<MtssIntervention>()
            .HasIndex(x => new { x.TenantId, x.Category })
            .HasDatabaseName("IX_MtssInterventions_Tenant_Category");
        modelBuilder.Entity<MtssIntervention>()
            .Property(x => x.Name)
            .HasMaxLength(256);
        modelBuilder.Entity<MtssIntervention>()
            .Property(x => x.Category)
            .HasMaxLength(64);
        modelBuilder.Entity<MtssIntervention>()
            .Property(x => x.ApplicableTiersJson)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<TierAssignment>().HasKey(x => x.AssignmentId);
        modelBuilder.Entity<TierAssignment>()
            .HasIndex(x => x.CaseId)
            .IsUnique()
            .HasDatabaseName("IX_TierAssignments_Case");
        modelBuilder.Entity<TierAssignment>()
            .Property(x => x.AssignmentReason)
            .HasMaxLength(64);
        modelBuilder.Entity<TierAssignment>()
            .Property(x => x.RationaleJson)
            .HasColumnType("nvarchar(max)");
        modelBuilder.Entity<TierAssignment>()
            .HasOne(x => x.Case)
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TierAssignment>()
            .HasOne(x => x.TierDefinition)
            .WithMany()
            .HasForeignKey(x => x.TierDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TierAssignmentHistory>().HasKey(x => x.HistoryId);
        modelBuilder.Entity<TierAssignmentHistory>()
            .HasIndex(x => new { x.CaseId, x.ChangedAtUtc })
            .HasDatabaseName("IX_TierAssignmentHistory_Case_ChangedAt");
        modelBuilder.Entity<TierAssignmentHistory>()
            .Property(x => x.ChangeType)
            .HasMaxLength(32);
        modelBuilder.Entity<TierAssignmentHistory>()
            .Property(x => x.ChangeReason)
            .HasMaxLength(256);
        modelBuilder.Entity<TierAssignmentHistory>()
            .Property(x => x.RationaleJson)
            .HasColumnType("nvarchar(max)");
        modelBuilder.Entity<TierAssignmentHistory>()
            .HasOne(x => x.Assignment)
            .WithMany()
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TierAssignmentHistory>()
            .HasOne(x => x.Case)
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CaseIntervention>().HasKey(x => x.CaseInterventionId);
        modelBuilder.Entity<CaseIntervention>()
            .HasIndex(x => new { x.CaseId, x.Status })
            .HasDatabaseName("IX_CaseInterventions_Case_Status");
        modelBuilder.Entity<CaseIntervention>()
            .Property(x => x.Status)
            .HasMaxLength(32);
        modelBuilder.Entity<CaseIntervention>()
            .HasOne(x => x.Case)
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CaseIntervention>()
            .HasOne(x => x.Intervention)
            .WithMany()
            .HasForeignKey(x => x.InterventionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationships
        modelBuilder.Entity<CaseTimelineEvent>()
            .HasOne(x => x.Case)
            .WithMany(x => x.TimelineEvents)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TimelineEvent>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TimelineEvent>()
            .HasOne(x => x.Case)
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SafeguardingAlert>()
            .HasOne(x => x.Case)
            .WithMany(x => x.SafeguardingAlerts)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // SyncRun and related entities
        modelBuilder.Entity<SyncRun>().HasKey(x => x.SyncRunId);
        modelBuilder.Entity<SyncRun>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.ProviderId, x.StartedAtUtc })
            .HasDatabaseName("IX_SyncRuns_Provider_Started");
        modelBuilder.Entity<SyncRun>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.Status })
            .HasDatabaseName("IX_SyncRuns_Status");
        modelBuilder.Entity<SyncRun>()
            .Property(x => x.ProviderId)
            .HasMaxLength(64);
        modelBuilder.Entity<SyncRun>()
            .Property(x => x.SyncType)
            .HasMaxLength(64);
        modelBuilder.Entity<SyncRun>()
            .Property(x => x.Status)
            .HasMaxLength(32);

        modelBuilder.Entity<SyncMetric>().HasKey(x => x.SyncMetricId);
        modelBuilder.Entity<SyncMetric>()
            .HasIndex(x => x.SyncRunId)
            .HasDatabaseName("IX_SyncMetrics_SyncRun");
        modelBuilder.Entity<SyncMetric>()
            .Property(x => x.EntityType)
            .HasMaxLength(64);
        modelBuilder.Entity<SyncMetric>()
            .HasOne(x => x.SyncRun)
            .WithMany(x => x.Metrics)
            .HasForeignKey(x => x.SyncRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SyncError>().HasKey(x => x.SyncErrorId);
        modelBuilder.Entity<SyncError>()
            .HasIndex(x => x.SyncRunId)
            .HasDatabaseName("IX_SyncErrors_SyncRun");
        modelBuilder.Entity<SyncError>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.OccurredAtUtc })
            .HasDatabaseName("IX_SyncErrors_OccurredAt");
        modelBuilder.Entity<SyncError>()
            .Property(x => x.EntityType)
            .HasMaxLength(64);
        modelBuilder.Entity<SyncError>()
            .Property(x => x.ExternalId)
            .HasMaxLength(256);
        modelBuilder.Entity<SyncError>()
            .Property(x => x.RawPayloadJson)
            .HasColumnType("nvarchar(max)");
        modelBuilder.Entity<SyncError>()
            .HasOne(x => x.SyncRun)
            .WithMany(x => x.Errors)
            .HasForeignKey(x => x.SyncRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SyncPayloadArchive>().HasKey(x => x.ArchiveId);
        modelBuilder.Entity<SyncPayloadArchive>()
            .HasIndex(x => x.SyncRunId)
            .HasDatabaseName("IX_SyncPayloadArchives_SyncRun");
        modelBuilder.Entity<SyncPayloadArchive>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.ExpiresAtUtc })
            .HasDatabaseName("IX_SyncPayloadArchives_ExpiresAt");
        modelBuilder.Entity<SyncPayloadArchive>()
            .Property(x => x.EntityType)
            .HasMaxLength(64);
        modelBuilder.Entity<SyncPayloadArchive>()
            .Property(x => x.ExternalId)
            .HasMaxLength(256);
        modelBuilder.Entity<SyncPayloadArchive>()
            .Property(x => x.PayloadJson)
            .HasColumnType("nvarchar(max)");

        // ClassGroup and StudentClassEnrollment
        modelBuilder.Entity<ClassGroup>().HasKey(x => x.ClassGroupId);
        modelBuilder.Entity<ClassGroup>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.ExternalClassId })
            .IsUnique()
            .HasDatabaseName("IX_ClassGroups_ExternalId");
        modelBuilder.Entity<ClassGroup>()
            .Property(x => x.ExternalClassId)
            .HasMaxLength(256);
        modelBuilder.Entity<ClassGroup>()
            .Property(x => x.Name)
            .HasMaxLength(256);
        modelBuilder.Entity<ClassGroup>()
            .Property(x => x.Code)
            .HasMaxLength(64);
        modelBuilder.Entity<ClassGroup>()
            .Property(x => x.AcademicYear)
            .HasMaxLength(32);
        modelBuilder.Entity<ClassGroup>()
            .Property(x => x.Source)
            .HasMaxLength(64);

        modelBuilder.Entity<StudentClassEnrollment>().HasKey(x => x.EnrollmentId);
        modelBuilder.Entity<StudentClassEnrollment>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.StudentId, x.ClassGroupId })
            .IsUnique()
            .HasDatabaseName("IX_StudentClassEnrollments_Student_Class");
        modelBuilder.Entity<StudentClassEnrollment>()
            .HasIndex(x => x.StudentId)
            .HasDatabaseName("IX_StudentClassEnrollments_Student");
        modelBuilder.Entity<StudentClassEnrollment>()
            .HasIndex(x => x.ClassGroupId)
            .HasDatabaseName("IX_StudentClassEnrollments_ClassGroup");
        modelBuilder.Entity<StudentClassEnrollment>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StudentClassEnrollment>()
            .HasOne(x => x.ClassGroup)
            .WithMany(x => x.StudentEnrollments)
            .HasForeignKey(x => x.ClassGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // ReasonCodeMapping
        modelBuilder.Entity<ReasonCodeMapping>().HasKey(x => x.ReasonCodeMappingId);
        modelBuilder.Entity<ReasonCodeMapping>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.ProviderId, x.ProviderCode })
            .IsUnique()
            .HasDatabaseName("IX_ReasonCodeMappings_Provider_Code");
        modelBuilder.Entity<ReasonCodeMapping>()
            .Property(x => x.ProviderId)
            .HasMaxLength(64);
        modelBuilder.Entity<ReasonCodeMapping>()
            .Property(x => x.ProviderCode)
            .HasMaxLength(128);
        modelBuilder.Entity<ReasonCodeMapping>()
            .Property(x => x.ProviderDescription)
            .HasMaxLength(512);
        modelBuilder.Entity<ReasonCodeMapping>()
            .Property(x => x.InternalCode)
            .HasMaxLength(128);

        // SchoolSyncState
        modelBuilder.Entity<SchoolSyncState>().HasKey(x => x.SchoolSyncStateId);
        modelBuilder.Entity<SchoolSyncState>()
            .HasIndex(x => new { x.TenantId, x.SchoolId, x.ProviderId, x.EntityType })
            .IsUnique()
            .HasDatabaseName("IX_SchoolSyncStates_Provider_Entity");
        modelBuilder.Entity<SchoolSyncState>()
            .Property(x => x.ProviderId)
            .HasMaxLength(64);
        modelBuilder.Entity<SchoolSyncState>()
            .Property(x => x.EntityType)
            .HasMaxLength(64);
        modelBuilder.Entity<SchoolSyncState>()
            .Property(x => x.LastError)
            .HasMaxLength(2048);

        // Extend IngestionSyncLog
        modelBuilder.Entity<IngestionSyncLog>()
            .Property(x => x.MismatchDetailsJson)
            .HasColumnType("nvarchar(max)");
        modelBuilder.Entity<IngestionSyncLog>()
            .Property(x => x.ErrorRateThreshold)
            .HasColumnType("decimal(18,2)");
    }

    public override int SaveChanges()
    {
        EnforceTenancyOnWrites();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnforceTenancyOnWrites();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantAndSchoolFilters(ModelBuilder modelBuilder)
    {
        // These values come from the scoped TenantContext. They must be set before querying.
        var tenantId = _tenant.TenantId;
        var schoolId = _tenant.SchoolId;

        // Skip tenant filtering for AppUser - Identity operations need global access
        // AppUser access is controlled by unique index on (TenantId, SchoolId, NormalizedUserName)
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // Skip AppUser for tenant filtering (handled by unique constraint)
            if (clrType == typeof(AppUser))
            {
                continue;
            }

            // Tenant filter
            if (typeof(ITenantScoped).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                var tenantProp = Expression.Property(parameter, nameof(ITenantScoped.TenantId));
                var tenantValue = Expression.Constant(tenantId);
                var tenantEq = Expression.Equal(tenantProp, tenantValue);
                var lambda = Expression.Lambda(tenantEq, parameter);

                modelBuilder.Entity(clrType).HasQueryFilter(lambda);
            }

            // Optional school filter if the entity is school-scoped and SchoolId is set
            if (schoolId.HasValue && typeof(ISchoolScoped).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                var schoolProp = Expression.Property(parameter, nameof(ISchoolScoped.SchoolId));
                var schoolValue = Expression.Constant(schoolId.Value);
                var schoolEq = Expression.Equal(schoolProp, schoolValue);
                var lambda = Expression.Lambda(schoolEq, parameter);

                modelBuilder.Entity(clrType).HasQueryFilter(lambda);
            }
        }
    }

    private void EnforceTenancyOnWrites()
    {
        var tenantId = _tenant.TenantId;
        var schoolId = _tenant.SchoolId;
        var appUserEntries = ChangeTracker.Entries<AppUser>().ToList();
        var hasNonAppUserChanges = ChangeTracker.Entries<ITenantScoped>()
            .Any(e => e.Entity is not AppUser);

        // Require TenantContext for non-AppUser changes
        if (hasNonAppUserChanges && tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantContext.TenantId not set before SaveChanges.");
        }

        // Handle AppUser - allow explicit TenantId/SchoolId or use TenantContext
        foreach (var entry in appUserEntries)
        {
            if (entry.State == EntityState.Added)
            {
                // Use TenantContext if available, otherwise require explicit TenantId
                if (entry.Entity.TenantId == Guid.Empty)
                {
                    if (tenantId == Guid.Empty)
                    {
                        throw new InvalidOperationException("AppUser must have TenantId set explicitly when TenantContext is not available.");
                    }
                    entry.Entity.TenantId = tenantId;
                    if (schoolId.HasValue && entry.Entity.SchoolId == Guid.Empty)
                    {
                        entry.Entity.SchoolId = schoolId.Value;
                    }
                }
                else if (tenantId != Guid.Empty && entry.Entity.TenantId != tenantId)
                {
                    throw new InvalidOperationException($"AppUser TenantId {entry.Entity.TenantId} does not match TenantContext {tenantId}.");
                }
            }
            else if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                if (tenantId == Guid.Empty)
                {
                    throw new InvalidOperationException("TenantContext must be set to modify or delete AppUser.");
                }
                if (entry.Entity.TenantId != tenantId)
                {
                    throw new InvalidOperationException("AppUser TenantId mismatch on write.");
                }
            }
        }

        // Handle other tenant-scoped entities (not AppUser)
        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.Entity is AppUser)
            {
                continue; // Already handled above
            }

            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = tenantId;
            }
            else if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                if (entry.Entity.TenantId != tenantId)
                    throw new InvalidOperationException("TenantId mismatch on write.");
            }
        }

        // Handle school-scoped entities (not AppUser)
        if (schoolId.HasValue)
        {
            foreach (var entry in ChangeTracker.Entries<ISchoolScoped>())
            {
                if (entry.Entity is AppUser)
                {
                    continue; // Already handled above
                }

                if (entry.State == EntityState.Added)
                {
                    entry.Entity.SchoolId = schoolId.Value;
                }
                else if (entry.State is EntityState.Modified or EntityState.Deleted)
                {
                    if (entry.Entity.SchoolId != schoolId.Value)
                        throw new InvalidOperationException("SchoolId mismatch on write.");
                }
            }
        }
    }
}
