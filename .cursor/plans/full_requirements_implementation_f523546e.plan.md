---
name: Full Requirements Implementation
overview: Implement all partially implemented and missing requirements from Design Brief v1.2, including RBAC roles, reason taxonomy, evidence packs, review windows, reporting, templates, channel fallback, ETB dashboards, tier escalation, WhatsApp, AI features with Azure OpenAI, and supporting infrastructure.
todos:
  - id: phase1-data-model
    content: "Phase 1: Create new entities (SchoolSettings, WorkTask, WorkTaskChecklist, ReasonCode, EvidencePack, IngestionSyncLog, MessageTemplate, NotificationRecipient, ETBTrust, Notification) and update existing entities"
    status: completed
  - id: phase1-migration
    content: "Phase 1: Create Step6_AddFullRequirements migration"
    status: completed
  - id: phase2-rbac
    content: "Phase 2: Implement StaffRole enum, authorization policies, and role seeding"
    status: completed
  - id: phase3-taxonomy
    content: "Phase 3: Create Ireland/TUSLA reason taxonomy policy pack and ReasonTaxonomyService"
    status: completed
  - id: phase4-tasks
    content: "Phase 4: Implement TaskService, review windows logic, and TaskDueConsumer"
    status: completed
  - id: phase5-evidence
    content: "Phase 5: Implement EvidencePackService with PDF generation"
    status: completed
  - id: phase6-reporting
    content: "Phase 6: Implement ReportingService, ReportsController, and update Reports.razor"
    status: completed
  - id: phase7-templates
    content: "Phase 7: Create TemplateEngine and policy pack templates"
    status: completed
  - id: phase8-fallback
    content: "Phase 8: Implement Polly retry policies and channel fallback logic"
    status: completed
  - id: phase9-etb
    content: "Phase 9: Implement ETBTrust entity, roll-up queries, and ETBDashboard.razor"
    status: completed
  - id: phase10-tier3
    content: "Phase 10: Implement Tier 3 escalation with evidence pack and DLP notification"
    status: completed
  - id: phase11-whatsapp
    content: "Phase 11: Implement TwilioWhatsAppSender and webhook handling"
    status: completed
  - id: phase12-ai
    content: "Phase 12: Create AnseoConnect.AI project with Azure OpenAI services, autonomy levels, and guardrails"
    status: postponed
  - id: phase13-ingestion
    content: "Phase 13: Implement IngestionSyncLog tracking, IngestionHealthController, and UI"
    status: completed
  - id: phase14-routing
    content: "Phase 14: Implement NotificationRoutingService and in-app notifications"
    status: completed
  - id: phase15-entra
    content: "Phase 15: Add Microsoft.Identity.Web and configure Entra ID authentication"
    status: completed
  - id: phase16-checklist
    content: "Phase 16: Fix checklist completion endpoint and add progress tracking"
    status: completed
  - id: phase17-cutoff
    content: "Phase 17: Add configurable cut-off times to SchoolSettings and AbsenceDetectionService"
    status: completed
  - id: phase18-rfid
    content: "Phase 18: Implement AttendanceReconciliationService and UI"
    status: completed
  - id: phase19-failsafe
    content: "Phase 19: Implement Wonde sync fail-safe with status tracking and messaging pause"
    status: completed
---

# Full Requirements Implementation Plan

This plan addresses all gaps identified in the Design Brief v1.2 review, organized into logical phases.

## Phase 1: Data Model Extensions

### 1.1 New Entities

Add entities to [AnseoConnect.Data/Entities/](src/Shared/AnseoConnect.Data/Entities/):

- **`SchoolSettings.cs`** - Per-school configuration (cut-off times, autonomy level, policy pack overrides)
- **`WorkTask.cs`** - Work items assigned to staff (linked to Case)
- **`WorkTaskChecklist.cs`** - Checklist items for tasks
- **`ReasonCode.cs`** - Absence reason taxonomy (TUSLA/DfE codes)
- **`EvidencePack.cs`** - Generated evidence exports
- **`IngestionSyncLog.cs`** - Sync health tracking (errors, mismatch rates)
- **`MessageTemplate.cs`** - Policy-driven templates with variables
- **`NotificationRecipient.cs`** - DLP/safeguarding routing configuration

### 1.2 Entity Updates

Extend existing entities:

- **`Case.cs`** - Add `ReviewDueAtUtc`, `AssignedToUserId`, `EscalatedAtUtc`, `BarrierCodes`
- **`School.cs`** - Add `ETBTrustId`, `SyncStatus`, `SyncErrorCount`
- **`AppUser.cs`** - Add `Role` enum property for RBAC
- **`SafeguardingAlert.cs`** - Add `RoutedToUserIds`, `AcknowledgedAtUtc`, `ChecklistProgress`

### 1.3 Migration

Create migration `Step6_AddFullRequirements` in [AnseoConnect.Data/Migrations/](src/Shared/AnseoConnect.Data/Migrations/)

---

## Phase 2: RBAC Implementation

### 2.1 Define Role Enum

Create `StaffRole` enum in [AnseoConnect.Data/Entities/](src/Shared/AnseoConnect.Data/Entities/):

```csharp
public enum StaffRole
{
    AttendanceAdmin,    // Daily lists, contact, logging
    Teacher,            // Basic read access
    YearHead,           // Targeted interventions, meetings
    Principal,          // Oversight, reporting
    DeputyPrincipal,
    DLP,                // Safeguarding alerts
    ETBTrustAdmin       // Multi-school roll-ups
}
```

### 2.2 Authorization Policies

Update [ApiGateway/Program.cs](src/Services/AnseoConnect.ApiGateway/Program.cs):

```csharp
options.AddPolicy("AttendanceAccess", p => p.RequireRole("AttendanceAdmin", "YearHead", "Principal", "DeputyPrincipal", "DLP"));
options.AddPolicy("CaseManagement", p => p.RequireRole("YearHead", "Principal", "DeputyPrincipal", "DLP"));
options.AddPolicy("SafeguardingAccess", p => p.RequireRole("DLP", "Principal", "DeputyPrincipal"));
options.AddPolicy("ReportingAccess", p => p.RequireRole("Principal", "DeputyPrincipal", "ETBTrustAdmin"));
options.AddPolicy("ETBTrustAccess", p => p.RequireRole("ETBTrustAdmin"));
options.AddPolicy("SettingsAdmin", p => p.RequireRole("Principal", "DeputyPrincipal"));
```

### 2.3 Seed Roles

Add role seeding in [DBMigrator/Program.cs](tools/DBMigrator/Program.cs)

---

## Phase 3: Reason Taxonomy (Ireland/TUSLA)

### 3.1 Policy Pack Data

Create [policy-packs/ie/IE-ANSEO-DEFAULT/1.3.0/reason-taxonomy.json](policy-packs/ie/IE-ANSEO-DEFAULT/1.3.0/reason-taxonomy.json):

```json
{
  "enabled": true,
  "countryDefaults": {
    "IE": {
      "scheme": "TUSLA_TESS",
      "version": "2026",
      "codes": [
        { "code": "ILL", "label": "Illness", "type": "AUTHORISED" },
        { "code": "MED", "label": "Medical/Dental appointment", "type": "AUTHORISED" },
        { "code": "FAM", "label": "Family reasons", "type": "AUTHORISED" },
        { "code": "REL", "label": "Religious observance", "type": "AUTHORISED" },
        { "code": "TRA", "label": "Traveller (traditional activities)", "type": "AUTHORISED" },
        { "code": "SUS", "label": "Suspension", "type": "AUTHORISED" },
        { "code": "EXP", "label": "Expulsion", "type": "AUTHORISED" },
        { "code": "UNE", "label": "Unexplained absence", "type": "UNAUTHORISED" },
        { "code": "TRU", "label": "Truancy", "type": "UNAUTHORISED" },
        { "code": "LAT", "label": "Late arrival (after roll)", "type": "UNAUTHORISED" },
        { "code": "HOL", "label": "Holiday during term", "type": "UNAUTHORISED" },
        { "code": "OTH", "label": "Other", "type": "OTHER" }
      ]
    }
  }
}
```

### 3.2 Taxonomy Service

Create `ReasonTaxonomyService` in [AnseoConnect.PolicyRuntime/](src/Shared/AnseoConnect.PolicyRuntime/) to load and validate codes

---

## Phase 4: Review Windows and Tasks

### 4.1 Case Review Logic

Update [CaseService.cs](src/Services/AnseoConnect.Workflow/Services/CaseService.cs):

- Add `SetReviewWindow(caseId, daysFromNow)` method
- Add `GetOverdueReviews()` query
- Add automatic review window based on tier (Tier 1: 5 days, Tier 2: 10 days, Tier 3: 3 days)

### 4.2 Task Service

Create `TaskService.cs` in [AnseoConnect.Workflow/Services/](src/Services/AnseoConnect.Workflow/Services/):

- `CreateTaskAsync(caseId, title, assignedRole, dueDate, checklistId)`
- `CompleteTaskAsync(taskId, notes)`
- `GetTasksDueToday()`
- `GetOverdueTasks()`

### 4.3 Task Consumer

Create `TaskDueConsumer.cs` to publish reminder notifications when tasks are overdue

---

## Phase 5: Evidence Pack Export

### 5.1 Evidence Pack Service

Create `EvidencePackService.cs` in [AnseoConnect.Workflow/Services/](src/Services/AnseoConnect.Workflow/Services/):

```csharp
public async Task<EvidencePackDto> GenerateEvidencePackAsync(Guid caseId)
{
    // Collect: attendance history, communications, interventions, timeline, outcomes
    // Generate PDF or structured JSON export
}
```

### 5.2 API Endpoint

Add to [CasesController.cs](src/Services/AnseoConnect.ApiGateway/Controllers/CasesController.cs):

```csharp
[HttpGet("{caseId}/evidence-pack")]
[Authorize(Policy = "CaseManagement")]
public async Task<IActionResult> GetEvidencePack(Guid caseId)
```

### 5.3 PDF Generation

Add `QuestPDF` NuGet package for PDF generation

---

## Phase 6: Reporting and Dashboards

### 6.1 Reporting Service

Create `ReportingService.cs` in [AnseoConnect.ApiGateway/Services/](src/Services/AnseoConnect.ApiGateway/Services/):

- `GetSchoolDashboardMetrics()` - attendance trend, persistent absence %, open cases by tier
- `GetCommsEffectivenessMetrics()` - same-day contact rate, reply rate, opt-out rate
- `GetETBRollupMetrics(etbId)` - aggregated metrics across schools

### 6.2 API Endpoints

Create `ReportsController.cs` in [ApiGateway/Controllers/](src/Services/AnseoConnect.ApiGateway/Controllers/):

```csharp
[HttpGet("school-dashboard")]
[Authorize(Policy = "ReportingAccess")]
public async Task<IActionResult> GetSchoolDashboard()

[HttpGet("etb-dashboard")]
[Authorize(Policy = "ETBTrustAccess")]
public async Task<IActionResult> GetETBDashboard()

[HttpGet("export")]
[Authorize(Policy = "ReportingAccess")]
public async Task<IActionResult> ExportReport([FromQuery] string format = "csv")
```

### 6.3 Update UI

Update [Reports.razor](src/Web/AnseoConnect.Web/Pages/Reports.razor) to call real API endpoints

---

## Phase 7: Message Templates with Variables

### 7.1 Template Engine

Create `TemplateEngine.cs` in [AnseoConnect.PolicyRuntime/](src/Shared/AnseoConnect.PolicyRuntime/):

- Load templates from policy pack
- Variable substitution: `{{StudentName}}`, `{{Date}}`, `{{SchoolName}}`
- Tone constraint validation

### 7.2 Policy Pack Templates

Add to [policy-packs/ie/IE-ANSEO-DEFAULT/1.3.0/templates.json](policy-packs/ie/IE-ANSEO-DEFAULT/1.3.0/templates.json):

```json
{
  "templates": [
    {
      "id": "ABSENCE_FIRST_CONTACT",
      "channel": "SMS",
      "subject": null,
      "body": "Dear {{GuardianTitle}}, {{StudentFirstName}} was marked absent on {{Date}}. Please reply with reason or contact the school.",
      "toneConstraints": ["professional", "concise"],
      "maxLength": 160
    }
  ]
}
```

### 7.3 Update MessageService

Update [MessageService.cs](src/Services/AnseoConnect.Comms/Services/MessageService.cs) to use `TemplateEngine`

---

## Phase 8: Channel Fallback and Retry

### 8.1 Retry Policy

Add Polly retry policies to [MessageService.cs](src/Services/AnseoConnect.Comms/Services/MessageService.cs):

```csharp
private static readonly AsyncRetryPolicy RetryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
```

### 8.2 Channel Fallback Logic

Implement fallback in `MessageService`:

```csharp
public async Task SendWithFallbackAsync(SendMessageRequestedV1 command)
{
    var channels = GetChannelPriority(command.MessageType); // SMS -> EMAIL -> WHATSAPP
    foreach (var channel in channels)
    {
        if (await TrySendAsync(command with { Channel = channel }))
            return;
    }
    await CreateFailedMessageAsync(command, "ALL_CHANNELS_FAILED");
}
```

### 8.3 Dead Letter Queue

Configure Service Bus DLQ handling for failed messages

---

## Phase 9: ETB/Trust Dashboard

### 9.1 Data Model

Add `ETBTrust` entity and link to `School`:

```csharp
public class ETBTrust
{
    public Guid ETBTrustId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; }
    public ICollection<School> Schools { get; set; }
}
```

### 9.2 Roll-up Queries

Add to `ReportingService`:

- Aggregate attendance across all schools in ETB
- Benchmark comparisons
- Cohort analysis

### 9.3 UI Page

Create [ETBDashboard.razor](src/Web/AnseoConnect.Web/Pages/ETBDashboard.razor) for multi-school view

---

## Phase 10: Tier 3 Escalation

### 10.1 Escalation Logic

Update [CaseService.cs](src/Services/AnseoConnect.Workflow/Services/CaseService.cs):

```csharp
public async Task<bool> EscalateToTier3Async(Guid caseId, string reason)
{
    // Tier 3 requires: evidence pack generated, DLP notified
    await GenerateEvidencePackAsync(caseId);
    await NotifyDLPAsync(caseId, "TIER_3_ESCALATION");
    // Update case tier and create timeline event
}
```

### 10.2 Tier Thresholds

Add configurable thresholds to `SchoolSettings`:

- Tier 2 threshold: X unexplained absences in Y days
- Tier 3 threshold: Z consecutive days or % absence rate

---

## Phase 11: WhatsApp Channel

### 11.1 Twilio WhatsApp Sender

Create `TwilioWhatsAppSender.cs` in [AnseoConnect.Comms/Services/](src/Services/AnseoConnect.Comms/Services/):

```csharp
public async Task<WhatsAppSendResult> SendWhatsAppAsync(string to, string body)
{
    // Use Twilio WhatsApp API (prefix number with "whatsapp:")
    var message = await MessageResource.CreateAsync(
        to: new PhoneNumber($"whatsapp:{to}"),
        from: new PhoneNumber($"whatsapp:{_fromNumber}"),
        body: body
    );
}
```

### 11.2 WhatsApp Webhook

Add `TwilioWhatsAppWebhookController.cs` for delivery/reply handling

### 11.3 Update MessageService

Add WhatsApp routing in `MessageService.ProcessMessageRequestAsync`

---

## Phase 12: AI Features (Azure OpenAI)

### 12.1 AI Service

Create `AnseoConnect.AI` project with:

- **`AzureOpenAIService.cs`** - Client wrapper
- **`MessageDraftingService.cs`** - Draft messages within templates
- **`CaseSummarizationService.cs`** - Summarize case timelines
- **`ReplyClassificationService.cs`** - Classify guardian replies to reason codes
- **`PrioritizationService.cs`** - Rank daily action list

### 12.2 Autonomy Levels

Implement A0/A1/A2 in `SchoolSettings`:

```csharp
public enum AutonomyLevel
{
    A0_Advisory,      // Drafts/recommendations only
    A1_AutoMessage,   // Can send messages within policy
    A2_AutoEscalate   // Can open/advance cases (never safeguarding)
}
```

### 12.3 AI Guardrails

Create `AIGuardrails.cs` to enforce:

- Safeguarding always A0
- Template/tone constraints
- Human approval for sensitive actions

---

## Phase 13: Ingestion Health Dashboard

### 13.1 Sync Health Tracking

Update [IngestionService.cs](src/Services/AnseoConnect.Ingestion.Wonde/Services/IngestionService.cs):

- Log sync results to `IngestionSyncLog`
- Track: last sync time, records processed, errors, mismatch rate

### 13.2 Health API

Create `IngestionHealthController.cs`:

```csharp
[HttpGet("health")]
public async Task<IActionResult> GetIngestionHealth()
// Returns: last sync per school, error counts, missing entities
```

### 13.3 UI Page

Create [IngestionHealth.razor](src/Web/AnseoConnect.Web/Pages/IngestionHealth.razor)

---

## Phase 14: DLP Recipient Routing

### 14.1 Notification Routing Service

Create `NotificationRoutingService.cs` in [AnseoConnect.Workflow/Services/](src/Services/AnseoConnect.Workflow/Services/):

- Load routing rules from policy pack
- Route safeguarding alerts to configured DLP/Principal
- Escalate to secondary if no acknowledgement within SLA

### 14.2 In-App Notifications

Create `Notification` entity and `NotificationService` for in-app alerts

---

## Phase 15: Entra ID Authentication

### 15.1 Add Microsoft.Identity.Web

Update [ApiGateway.csproj](src/Services/AnseoConnect.ApiGateway/AnseoConnect.ApiGateway.csproj):

```xml
<PackageReference Include="Microsoft.Identity.Web" Version="3.*" />
```

### 15.2 Configure Dual Auth

Update [Program.cs](src/Services/AnseoConnect.ApiGateway/Program.cs):

```csharp
.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
```

### 15.3 Claim Mapping

Map Entra claims to tenant/school context

---

## Phase 16: Checklist Completion

### 16.1 Fix Placeholder

Update [CasesController.cs](src/Services/AnseoConnect.ApiGateway/Controllers/CasesController.cs):

```csharp
[HttpPatch("{caseId}/checklist/{checklistId}/items/{itemId}/complete")]
public async Task<IActionResult> MarkChecklistItemComplete(
    Guid caseId, string checklistId, string itemId, [FromBody] CompleteChecklistRequest request)
{
    await _caseService.CompleteChecklistItemAsync(caseId, checklistId, itemId, request.Notes);
    return Ok();
}
```

### 16.2 Checklist Progress Tracking

Add `ChecklistProgress` JSON column to `SafeguardingAlert` and `WorkTask`

---

## Phase 17: Configurable Cut-off Times

### 17.1 School Settings

Add to `SchoolSettings`:

```csharp
public TimeOnly AMCutoffTime { get; set; } = new(10, 30);
public TimeOnly PMCutoffTime { get; set; } = new(14, 30);
```

### 17.2 Update Absence Detection

Update [AbsenceDetectionService.cs](src/Services/AnseoConnect.Workflow/Services/AbsenceDetectionService.cs) to use per-school cut-offs

---

## Phase 18: RFID Reconciliation

### 18.1 Reconciliation Service

Create `AttendanceReconciliationService.cs`:

- Compare RFID attendance vs SIS marks
- Flag mismatches for review
- Calculate mismatch rate

### 18.2 Reconciliation UI

Add reconciliation panel to ingestion health dashboard

---

## Phase 19: Wonde Sync Fail-safe

### 19.1 Sync Status Tracking

Add `SyncStatus` enum to `School`:

```csharp
public enum SyncStatus { Healthy, Warning, Failed, Paused }
```

### 19.2 Fail-safe Logic

Update ingestion to:

- Set `SyncStatus = Failed` on errors
- Pause automated messaging when status is Failed
- Notify admins via email/in-app

---

## Files to Create

| File | Purpose |

|------|---------|

| `Entities/SchoolSettings.cs` | Per-school configuration |

| `Entities/WorkTask.cs` | Work items |

| `Entities/WorkTaskChecklist.cs` | Task checklists |

| `Entities/ReasonCode.cs` | Reason taxonomy |

| `Entities/EvidencePack.cs` | Evidence exports |

| `Entities/IngestionSyncLog.cs` | Sync health |

| `Entities/MessageTemplate.cs` | Templates |

| `Entities/NotificationRecipient.cs` | Routing config |

| `Entities/ETBTrust.cs` | ETB/Trust grouping |

| `Entities/Notification.cs` | In-app notifications |

| `Services/TaskService.cs` | Work task management |

| `Services/EvidencePackService.cs` | Evidence generation |

| `Services/ReportingService.cs` | Metrics calculation |

| `Services/NotificationRoutingService.cs` | DLP routing |

| `Services/AttendanceReconciliationService.cs` | RFID reconciliation |

| `PolicyRuntime/TemplateEngine.cs` | Template variables |

| `PolicyRuntime/ReasonTaxonomyService.cs` | Taxonomy loading |

| `Comms/Services/TwilioWhatsAppSender.cs` | WhatsApp channel |

| `AI/AzureOpenAIService.cs` | AI client |

| `AI/MessageDraftingService.cs` | AI drafting |

| `AI/CaseSummarizationService.cs` | AI summaries |

| `AI/ReplyClassificationService.cs` | AI classification |

| `AI/PrioritizationService.cs` | AI prioritization |

| `AI/AIGuardrails.cs` | AI safety |

| `Controllers/ReportsController.cs` | Reporting endpoints |

| `Controllers/IngestionHealthController.cs` | Sync health |

| `Controllers/TasksController.cs` | Task endpoints |

| `Pages/ETBDashboard.razor` | ETB view |

| `Pages/IngestionHealth.razor` | Health dashboard |

| `policy-packs/ie/.../reason-taxonomy.json` | TUSLA codes |

| `policy-packs/ie/.../templates.json` | Message templates |

---

## Execution Order

```mermaid
flowchart TD
    P1[Phase 1: Data Model] --> P2[Phase 2: RBAC]
    P1 --> P3[Phase 3: Reason Taxonomy]
    P2 --> P4[Phase 4: Review Windows/Tasks]
    P3 --> P4
    P4 --> P5[Phase 5: Evidence Pack]
    P4 --> P6[Phase 6: Reporting]
    P6 --> P9[Phase 9: ETB Dashboard]
    P4 --> P10[Phase 10: Tier 3]
    P1 --> P7[Phase 7: Templates]
    P7 --> P8[Phase 8: Fallback/Retry]
    P8 --> P11[Phase 11: WhatsApp]
    P7 --> P12[Phase 12: AI Features]
    P1 --> P13[Phase 13: Ingestion Health]
    P13 --> P18[Phase 18: RFID Reconciliation]
    P13 --> P19[Phase 19: Wonde Fail-safe]
    P2 --> P14[Phase 14: DLP Routing]
    P2 --> P15[Phase 15: Entra ID]
    P4 --> P16[Phase 16: Checklist]
    P1 --> P17[Phase 17: Cut-off Times]
```