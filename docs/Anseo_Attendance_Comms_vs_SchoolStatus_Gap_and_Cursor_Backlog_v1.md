# Functional Gap Analysis + Cursor Execution Backlog (Final-Product Target)
**Scope:** Attendance + Case Management + Family Communications (Staff + Family Portal)  
**Explicitly out of scope:** Forms & Flows, Sites, Educator Observation/Coaching  
**Target regions:** Ireland + UK  
**Channels:** SMS, Email, WhatsApp, In-app  
**SIS targets:** Tyro, VSware, Compass Education, Wonde (aggregator)  
**Generated:** January 12, 2026

---

## 1. Purpose
This document identifies what is missing in the current system design compared to the SchoolStatus feature set (Connect + Attend + Attend Hybrid), and turns it into a detailed, Cursor-ready build plan aimed at a production-grade final product (not an MVP).

It is written to keep Cursor “on rails”: each deliverable includes precise requirements, acceptance criteria, and copy/paste prompts.

---

## 2. Current State (as provided)
Partial implementation exists for:
- Case Management
- SIS Sync
- Messaging (some capability)

Not implemented (or not yet at parity):
- Family portal/app
- Full communications hub (threads, segmentation, analytics, translation, preference center, governance)
- Automated intervention orchestration (rules engine, staged letters, hybrid multi-touch playbooks)
- MTSS attendance tier model and evidence packs
- Channel consent + deliverability/reachability analytics

---

## 3. High-Level Gaps vs SchoolStatus (What you must add to compete)
### 3.1 Communications Hub (Connect-equivalent)
Missing: A full district/school communications system, not just “send a message”.
- Threaded two-way messaging (staff ↔ guardians) with search and history
- Multi-guardian modelling per student (household reality)
- Language preference + auto-translation loop (outbound + inbound)
- Audience segmentation + campaigns + saved segments
- Engagement analytics (delivered/opened/replied/failed/unreachable)
- Attachments and “document delivery” audit (virtual backpack concept)
- Template governance: versioning, approvals, locked district templates, per-school overrides
- Family portal/app: inbox + preferences + consent management

### 3.2 Attendance Intervention Orchestrator (Attend-equivalent)
Missing: Automated staged interventions driven by attendance data.
- Eligibility engine: daily and on-sync evaluation
- Stage ladder: Letter 1 → Letter 2 → Conference → Escalation (configurable by jurisdiction)
- Letter artifact generation (PDF) + template versions + translation
- Tracking: which stage sent, when, and what happened afterward
- Conference tracking + outcome recording + follow-up tasks
- Reporting cadence: scheduled “analysis reports” and dashboards

### 3.3 Hybrid Multi-touch Playbooks (Attend Hybrid-equivalent)
Missing: One configured playbook that runs continuously.
- A playbook runner that schedules multi-channel touches after stage triggers
- Stop/escalation conditions based on subsequent attendance improvements
- Proof/ROI telemetry: time saved, touch counts, impact per stage

### 3.4 MTSS Attendance Layer (Operational Strategy)
Missing: Tiered intervention model with measurable progression.
- Tier 1/2/3 model
- Intervention library mapped to tiers and stages
- Entry/exit/escalation criteria
- Evidence packs for inspection/audit and handoff to external agencies

### 3.5 GDPR-grade Consent + Deliverability + Reachability
Missing: Compliance and operational reliability in comms.
- Channel-specific consent and opt-outs (esp. WhatsApp)
- Preference center (language/channel/time windows)
- Bounce/failed delivery handling
- “Unreachable families” queues and dashboards
- Audit logs for data access and communications

---

## 4. Product Vision (Final Product)
A unified platform that provides:
1) Student Support Timeline: one place to see attendance events + comms threads + letters + meetings + tasks + outcomes (RBAC-filtered).  
2) Comms Hub: staff tools + family portal + analytics + translation + segmentation + governance.  
3) Attendance Orchestration: rules engine + staged interventions + hybrid follow-ups + conference workflow.  
4) MTSS Layer: tiering + intervention library + evidence packs + escalation workflows.  
5) Connector ecosystem: robust SIS integrations with health monitoring.

---

## 5. Architecture Principles (Non-negotiables for “final product”)
### 5.1 Reliability and correctness
- Transactional outbox for all outbound sends (SMS/Email/WhatsApp/In-app). No direct “send” in request thread.
- Idempotency keys for message sends and letter runs.
- Retry + DLQ (dead-letter queue) with operator tooling.
- Event-based timeline (append-only events) for auditability and evidence packs.

### 5.2 Security and compliance (Ireland/UK + GDPR)
- Least privilege RBAC + school boundary enforcement
- Audit log: read/write on sensitive student data, message content access, report exports
- Retention policies per tenant
- Consent model per channel and per guardian
- DPIA-friendly data classification tags

### 5.3 Tenant model
- District/Trust/ETB-level tenant with schools under it
- Per-school overrides: thresholds, templates, routing, escalation policies

---

## 6. Detailed Cursor Backlog (Epics → Stories → Tasks)
Legend
- Epic: large deliverable
- Story: user-facing capability
- Task: implementable work item

Each story includes: requirements, acceptance criteria, and Cursor prompts.

---

# EPIC A — Communications Hub (Connect-equivalent)

## A1. Core Data Model for Communications
### A1.S1 — Conversation Threads (channel-agnostic)
Requirements
- Thread between staff member(s) and guardian(s), optionally linked to a student (or multiple students if household message).
- Support multiple guardians per student; guardians may be attached to multiple students.
- Thread metadata: status, last activity, tags, assigned owner, school context.
- Messages include direction, channel, provider IDs, translated variants, delivery states, and attachments.

Acceptance Criteria
- Create thread, add participants, send message, receive reply, view full history.
- Search threads by student, guardian, phone/email, keyword.
- RBAC enforcement: staff can only access threads for schools they have rights to.

Cursor Prompt
```text
Create EF Core entities + migrations:
- Guardian, StudentGuardianLink
- ConversationThread, ConversationParticipant
- Message, MessageLocalizedText
- MessageDeliveryAttempt
- Attachment
Add indexes for (TenantId, SchoolId, StudentId, LastActivityUtc), (ThreadId, CreatedAtUtc).
Add soft-delete + audit fields and rowversion concurrency.
```

### A1.S2 — Contact Preferences + Consent
Requirements
- Preferences per guardian: preferred language, channel enablement, quiet hours, preferred contact order.
- Consent per channel with timestamp, source, scope, and revocation.
- Audit trail of changes.

Acceptance Criteria
- System prevents WhatsApp/SMS sends when consent is absent or revoked.
- Preference center can be managed by staff and via family portal (Epic A6).

Cursor Prompt
```text
Add entities:
- ContactPreference (GuardianId, PreferredLanguage, PreferredChannels, QuietHoursJson, UpdatedBy, UpdatedAtUtc)
- ConsentRecord (GuardianId, Channel, Status, CapturedAtUtc, CapturedSource, RevokedAtUtc, Notes)
Update send pipeline to enforce consent checks and log blocks.
```

---

## A2. Messaging Infrastructure (Providers + Outbox)
### A2.S1 — Transactional Outbox + Dispatcher
Requirements
- Outbox table storing message intent.
- Background dispatcher sends via provider implementations.
- Retries + exponential backoff + DLQ.
- Idempotency keys to prevent duplicates.

Acceptance Criteria
- If provider is down, messages queue and retry; UI shows status.
- No duplicate sends on retry.

Cursor Prompt
```text
Implement Outbox pattern:
- OutboxMessage (Id, TenantId, Type, PayloadJson, IdempotencyKey, Status, AttemptCount, NextAttemptUtc, LastError, CreatedAtUtc)
- Background service dispatcher with per-tenant throttling and concurrency control.
- DeadLetterMessage table for exhausted retries.
Add admin screens: Outbox monitor + DLQ triage + replay.
```

### A2.S2 — Provider Interfaces + Implementations (SMS/Email/WhatsApp/In-app)
Requirements
- ISmsProvider, IEmailProvider, IWhatsAppProvider, IInAppNotifier
- Common delivery state machine and normalized delivery receipts.
- Webhook endpoints for delivery callbacks and inbound replies.

Acceptance Criteria
- Outbound send works for each channel in sandbox/stub mode.
- Delivery receipts update delivery attempts.

Cursor Prompt
```text
Create provider interfaces and a normalized delivery model.
Implement initial providers:
- Email: SendGrid (or SMTP abstraction)
- SMS: Twilio (or equivalent)
- WhatsApp: WhatsApp Business API provider abstraction (initially stub + config)
- In-app: Notification table + websocket/signalr push
Implement webhook controllers to process delivery callbacks and inbound replies; store raw payload for audit.
```

---

## A3. Translation + Localization Loop
### A3.S1 — Auto-translation and dual-storage
Requirements
- Store original text and translated text per recipient language.
- Inbound messages stored in original + translated-to-staff-default language.
- Translation caching to reduce cost.
- Human override option (staff can edit translation before send).

Acceptance Criteria
- Guardian receives messages in preferred language.
- Staff can view both original and translated content.

Cursor Prompt
```text
Implement ITranslationService with caching.
Add MessageLocalizedText records per message per language.
Add UI toggles: view original vs translated.
Add optional 'Review translation before send' feature flag per school.
```

---

## A4. Segmentation + Campaigns + Templates Governance
### A4.S1 — Audience Segments
Requirements
- Saved segments with filters: school, year, class/form, attendance-risk flag, case status, tags.
- Segment snapshots for campaigns (audit stable recipient lists).

Acceptance Criteria
- Bulk send uses a segment snapshot; later SIS changes do not alter “who was targeted” historically.

Cursor Prompt
```text
Create AudienceSegment (definition json) and AudienceSnapshot (materialized recipient list).
Build a query engine to resolve segments.
Add UI: segment builder + preview count + validation.
```

### A4.S2 — Template Versioning + Approvals
Requirements
- Message templates with merge fields, per-tenant governance.
- Versioning with approvals; ability to lock templates at district level.
- Per-school overrides where allowed.

Acceptance Criteria
- Template changes are audited and revertible.
- Sends record template version used.

Cursor Prompt
```text
Extend MessageTemplate to include:
- Version, Status(Draft/Approved/Retired), ApprovedBy, ApprovedAtUtc
- MergeFieldSchemaJson
- LockScope(DistrictOnly/SchoolOverrideAllowed)
Update send pipeline to bind a specific approved template version.
Add admin UI: template library, diff viewer, approve workflow, rollback.
```

---

## A5. Engagement Analytics + Reachability
### A5.S1 — Engagement metrics pipeline
Requirements
- Track delivered/opened/clicked/replied/failed per recipient.
- Identify unreachable guardians (persistent failures, invalid numbers, bounce).
- Dashboards by school/year/segment/campaign.

Acceptance Criteria
- You can answer: “who didn’t get it?” “who never replies?” “which channel performs best?”

Cursor Prompt
```text
Implement engagement tracking:
- Email open/click via tracking links/pixel
- Reply detection for SMS/WhatsApp
- Failure categorization and reachability scoring per guardian
Create dashboards and exports; add 'Unreachable' work queue.
```

---

## A6. Family Portal (Full product, not MVP)
### A6.S1 — Guardian authentication and access control
Requirements
- Guardian identity verification and onboarding flow.
- Access only to linked students.
- Preference/consent management.
- In-app inbox and notifications.

Acceptance Criteria
- Guardian can sign in, see messages, reply, change preferences, download documents.
- Full audit log of portal activity.

Cursor Prompt
```text
Build Family Portal:
- Auth: magic link + optional MFA, or SSO where district supports
- RBAC: Guardian role, strict student link enforcement
- Pages: Inbox, Thread view, Documents, Preferences/Consent, Notifications
Implement API endpoints with rate limiting and abuse prevention.
```

---

# EPIC B — Attendance Intervention Orchestrator (Attend-equivalent)

## B1. Attendance Data Normalization
### B1.S1 — Normalize attendance events for rule evaluation
Requirements
- Store per-day attendance states, sessions (AM/PM), and reason codes.
- Support UK/Ireland session models (configurable).
- Keep raw SIS payloads for traceability.

Acceptance Criteria
- A consistent internal schema exists across Tyro/VSware/Compass/Wonde.

Cursor Prompt
```text
Create AttendanceEventNormalized and AttendanceDailySummary entities.
Add mapping layer per SIS connector to map raw attendance to normalized events.
Support configurable session model per tenant/school.
Store raw payload references for auditing.
```

---

## B2. Rule Engine + Intervention Stages
### B2.S1 — Rule sets per jurisdiction and per school
Requirements
- Rules based on absence count, % attendance, consecutive absences, tardy counts, date window.
- Stage ladder configurable: Letter1, Letter2, Meeting/Conference, Escalation.
- Stop conditions: improved attendance, resolved case, exemption flags.
- Escalation triggers: chronic absence threshold, safeguarding flags, non-response.

Acceptance Criteria
- Daily job produces “today’s intervention queue” deterministically.
- Staff can simulate rules (“what would happen if…”) in admin UI.

Cursor Prompt
```text
Implement InterventionRuleSet + InterventionStage + RuleCondition model.
Build rule evaluation service that runs:
- nightly scheduled job
- on-demand for a student
- on SIS sync completion
Add simulation endpoint and admin UI for preview.
```

---

## B3. Letter Artifacts (PDF) + Template Governance
### B3.S1 — Letter templates with merge fields + translation
Requirements
- PDF letters generated from template and merge data (student, guardian, school, attendance metrics).
- Store generated artifact + hash.
- Template version locked to stage run.
- Translation based on guardian preferred language.

Acceptance Criteria
- For an intervention run, artifacts can be re-downloaded and match hash.
- Template and merge values used are auditable.

Cursor Prompt
```text
Implement LetterTemplateVersion + LetterArtifact.
Build PDF generation pipeline (server-side).
Bind artifacts to StudentInterventionInstance + InterventionEvent.
Translate content per guardian language; store both source + translated PDFs if required.
```

---

## B4. Conferences/Meetings + Outcomes
### B4.S1 — Meeting scheduling + minutes + outcomes
Requirements
- Track meeting scheduling, attendees, notes, outcomes.
- Link meeting to intervention stage.
- Create follow-up tasks automatically.

Acceptance Criteria
- Meeting is visible on Student Support Timeline.
- Outcomes influence next stage (stop/escalate).

Cursor Prompt
```text
Add InterventionMeeting entity linked to InterventionInstance.
Add outcomes taxonomy (Resolved/MedicalPlan/SupportPlan/Escalate etc.).
Auto-create Task items on meeting completion.
Expose meeting events in timeline.
```

---

## B5. Dashboards + Scheduled Reports (final-product)
### B5.S1 — Dashboards with intervention attribution
Requirements
- KPIs: attendance rate, persistent absence, cohort changes after interventions.
- Drilldown: district → school → year → class → student.
- Attribution: which stage preceded improvement.

Acceptance Criteria
- Dashboards can be filtered by rule set, stage, time window, segment.

Cursor Prompt
```text
Implement analytics tables/materialized views for attendance + intervention attribution.
Build dashboards with drill-down and exports.
Include reachability overlays (who couldn't be contacted).
```

### B5.S2 — Scheduled “analysis report” generator
Requirements
- Configurable schedule (e.g., 3x/year default, but editable).
- Outputs PDF + spreadsheet.
- Includes narrative sections + charts + cohort comparisons.

Acceptance Criteria
- Reports generate automatically and are stored with an immutable snapshot ID.

Cursor Prompt
```text
Build scheduled report service:
- ReportDefinition + ReportRun + ReportArtifact
- Templates for Attendance Analysis Report
- Scheduled job + manual trigger
Store immutable snapshot of data used; allow download.
```

---

# EPIC C — Hybrid Multi-touch Playbooks (Attend Hybrid-equivalent)

## C1. Playbook Engine (multi-channel sequences)
### C1.S1 — Sequence definition and runner
Requirements
- Define sequences: after Stage X, send Y messages at offsets (D+1 SMS, D+3 WhatsApp, D+7 email).
- Personalize and translate each touch.
- Stop conditions: attendance improvement, guardian reply, case closed.
- Escalation: create meeting task after non-response.

Acceptance Criteria
- A playbook runs automatically from stage events.
- No duplicate touches even if jobs retry.

Cursor Prompt
```text
Create PlaybookDefinition + PlaybookStep + PlaybookRun + PlaybookExecutionLog.
Implement runner service triggered by InterventionEvent.
Ensure idempotency per (student, step, scheduledAt).
Integrate with outbox send pipeline.
```

## C2. Operational Telemetry (time saved + ROI)
### C2.S1 — Automation metrics
Requirements
- Track manual time avoided: “would have taken X minutes per letter/message” configurable.
- Track touches delivered and resulting attendance changes.

Acceptance Criteria
- District admin can export ROI report and evidence.

Cursor Prompt
```text
Add TelemetryEvent model and ROI calculator.
Capture: touches scheduled/sent, staff time saved estimates, attendance deltas.
Add exportable ROI dashboard and report.
```

---

# EPIC D — MTSS Attendance Layer + Evidence Packs (Ireland/UK)

## D1. MTSS Tier Model and Intervention Library
### D1.S1 — Tier configuration and mapping
Requirements
- Tier 1/2/3 definitions per tenant, with recommended interventions.
- Mapping from rule sets and stages to tiers.
- Entry/exit criteria and required artifacts per tier.

Acceptance Criteria
- Students can be shown as Tier 1/2/3 with explainable reasons and history.

Cursor Prompt
```text
Create MtssTier, MtssIntervention, MtssCriteria, TierAssignmentHistory.
Map InterventionStages to tiers; auto-assign based on attendance metrics and stage progression.
Expose tier rationale in UI.
```

## D2. Evidence Packs (final-product)
### D2.S1 — One-click evidence pack for a student/case
Requirements
- Select date range + include/exclude categories.
- Auto-include: attendance charts, stage artifacts, comms transcript excerpts, meeting outcomes, tasks completed.
- Produce PDF bundle with index + hashes for integrity.

Acceptance Criteria
- Evidence pack can be regenerated and compared via hashes.
- Export is permission-restricted and audited.

Cursor Prompt
```text
Implement EvidencePackBuilder:
- Inputs: studentId, dateRange, scope options
- Outputs: PDF (index + sections) and zip (raw artifacts)
Include integrity hashes and an export audit log entry.
```

---

# EPIC E — Student Support Timeline (Differentiator)

## E1. Unified timeline API and UI
### E1.S1 — Append-only timeline event stream
Requirements
- Timeline merges: normalized attendance events, intervention events, messages, delivery receipts, meeting notes, tasks, evidence exports.
- RBAC-filtering per role (e.g., safeguarding notes restricted).
- Full-text search for staff with permissions.

Acceptance Criteria
- Single screen tells the complete story for a student.
- Export of timeline is possible with redaction rules.

Cursor Prompt
```text
Create TimelineEvent table (append-only) with Type, EntityRef, OccurredAtUtc, MetadataJson, VisibilityScope.
Implement projectors from each module to timeline events.
Build UI: Student Support Timeline with filters and search.
```

---

# EPIC F — SIS Connector Ecosystem (Tyro, VSware, Compass, Wonde)

## F1. Connector framework + health monitoring
### F1.S1 — Pluggable connectors with capabilities
Requirements
- ISisConnector with capabilities: RosterSync, ContactsSync, AttendanceSync, ClassesSync, TimetableSync.
- Sync runs produce logs, metrics, mismatches, alerts.
- Store raw sync payloads with retention policies.

Acceptance Criteria
- Each connector can be enabled per tenant/school.
- Operator can see health status and last successful sync.

Cursor Prompt
```text
Refactor SIS sync into connector framework:
- ISisConnector + capability interfaces
- SyncRun + SyncMetric + SyncError tables
Extend IngestionSyncLog to include mismatch metrics and alert thresholds.
Create admin UI: connector config + health dashboard + manual resync.
```

## F2. Implement connectors
### F2.S1 — Wonde integration (UK first)
Cursor Prompt
```text
Implement Wonde connector:
- OAuth/API key config per tenant
- Roster, contacts, attendance sync
- Mapping to normalized attendance schema
Include paging, rate limits, retry, and delta sync where possible.
```

### F2.S2 — Tyro, VSware, Compass Education
Cursor Prompt
```text
Implement connectors for Tyro, VSware, Compass.
If APIs differ, create adapter layers and normalize outputs into the same internal schemas.
Add integration tests with recorded fixtures.
```

---

# EPIC G — Governance, Admin, Operations (Final-product readiness)

## G1. RBAC + permissions down to feature/action
Requirements
- Permission matrix: view student, view comms, send comms, approve templates, export evidence, administer SIS connectors, view audit logs.
- School boundary enforcement.

Cursor Prompt
```text
Implement Permission model (scopes + actions).
Add policy-based authorization across API and UI.
Add admin screen: permission matrix per role with export/import.
```

## G2. Audit Logging (GDPR-friendly)
Requirements
- Log sensitive reads (student record views, message content access), exports, and administrative changes.
- Tamper-resistant storage (append-only + hashes optional).

Cursor Prompt
```text
Implement AuditEvent (append-only) with Actor, Action, EntityRef, Timestamp, Metadata.
Instrument key endpoints and screens.
Add audit search UI with filters and export.
```

## G3. Observability + Alerting
Requirements
- Central logging, metrics, traces.
- Alert rules: SIS sync failures, outbox DLQ growth, message deliverability drop.

Cursor Prompt
```text
Add structured logging + metrics.
Create health endpoints and alert policies.
Build ops dashboard: SIS sync health, outbox status, deliverability stats.
```

---

## 7. Cursor Operating Instructions (How to keep Cursor on track)
### 7.1 Branching and PR discipline
- Branch name: epic-<letter>-<short-name> or story-<id>
- Every PR must update:
  - docs/CHANGELOG.md
  - docs/ARCH_DECISIONS.md (if architecture changes)
  - docs/API.md (if endpoints added)

### 7.2 Definition of Done for every story
- Unit tests for core services
- Integration tests for connectors/webhooks
- Audit and permission checks verified
- Background jobs idempotent and observable
- UI screens include empty/error/loading states
- Documentation updated

### 7.3 Prompting style for Cursor (recommended)
When pasting a task to Cursor, append:
- “Do not guess. Search the solution for existing patterns and follow them.”
- “Write tests. Ensure migrations are deterministic.”
- “Update docs and ensure build passes.”

---


---

## High-level “To-Do” table to reach final product
This table compares what the current system includes (per the Cursor system docs) against the final-product target in this backlog, and maps each gap to the relevant **Epic / Story / Task** IDs in this document.

> **Note on Task IDs:** Stories in this document include one or more concrete implementation tasks. In this table, “Task” references the primary task implied by the story’s Cursor Prompt (e.g., “entities+migration”, “service+job”, “UI screen set”).

| Area | Current (from Cursor docs) | Missing to reach final product | High-level To-Dos | Epic | Story | Task (primary) | Priority | Key dependencies |
|---|---|---|---|---|---|---|---|---|
| Comms hub foundation (Threads + Guardians) | Messaging exists; case-linked comms; student↔guardian linkage noted | No true conversation threads, participants, ownership/tagging, global thread search | Introduce ConversationThread/Participants; migrate message model to thread-first; build staff inbox + thread UI + search | EPIC A | A1.S1 | A1.S1-T1 Data model + migration | P0 | RBAC (EPIC G), Provider pipeline (A2) |
| Outbox + send reliability | Sends exist; delivery receipts limited | No transactional outbox, idempotency, retries, DLQ, operator tooling | Implement OutboxMessage + dispatcher; enforce queue-only sends; add DLQ triage + replay UI | EPIC A | A2.S1 | A2.S1-T1 Outbox tables + dispatcher + UI | P0 | Provider interfaces (A2.S2), Observability (G3) |
| Provider coverage (SMS/Email/WhatsApp/In-app) | SMS primary; Email optional; WhatsApp optional | Unified provider abstraction, normalized receipts, inbound webhooks + reply ingestion across channels | Implement provider interfaces; normalize receipts; add webhook endpoints; add in-app notifications (SignalR) | EPIC A | A2.S2 | A2.S2-T1 Providers + webhooks + receipt normalizer | P0 | Outbox (A2.S1), Consent enforcement (A1.S2) |
| Consent + preference center (GDPR-grade) | Consent state model mentioned | Consent enforcement everywhere; preference center (quiet hours/channel priority); audited change history | Add ContactPreference + ConsentRecord; enforce consent in send pipeline; staff UI now, family self-service later | EPIC A | A1.S2 | A1.S2-T1 Consent + preference entities + enforcement | P0 | Family portal (A6), Audit logging (G2) |
| Translation loop (outbound + inbound) | Not confirmed implemented | Auto-translation with dual-storage per recipient language + inbound translate-to-staff language | Implement ITranslationService + caching; store localized text; UI toggles; optional “review before send” | EPIC A | A3.S1 | A3.S1-T1 Translation service + storage + UI | P1 | Threads (A1.S1), Providers (A2.S2) |
| Segmentation + campaigns | Not described | Saved segments; campaign sends; recipient snapshots for audit | Build segment builder + resolver; snapshot recipient list per campaign; campaign entity | EPIC A | A4.S1 | A4.S1-T1 Segment engine + snapshot | P1 | Student/guardian model (A1), Attendance risk flags (B2/B5) |
| Template governance (versioning/approvals) | Identified as a gap | Versioning, approvals, lock scope, per-school overrides, rollback | Extend MessageTemplate with versions/status/approvals; bind sends to approved version; admin UI for diff/rollback | EPIC A | A4.S2 | A4.S2-T1 Template governance workflow | P0 | RBAC (G1), Audit (G2) |
| Engagement analytics + reachability | Receipts limited; reporting minimal | Delivered/opened/replied/failed metrics; unreachable work queue; channel performance | Implement engagement events; email open/click; reply detection; bounce handling; reachability scoring dashboards | EPIC A | A5.S1 | A5.S1-T1 Metrics pipeline + dashboards | P1 | Providers+webhooks (A2.S2), Data model (A1) |
| Family portal (guardian UX) | Staff-only | Guardian auth, inbox/reply, documents, preferences/consent, notifications | Build guardian portal with strict student-link enforcement; pages: Inbox/Thread/Documents/Preferences/Notifications | EPIC A | A6.S1 | A6.S1-T1 Family portal app + APIs | P1 | Threads (A1), Consent (A1.S2), Security (G1/G2) |
| Attendance normalization (multi-SIS) | Attendance marks exist; Wonde-first read-only | Normalized events across Tyro/VSware/Compass/Wonde; raw payload retention; consistent reason mapping | Add normalized attendance schema + daily summaries; per-connector mappers; configurable session model (UK/IE) | EPIC B | B1.S1 | B1.S1-T1 Normalize schema + mapper layer | P0 | Connector framework (F1), Reason codes taxonomy |
| Intervention rules engine + stage ladder | Tiered case mgmt exists | Deterministic eligibility engine; stage ladder; simulation tooling; “today’s queue” | Implement rule sets/stages/conditions; nightly + on-sync evaluation; simulation UI | EPIC B | B2.S1 | B2.S1-T1 Rule engine service + jobs + UI | P0 | Normalized attendance (B1), Policy governance (A4.S2 / G) |
| Letter artifacts (PDF) + template binding | Message templates exist; letters not confirmed | PDF generation per stage with template version; translations; artifact integrity hashes | Implement LetterTemplateVersion + PDF pipeline; store artifacts + hashes; bind to intervention events | EPIC B | B3.S1 | B3.S1-T1 PDF generation + storage + hashing | P1 | Rule engine (B2), Translation (A3) |
| Conferences/meetings + outcomes | Case tasks/checklists exist | Meeting scheduling + structured outcomes driving stop/escalate; timeline projection | Add meeting entity linked to intervention; outcomes taxonomy; auto-create tasks; project to timeline | EPIC B | B4.S1 | B4.S1-T1 Meetings + outcomes + task automation | P2 | Rule engine (B2), Timeline (E1) |
| Hybrid multi-touch playbooks | Not present | Automated sequences across channels; stop/escalate conditions; execution logs | Implement playbook definitions + runner triggered by intervention events; idempotent step execution | EPIC C | C1.S1 | C1.S1-T1 Playbook engine + runner | P2 | Outbox (A2), Intervention events (B2/B3) |
| ROI / automation telemetry | Not present | Time-saved and impact telemetry + exportable ROI report | Add telemetry events + ROI calculator; dashboards and exports | EPIC C | C2.S1 | C2.S1-T1 Telemetry + ROI reporting | P2 | Playbooks (C1), Analytics (B5) |
| Dashboards with attribution | Reporting minimal | Drilldown dashboards; intervention attribution; reachability overlays | Build analytics views/materializations; dashboards with drilldown and filters; exports | EPIC B | B5.S1 | B5.S1-T1 Analytics model + dashboards | P2 | Normalization (B1), Engagement metrics (A5) |
| Scheduled analysis reports | Not present | Configurable scheduled PDF+spreadsheet reports with immutable snapshots | Implement report definitions/runs/artifacts; scheduled job + manual trigger | EPIC B | B5.S2 | B5.S2-T1 Scheduled report generator | P2 | Analytics (B5.S1), Artifact storage |
| MTSS tier model + intervention library | Tiered case mgmt exists | Configurable MTSS tiers, criteria, tier history; mapping stages to tiers | Implement MTSS tier entities + mapping; show rationale and history in UI | EPIC D | D1.S1 | D1.S1-T1 MTSS model + UI | P2 | Rule engine (B2), Timeline (E1) |
| Evidence packs | Entity planned; not confirmed implemented | One-click export with index + integrity hashes; strict permissions + audit | Implement EvidencePackBuilder producing PDF bundle + zip; audit exports | EPIC D | D2.S1 | D2.S1-T1 Evidence generator + export | P2 | Timeline (E1), Artifacts (B3), Audit (G2) |
| Unified Student Support Timeline | Case timeline exists but not unified | Single merged timeline with RBAC filtering + search + redacted export | Implement TimelineEvent + projectors; Student Support Timeline UI + exports | EPIC E | E1.S1 | E1.S1-T1 Timeline event stream + UI | P2 | Most modules (A/B/C/D), RBAC (G1) |
| Connector framework + health monitoring | SIS sync partial | Pluggable connectors; sync runs/metrics/errors; admin UI; alerts | Refactor sync into connector framework; add health dashboard; manual resync | EPIC F | F1.S1 | F1.S1-T1 Connector framework + ops UI | P0 | Observability (G3), Normalization (B1) |
| Wonde connector hardening (UK first) | Wonde-first exists | Full roster/contacts/attendance mapping, paging, rate limiting, delta sync | Implement/complete Wonde connector with robust sync semantics and tests | EPIC F | F2.S1 | F2.S1-T1 Wonde connector implementation | P1 | Connector framework (F1), Normalization (B1) |
| Tyro / VSware / Compass connectors | Not complete | Connectors implemented with mapping and fixtures | Implement connectors + adapters; normalize into same schema; integration tests | EPIC F | F2.S2 | F2.S2-T1 Tyro/VSware/Compass connectors | P1 | Connector framework (F1), Normalization (B1) |
| RBAC permissions matrix | RBAC exists | Fine-grained action-level permissions and admin UI | Implement Permission scopes/actions; enforce policies; admin UI for role matrix | EPIC G | G1 | G1-T1 Permission model + UI | P0 | All epics |
| Audit logging (reads + exports) | Some auditing mentioned; unclear depth | Append-only audit for sensitive reads/exports/admin changes; searchable UI | Implement AuditEvent; instrument endpoints/screens; build audit viewer/export | EPIC G | G2 | G2-T1 Audit pipeline + UI | P0 | RBAC (G1), Evidence exports |
| Observability + alerting | Not fully implemented | Health endpoints; alerting on sync failures/DLQ/deliverability | Add logging/metrics; ops dashboard; alert policies | EPIC G | G3 | G3-T1 Ops dashboard + alerts | P1 | Outbox (A2), Connectors (F1) |
| API maturity (pagination/filters) | Pagination limits and filtering gaps noted | Cursor pagination for large datasets; robust filtering/sorting | Implement stable pagination + query filters; add indexes and perf tests | Cross-cutting | (supports many) | API-T1 Pagination/filter refactor | P0 | UI, reporting, exports |

## 8. Delivery Order (Strong recommendation)
1) EPIC A (Comms Hub foundation) + EPIC G (RBAC/Audit baseline)
2) EPIC B (Attend rule engine + letters + dashboards)
3) EPIC C (Hybrid playbooks)
4) EPIC E (Student Support Timeline)
5) EPIC D (MTSS layer + evidence packs)
6) EPIC F (Connector breadth + hardening)

Reason: Without Comms Hub + RBAC/Audit, attendance orchestration won’t be trusted or adoptable.

---

## 9. Open Decisions (decide early)
- WhatsApp provider: Meta Cloud API vs third-party (affects consent flows and webhook formats)
- Translation provider: Azure vs Google vs DeepL (cost + accuracy + DPA)
- Guardian authentication method: magic link vs OTP vs district SSO
- Data retention defaults per tenant (GDPR)
- Jurisdiction pack design: ship Ireland/UK defaults without hardcoding
