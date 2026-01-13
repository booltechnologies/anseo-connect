---
name: Epic B Attendance Orchestrator
overview: Implement the Attendance Intervention Orchestrator (Epic B) - a rule-based system for automated staged interventions, letter artifact generation, meeting/conference tracking, and attendance analytics dashboards.
todos:
  - id: b1-normalization
    content: "B1: Add AttendanceDailySummary entity, migration, and AttendanceNormalizationService"
    status: completed
  - id: b2-rule-entities
    content: "B2: Add InterventionRuleSet, InterventionStage, StudentInterventionInstance, InterventionEvent entities + migration"
    status: completed
  - id: b2-rule-engine
    content: "B2: Implement InterventionRuleEngine service with rule evaluation and simulation"
    status: completed
  - id: b2-scheduler
    content: "B2: Implement InterventionScheduler background service for daily queue processing"
    status: completed
  - id: b3-letter-entities
    content: "B3: Add LetterTemplate and LetterArtifact entities + migration"
    status: completed
  - id: b3-letter-service
    content: "B3: Implement LetterGenerationService with QuestPDF, hash generation, and translation support"
    status: completed
  - id: b4-meeting
    content: "B4: Add InterventionMeeting entity, MeetingService with outcome handling and auto-task creation"
    status: completed
  - id: b5-analytics
    content: "B5: Add InterventionAnalytics entity and InterventionAnalyticsService"
    status: completed
  - id: b5-dashboards
    content: "B5: Create AttendanceDashboard.razor with DxChart components"
    status: completed
  - id: b5-reports
    content: "B5: Add ReportDefinition/Run/Artifact entities and ScheduledReportService"
    status: completed
  - id: api-endpoints
    content: "Add API controllers: InterventionsController, LettersController, MeetingsController, AnalyticsController"
    status: completed
  - id: ui-pages
    content: "Create UI pages: InterventionQueue, InterventionRules, Meetings, LetterTemplates, ScheduledReports"
    status: completed
---

# Epic B: Attendance Intervention Orchestrator Implementation Plan

## Summary

Epic B transforms the existing attendance and case management infrastructure into a full intervention orchestrator with:

- Normalized attendance events for consistent rule evaluation across SIS sources
- Configurable rule engine with stage ladder (Letter 1 -> Letter 2 -> Conference -> Escalation)
- PDF letter artifact generation with template versioning and translation
- Meeting/conference tracking with outcomes that drive workflow progression
- Dashboards with intervention attribution and scheduled analysis reports

---

## Current State Assessment

### What Exists

- `AttendanceMark` entity with basic fields (Date, Session, Status, ReasonCode, Source)
- `Case` entity with Tier (1/2/3) and basic escalation in `CaseService`
- `AbsenceDetectionService` for detecting unexplained absences
- Basic `ReportingService` with simple metrics
- `EvidencePackService` using QuestPDF for PDF generation
- `PolicyRuntime` with `TemplateEngine` for message rendering
- `ReasonCode` entity for absence taxonomy

### Key Files to Build On

- [`src/Shared/AnseoConnect.Data/Entities/Case.cs`](src/Shared/AnseoConnect.Data/Entities/Case.cs) - extend for intervention tracking
- [`src/Services/AnseoConnect.Workflow/Services/CaseService.cs`](src/Services/AnseoConnect.Workflow/Services/CaseService.cs) - integrate with rule engine
- [`src/Shared/AnseoConnect.PolicyRuntime/TemplateEngine.cs`](src/Shared/AnseoConnect.PolicyRuntime/TemplateEngine.cs) - extend for letter templates
- [`src/Services/AnseoConnect.Workflow/Services/EvidencePackService.cs`](src/Services/AnseoConnect.Workflow/Services/EvidencePackService.cs) - pattern for PDF generation

---

## B1. Attendance Data Normalization

### B1.S1 - Normalized Attendance Events

**New Entities:**

```csharp
// AttendanceDailySummary - aggregated daily view per student
public sealed class AttendanceDailySummary : SchoolEntity {
    Guid SummaryId;
    Guid StudentId;
    DateOnly Date;
    string AMStatus; // PRESENT/ABSENT/LATE/AUTHORISED_ABSENT/UNAUTHORISED_ABSENT
    string PMStatus;
    string? AMReasonCode;
    string? PMReasonCode;
    decimal AttendancePercent; // calculated rolling %
    int ConsecutiveAbsenceDays;
    int TotalAbsenceDaysYTD;
    DateTimeOffset ComputedAtUtc;
}
```

**Implementation:**

- Add `AttendanceDailySummary` entity and migration
- Create `AttendanceNormalizationService` that:
  - Runs after each Wonde sync (`AttendanceMarksIngestedConsumer`)
  - Computes daily summaries with AM/PM status consolidation
  - Calculates rolling metrics (% attendance, consecutive absences, YTD totals)
- Store raw Wonde payload reference in `AttendanceMark.RawPayloadJson` for traceability

**Files to modify:**

- `src/Shared/AnseoConnect.Data/Entities/` - add new entity
- `src/Shared/AnseoConnect.Data/AnseoConnectDbContext.cs` - register entity
- `src/Services/AnseoConnect.Workflow/Consumers/AttendanceMarksIngestedConsumer.cs` - trigger normalization

---

## B2. Rule Engine + Intervention Stages

### B2.S1 - Rule Sets and Stage Ladder

**New Entities:**

```csharp
// InterventionRuleSet - configurable per tenant/school
public sealed class InterventionRuleSet : SchoolEntity {
    Guid RuleSetId;
    string Name;
    string Jurisdiction; // IE, UK
    bool IsActive;
    string RulesJson; // Array of RuleCondition
}

// InterventionStage - defines the stage ladder
public sealed class InterventionStage : ITenantScoped {
    Guid StageId;
    Guid RuleSetId;
    int Order; // 1, 2, 3, 4...
    string StageType; // LETTER_1, LETTER_2, MEETING, ESCALATION
    string? LetterTemplateId;
    int? DaysBeforeNextStage;
    string? StopConditionsJson;
    string? EscalationConditionsJson;
}

// StudentInterventionInstance - tracks a student through stages
public sealed class StudentInterventionInstance : SchoolEntity {
    Guid InstanceId;
    Guid StudentId;
    Guid CaseId;
    Guid RuleSetId;
    Guid CurrentStageId;
    string Status; // ACTIVE, STOPPED, COMPLETED, ESCALATED
    DateTimeOffset StartedAtUtc;
    DateTimeOffset? LastStageAtUtc;
}

// InterventionEvent - append-only log of actions
public sealed class InterventionEvent : SchoolEntity {
    Guid EventId;
    Guid InstanceId;
    Guid StageId;
    string EventType; // STAGE_ENTERED, LETTER_SENT, STOPPED, ESCALATED
    string? ArtifactId;
    DateTimeOffset OccurredAtUtc;
}
```

**Services:**

```csharp
// InterventionRuleEngine - evaluates eligibility
public sealed class InterventionRuleEngine {
    Task<List<EligibleStudent>> EvaluateAsync(Guid schoolId, DateOnly date);
    Task<SimulationResult> SimulateAsync(Guid studentId, InterventionRuleSet ruleSet);
}

// InterventionScheduler - processes daily queue
public sealed class InterventionScheduler : BackgroundService {
    // Runs nightly, produces "today's intervention queue"
    // Also triggered on SIS sync completion
}
```

**Rule Conditions (JSON schema):**

- `AbsenceCountThreshold` - e.g., 5 absences in 30 days
- `AttendancePercentThreshold` - e.g., below 90%
- `ConsecutiveAbsenceDays` - e.g., 3+ consecutive
- `TardyCountThreshold` - late arrivals
- `DateWindow` - evaluation period

**Stop Conditions:**

- Attendance improved above threshold
- Case resolved
- Exemption flag set
- Guardian provided valid reason

**Files to create:**

- `src/Shared/AnseoConnect.Data/Entities/InterventionRuleSet.cs`
- `src/Shared/AnseoConnect.Data/Entities/InterventionStage.cs`
- `src/Shared/AnseoConnect.Data/Entities/StudentInterventionInstance.cs`
- `src/Shared/AnseoConnect.Data/Entities/InterventionEvent.cs`
- `src/Services/AnseoConnect.Workflow/Services/InterventionRuleEngine.cs`
- `src/Services/AnseoConnect.Workflow/Services/InterventionScheduler.cs`

---

## B3. Letter Artifacts (PDF) + Template Governance

### B3.S1 - Letter Templates with Versioning

**New Entities:**

```csharp
// LetterTemplate - versioned templates
public sealed class LetterTemplate : ITenantScoped {
    Guid TemplateId;
    string TemplateKey; // LETTER_1_ATTENDANCE, LETTER_2_ATTENDANCE
    int Version;
    string Status; // DRAFT, APPROVED, RETIRED
    string BodyHtml;
    string? MergeFieldSchemaJson;
    string? ApprovedBy;
    DateTimeOffset? ApprovedAtUtc;
    string LockScope; // DISTRICT_ONLY, SCHOOL_OVERRIDE_ALLOWED
}

// LetterArtifact - generated letters
public sealed class LetterArtifact : SchoolEntity {
    Guid ArtifactId;
    Guid InstanceId; // StudentInterventionInstance
    Guid StageId;
    Guid TemplateId;
    int TemplateVersion;
    Guid GuardianId;
    string LanguageCode;
    string StoragePath;
    string ContentHash; // SHA256 for integrity
    string MergeDataJson; // snapshot of merge values
    DateTimeOffset GeneratedAtUtc;
}
```

**Services:**

```csharp
// LetterGenerationService - produces PDFs
public sealed class LetterGenerationService {
    Task<LetterArtifact> GenerateAsync(
        Guid instanceId, 
        Guid stageId, 
        Guid guardianId, 
        string? preferredLanguage);
}
```

**Integration points:**

- Extend `TemplateEngine` to support letter HTML templates
- Use `ITranslationService` (from Epic A) for guardian language preference
- Store both source and translated PDFs if required
- Compute SHA256 hash for artifact integrity verification

**Files to create:**

- `src/Shared/AnseoConnect.Data/Entities/LetterTemplate.cs`
- `src/Shared/AnseoConnect.Data/Entities/LetterArtifact.cs`
- `src/Services/AnseoConnect.Workflow/Services/LetterGenerationService.cs`

---

## B4. Conferences/Meetings + Outcomes

### B4.S1 - Meeting Scheduling and Outcomes

**New Entities:**

```csharp
// InterventionMeeting - scheduled meetings
public sealed class InterventionMeeting : SchoolEntity {
    Guid MeetingId;
    Guid InstanceId; // StudentInterventionInstance
    Guid StageId;
    DateTimeOffset ScheduledAtUtc;
    DateTimeOffset? HeldAtUtc;
    string Status; // SCHEDULED, HELD, CANCELLED, NO_SHOW
    string? AttendeesJson; // staff, guardians present
    string? NotesJson;
    string? OutcomeCode; // RESOLVED, MEDICAL_PLAN, SUPPORT_PLAN, ESCALATE
    string? OutcomeNotes;
    Guid? CreatedByUserId;
}
```

**Outcome Taxonomy:**

- `RESOLVED` - issue addressed, stop intervention
- `MEDICAL_PLAN` - medical accommodation in place
- `SUPPORT_PLAN` - support plan agreed, monitor
- `ESCALATE` - escalate to next tier/external agency
- `RESCHEDULE` - guardian no-show, reschedule
- `NO_ACTION` - no further action required

**Auto-task creation:**

- On meeting completion with `SUPPORT_PLAN`, create follow-up WorkTask
- On `ESCALATE`, trigger Tier 3 escalation in CaseService

**Files to create:**

- `src/Shared/AnseoConnect.Data/Entities/InterventionMeeting.cs`
- `src/Services/AnseoConnect.Workflow/Services/MeetingService.cs`

---

## B5. Dashboards + Scheduled Reports

### B5.S1 - Dashboards with Attribution

**Analytics Model:**

```csharp
// InterventionAnalytics - materialized view/table for fast queries
public sealed class InterventionAnalytics : SchoolEntity {
    Guid AnalyticsId;
    DateOnly Date;
    int TotalStudents;
    int StudentsInIntervention;
    int Letter1Sent;
    int Letter2Sent;
    int MeetingsScheduled;
    int MeetingsHeld;
    int Escalated;
    int Resolved;
    decimal PreInterventionAttendanceAvg;
    decimal PostInterventionAttendanceAvg;
}
```

**Dashboard Components (Blazor + DxChart):**

- Attendance rate trends by school/year/class
- Persistent absence cohort (below 90%)
- Intervention funnel: students at each stage
- Attribution: attendance change after intervention
- Drilldown: District -> School -> Year -> Class -> Student
- Unreachable families overlay (from Epic A engagement data)

**Files to create:**

- `src/Web/AnseoConnect.Web/Pages/AttendanceDashboard.razor`
- `src/Services/AnseoConnect.Workflow/Services/InterventionAnalyticsService.cs`

### B5.S2 - Scheduled Analysis Reports

**New Entities:**

```csharp
// ReportDefinition - configurable report templates
public sealed class ReportDefinition : ITenantScoped {
    Guid DefinitionId;
    string Name;
    string ReportType; // ATTENDANCE_ANALYSIS, INTERVENTION_SUMMARY
    string ScheduleCron; // e.g., "0 0 1 */4 *" for quarterly
    bool IsActive;
    string ParametersJson;
}

// ReportRun - execution instance
public sealed class ReportRun : SchoolEntity {
    Guid RunId;
    Guid DefinitionId;
    DateTimeOffset StartedAtUtc;
    DateTimeOffset? CompletedAtUtc;
    string Status; // RUNNING, COMPLETED, FAILED
    string? ErrorMessage;
}

// ReportArtifact - generated outputs
public sealed class ReportArtifact : SchoolEntity {
    Guid ArtifactId;
    Guid RunId;
    string Format; // PDF, XLSX
    string StoragePath;
    string DataSnapshotHash;
    DateTimeOffset GeneratedAtUtc;
}
```

**Services:**

```csharp
// ScheduledReportService - background job
public sealed class ScheduledReportService : BackgroundService {
    // Evaluates ReportDefinitions on schedule
    // Generates PDF + XLSX outputs
    // Stores immutable snapshots
}
```

**Files to create:**

- `src/Shared/AnseoConnect.Data/Entities/ReportDefinition.cs`
- `src/Shared/AnseoConnect.Data/Entities/ReportRun.cs`
- `src/Shared/AnseoConnect.Data/Entities/ReportArtifact.cs`
- `src/Services/AnseoConnect.Workflow/Services/ScheduledReportService.cs`
- `src/Services/AnseoConnect.Workflow/Services/AttendanceReportBuilder.cs`

---

## API Endpoints

Add to `AnseoConnect.ApiGateway`:

```
// Intervention Management
GET    /api/interventions/queue                    # Today's intervention queue
GET    /api/interventions/students/{studentId}     # Student's intervention history
POST   /api/interventions/simulate                 # Simulate rules for student
GET    /api/interventions/rules                    # List rule sets
PUT    /api/interventions/rules/{id}               # Update rule set

// Letter Management
GET    /api/letters/templates                      # List letter templates
POST   /api/letters/generate                       # Generate letter for student
GET    /api/letters/artifacts/{id}                 # Download artifact

// Meetings
GET    /api/meetings                               # List meetings
POST   /api/meetings                               # Schedule meeting
PUT    /api/meetings/{id}                          # Update meeting/record outcome

// Dashboards/Reports
GET    /api/analytics/attendance                   # Attendance analytics
GET    /api/analytics/interventions                # Intervention metrics
GET    /api/reports/definitions                    # Report definitions
POST   /api/reports/run                            # Trigger manual report
GET    /api/reports/artifacts/{id}                 # Download report
```

---

## UI Pages

Add to `AnseoConnect.Web`:

- `Pages/InterventionQueue.razor` - Daily queue with actions
- `Pages/InterventionRules.razor` - Admin: rule set configuration
- `Pages/InterventionSimulator.razor` - Admin: "what if" testing
- `Pages/LetterTemplates.razor` - Admin: template management
- `Pages/Meetings.razor` - Meeting scheduler and tracker
- `Pages/AttendanceDashboard.razor` - Analytics with DxChart
- `Pages/ScheduledReports.razor` - Admin: report configuration

---

## Migration Strategy

1. **Phase 1:** Data model + migrations (all new entities)
2. **Phase 2:** Rule engine service + scheduler
3. **Phase 3:** Letter generation + template governance
4. **Phase 4:** Meeting workflow
5. **Phase 5:** Dashboards + scheduled reports
6. **Phase 6:** UI pages + API endpoints

---

## Dependencies

- **From Epic A (if not yet implemented):**
  - `ITranslationService` for letter translation
  - Guardian language preferences
- **External:**
  - QuestPDF (already in use)
  - Azure Blob Storage for artifact storage

---

## Definition of Done

Per the document's requirements:

- Unit tests for rule engine and letter generation
- Integration tests for scheduler and report generation
- Audit logging for all intervention actions
- Background jobs are idempotent and observable
- UI includes empty/error/loading states
- All API endpoints accept CancellationToken
- RBAC enforcement on intervention data access