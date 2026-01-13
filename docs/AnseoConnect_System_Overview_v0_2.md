# Anseo Connect — System Overview & Architecture (v0.2)

## 1) Audience & purpose
- Product/ops leaders: understand current capabilities and behaviour for schools/guardians.
- Engineers/architects: see how the current implementation is assembled (services, data, integrations) to guide next-phase design.

## 2) Current scope (v0.1 feature set)
- Attendance focus: same-day unexplained absences, AM/PM sessions; read-only from SIS via Wonde.
- Channels: SMS (Sendmode) primary; Email (SendGrid) optional; WhatsApp via Twilio optional; Voice not yet active.
- Case management: Tiered (Tier 1/2/3), timeline, tasks/checklists; safeguarding alerts with mandatory human review.
- UI: Blazor Web host (`AnseoConnect.Web`) using shared DevExpress RCL (`AnseoConnect.UI.Shared`); no guardian portal.
- Auth: Local JWT + Microsoft Entra; RBAC roles (AttendanceAdmin, YearHead, DLP, Principal, DeputyPrincipal, ETBTrustAdmin).

## 3) Personas & journeys (school/guardian view)
- Attendance Admin: opens Today, bulk messages guardians, records replies.
- Year Head: monitors Tier 2 plans, completes checklists, escalates.
- DLP/Safeguarding: reviews alerts, completes safeguarding checklist, records actions.
- Principal/Deputy: reporting, settings, governance, ETB/Trust roll-ups.
- Guardian: receives SMS/email, replies with reason, can opt out per channel.

## 4) User-facing flows (current behaviour)
- Sign-in: Local or Entra; single-school auto-selected; multi-school users see School Switcher.
- Today page: unexplained absences, due tasks, safeguarding alerts, quick-send SMS/email from templates with consent/fallback checks.
- Messaging: pick channel + template → preview → send; statuses (Queued → Sent → Delivered/Failed). SMS replies land in timeline; email replies currently manual/assisted.
- Cases: auto-open on policy thresholds or manually; timeline shows messages/replies/notes/tier changes; checklists enforce Tier 2 and safeguarding steps.
- Tier 2 plan: required checklist items, tasks assigned to roles, blocking rules before close/escalate.
- Safeguarding: deterministic triggers create restricted alerts routed to configured recipients; acknowledgement + checklist required.
- Students area: per-student attendance snapshot, guardians/consent, cases/messages.
- Reports (MVP): same-day contact rate, reply rate, opt-out rate, open cases by tier, safeguarding counts.
- Settings: cutoff times, channel priority, timezone, safeguarding recipients; policy pack/version selection; integration health (Wonde sync status).

## 5) Architecture (context + containers)
- Web UI: Blazor server host `AnseoConnect.Web`; DevExpress components in `AnseoConnect.UI.Shared`; typed client in `AnseoConnect.Client`.
- API Gateway `AnseoConnect.ApiGateway`: ASP.NET Core REST + OpenAPI; dual auth (Local JWT & Entra); policies for staff/safeguarding/reporting/settings; sets TenantContext from JWT claims; exposes case/reporting/taxonomy queries and webhook endpoints.
- Workflow service `AnseoConnect.Workflow`: Domain orchestration; services for absence detection, case/tier transitions, safeguarding evaluation, review windows, evidence packs, task scheduling; hosted consumers for attendance ingested, message events, task due.
- Comms service `AnseoConnect.Comms`: Outbound channels; Sendmode SMS, SendGrid email (optional), Twilio WhatsApp (optional); consent + template engine; hosted consumer for send-message requests; publishes delivery/reply/opt-out events.
- Ingestion service `AnseoConnect.Ingestion.Wonde`: Pulls SIS data via Wonde API; publishes attendance ingested events; exposes admin endpoints.
- Shared libraries:
  - `AnseoConnect.Data`: EF Core DbContext with tenant/school filters + write enforcement; identity tables; core entities (Student, Guardian, AttendanceMark, ConsentState, Message, Case, CaseTimelineEvent, SafeguardingAlert, WorkTask, Checklist, ReasonCode, EvidencePack, MessageTemplate, NotificationRecipient, ETBTrust, Notification).
  - `AnseoConnect.PolicyRuntime`: Consent and safeguarding evaluation, template engine.
  - `AnseoConnect.Shared`: Azure Service Bus publisher (`ServiceBusMessageBus`), contracts/messages, tenant context abstractions.
- Infrastructure: Azure SQL (multi-tenant schema with query filters), Azure Service Bus topics (`attendance`, `comms`, `workflow`), Wonde API, Sendmode, SendGrid, Twilio. Deployable as .NET 10 container/App Service workloads.

## 6) Data & policy flow
- Ingestion: Wonde sync (scheduled/manual) pulls students/guardians/attendance; writes to SQL with tenant/school scoping; logs in `IngestionSyncLog`; can pause automation on degraded health.
- Policy packs (`policy-packs/...`): JSON rules for reason taxonomy, consent, triggers, templates, checklists. Schools select a version; changes auditable.
- Consent: evaluated per-channel via policy pack (explicit opt-in required for WhatsApp/Voice; SMS/Email follow policy defaults).
- Messaging lifecycle: API request → consent/policy check → template merge → publish `SendMessageRequested` to Service Bus → Comms service sends via provider → delivery/opt-out/reply events publish back to Workflow → case timeline/task updates.
- Case lifecycle: thresholds/timers create or advance cases; tasks/checklists enforced (Tier 2, safeguarding); evidence pack generation service available.

## 7) Security, tenancy, RBAC
- TenantContext set from JWT claims (`tenant_id`, `school_id`); mandatory for DB writes; EF global filters enforce tenant/school on queries; writes enforce matching tenant/school.
- Auth schemes: `LocalBearer` (HMAC JWT with configured secret) and Entra OIDC; default policy requires auth + tenant claim.
- Role-based policies: AttendanceAccess, CaseManagement, SafeguardingAccess, ReportingAccess, ETBTrustAccess, SettingsAdmin.
- Data boundaries: No cross-tenant reads/writes; AppUser indexes scoped by tenant/school; safeguarding data marked as restricted case type.
- Auditability: Timeline events, messages, opt-outs, checklist completions, sync logs; identity-backed actions.

## 8) Data model highlights (current)
- Attendance: `AttendanceMark` keyed per student/date/session; unique index prevents duplicates.
- Messaging: `Message` with indexes by case/guardian/time; statuses captured; `ConsentState` per guardian/channel; `MessageTemplate` per tenant/channel.
- Cases & Tasks: `Case` with type/tier/status indexes; `CaseTimelineEvent` history; `WorkTask` with due/status index; `WorkTaskChecklist` + `ChecklistCompletion`.
- Safeguarding: `SafeguardingAlert` linked to case with review requirement; checklist enforcement supported.
- Schools/Tenancy: `Tenant`, `School` (Wonde ID index), `ETBTrust`; `StudentGuardian` linking with guarded delete rules.

## 9) Key runtime sequences
- Daily absence loop: Wonde sync → AttendanceMarksIngested event → Workflow AbsenceDetection → Case create/update → tasks/checklists → notifications routed.
- Message send: UI/API call → policy/consent check → template merge → publish SendMessageRequested → Comms sender (Sendmode/SendGrid/Twilio) → delivery/reply/opt-out events → Workflow MessageEventConsumer → CaseTimeline update + status changes.
- Safeguarding alert: Workflow evaluates patterns/keywords → creates restricted alert + checklist → notifications to recipients → human completes checklist → close/update.

## 10) Operations & configuration
- Config via `appsettings` or env vars: `ANSEO_SQL`, `ANSEO_SERVICEBUS`, JWT secret/issuer/audience, Wonde token/domain, Sendmode creds, SendGrid creds, Twilio creds.
- Health surfaces: Wonde sync health (last sync, errors, mismatch, automation pause), message status tracking; OpenAPI exposed in Development.
- Observability: Structured logging present; metrics/alerts not yet defined (to add for ingestion/comms failures, task backlog, safeguarding SLA).

## 11) Known limitations / gaps
- Email inbound handling is manual; full automated email reply ingestion not implemented.
- Message status depends on provider support (Sendmode delivery receipts limited).
- Case listing API currently supports only `status=OPEN`; pagination capped at 100.
- No guardian-facing portal; no SIS write-back.
- Tier/alert rules rely on policy packs; need governance for versioning and rollout.
- Limited reporting set; analytics lakehouse not implemented yet.
- Per-school cutoff/automation pause needs clear ops runbooks and alerts.
- AI assist not present in code yet (design brief only).

## 12) Next-phase considerations
- Expand inbound email handling and WhatsApp/Voice channel enablement with consent safeguards.
- Add observability (metrics, traces, dashboards) for ingestion/comms/workflow; alert on failures and backlog growth.
- Harden multi-tenant isolation tests and pen-test surfaces (webhooks, message ingestion).
- Enhance reporting (trends, cohort comparisons, evidence pack exports) and analytics store.
- Automate policy pack management (version promotion, drift detection, school-specific overrides).
- Reliability: retries/dead-letter handling per Service Bus topic, idempotency checks on consumers.
- User experience: guardian contact fallback rules, bulk actions, checklist UX, DevExpress UI consistency.
- Data lifecycle: retention, export, and deletion policies per GDPR; audit exports.
