# Anseo Connect v0.1 Build Plan (Cursor Instructions)

## Purpose
Build the first end-to-end “attendance improvement loop” for pilot schools in Ireland:

1) ingest AM/PM attendance + people from Wonde  
2) detect same-day unexplained absences after a school cutoff  
3) send guardian messaging via consent rules (SMS first)  
4) log delivery + replies + opt-outs  
5) create/maintain attendance cases + Tier 2 escalation  
6) raise safeguarding alerts from policy triggers (human review)

**Non-goals (v0.1):** full dashboards, advanced analytics, WhatsApp/Voice in production, multi-SIS beyond Wonde.

---

## Repository context (assumed)
Solution: `AnseoConnect` with projects similar to:
- `./src/Services/AnseoConnect.ApiGateway`
- `./src/Services/AnseoConnect.Ingestion.Wonde`
- `./src/Services/AnseoConnect.Workflow`
- `./src/Services/AnseoConnect.Comms`
- `./src/Shared/AnseoConnect.Contracts`
- `./src/Shared/AnseoConnect.Data`
- `./src/Shared/AnseoConnect.PolicyRuntime`
- `./tools/Tools/PolicyPackTool`
- `policy-packs/` + `policy-packs/schema/`

**Hard constraints**
- No secrets committed (Twilio keys, Wonde keys, SQL password). Use environment variables / user secrets.
- CI must stay green:
  - `PolicyPackTool validate policy-packs`
  - `PolicyPackTool test policy-packs`

---

## Global rules for Cursor (very important)
When working on this plan:

1) **Do one step at a time.**  
   Don’t “also refactor” or “also add features”. Each step ends with compilation + minimal tests.

2) **Always add acceptance criteria + how to run locally** for each step.

3) **Use Contracts for all Service Bus payloads** (no anonymous JSON blobs).  
   Version messages (`V1`, `V2`) rather than editing in-place.

4) **Tenant + School scoping is mandatory** for every DB read/write.  
   Use `TenantContext` and EF enforcement already added.

5) **Keep policy behavior in policy packs.**  
   Code should load policy and apply it, not hardcode thresholds/consent rules.

---

## Dev configuration (local)
Use environment variables (examples):

- `ANSEO_SQL`  
  `Server=JHP;Database=AnseoConnectDev;User Id=sa;Password=***;TrustServerCertificate=True;`

- `ANSEO_SERVICEBUS`  
  `Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=...`

- `WONDE_TOKEN`, `WONDE_SCHOOL_ID` (or per-tenant settings)

- `TWILIO_ACCOUNT_SID`, `TWILIO_AUTH_TOKEN`, `TWILIO_FROM_NUMBER`

---

# Step 0 — Authentication baseline (Web + Mobile)

## Goal
Your APIs must support:
- **Microsoft Entra ID login** (Enterprise App / SSO)
- **Non-SSO login** (username/password) for staff/guardians (staff-only for phase 1 per your earlier constraint)

### Recommended approach (v0.1)
In `ApiGateway`:
- Support **two authentication schemes**:
  1) Entra JWT Bearer (tokens from your tenant)
  2) Local JWT Bearer (issued by your own API for non-SSO accounts)

Do **not** build a full UI yet. Just get auth wired and protect endpoints.

## Cursor tasks
1) Add auth packages:
   - `Microsoft.Identity.Web` (Entra)
   - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (local users in Azure SQL)
2) Create DB entities for local staff accounts (minimal):
   - `AppUser` (IdentityUser<Guid>) scoped to TenantId/SchoolId
3) Add endpoints:
   - `POST /auth/local/login` → returns JWT
   - `POST /auth/local/register` (optional, or admin-only)
4) Configure Entra:
   - `AddAuthentication().AddMicrosoftIdentityWebApi(...)`
5) Add authorization policies:
   - `StaffOnly`
6) Add a “whoami” endpoint to confirm:
   - returns user id, tenant id, school id claims

## Acceptance criteria
- Entra token can access a protected endpoint
- Local login returns a token and can access same endpoint
- No secrets committed

> Note: Mobile will use MSAL for Entra and normal username/password for local login (API-issued JWT). Cursor does not need to build mobile yet—just ensure API supports both flows.

---

# Step 1 — Service Bus wiring (reliable messaging)

## Goal
Standardize how services publish/consume messages:
- serialization
- message headers (TenantId, SchoolId, CorrelationId)
- retries + dead-letter behavior
- local developer runbook

## Cursor tasks
1) Add a shared library module (or in `Shared/AnseoConnect.Shared`) for:
   - `IMessageBus` abstraction
   - `ServiceBusMessageBus` implementation using `Azure.Messaging.ServiceBus`
   - JSON serialization settings (System.Text.Json)
2) Enforce message envelope:
   - use `MessageEnvelope<T>` in `Contracts.Common`
3) Add correlation + tenant propagation:
   - read from envelope; set `TenantContext` in consumers before DB work
4) Add a simple publisher + consumer sample:
   - publish `AttendanceMarksIngestedV1`
   - consume it in Workflow service and log payload

## Service Bus topology (v0.1)
Use **topics** (future-proof):
- Topic: `attendance` (event: `AttendanceMarksIngestedV1`)
- Topic: `comms` (command: `SendMessageRequestedV1`)
- Topic: `workflow` (events: delivery/reply/opt-out)

## Acceptance criteria
- Ingestion publishes a message
- Workflow consumes it
- TenantContext is set from envelope
- Consumer handles poison messages (dead-letter)

---

# Step 2 — Wonde ingestion skeleton (people + attendance)

## Goal
Ingestion service can:
- connect to Wonde
- pull students + guardians
- upsert into Azure SQL
- pull AM/PM marks
- upsert attendance marks idempotently
- publish `AttendanceMarksIngestedV1`

## Cursor tasks
1) Build a Wonde client wrapper:
   - keep Wonde API calls isolated
2) Implement ingestion run:
   - nightly + manual trigger endpoint (for testing)
3) Upsert logic:
   - Students by `(TenantId, SchoolId, ExternalStudentId)`
   - Guardians by `(TenantId, SchoolId, ExternalGuardianId)`
   - AttendanceMarks by unique key `(TenantId, SchoolId, StudentId, Date, Session)`
4) Publish event with counts:
   - `AttendanceMarksIngestedV1(Date, StudentCount, MarkCount, "WONDE")`

## Acceptance criteria
- Running ingestion twice creates no duplicates
- Attendance marks update cleanly
- Event published after ingest

---

# Step 3 — Comms service skeleton (SMS first)

## Goal
Comms service can:
- consume `SendMessageRequestedV1`
- enforce consent rules from policy pack + consent state
- send SMS via Twilio
- persist outbound message
- handle delivery callbacks + replies
- record opt-outs and publish `GuardianOptOutRecordedV1`

## Cursor tasks
1) Implement `ConsentState` read/update logic
2) Implement policy-driven consent gate:
   - SMS allowed unless OPTED_OUT
   - WhatsApp/Voice blocked unless OPTED_IN (even if not used yet)
3) Implement Twilio sender:
   - send SMS
   - store provider message id
4) **Webhooks hosted in ApiGateway** (confirmed):
   - delivery status updates → publish `MessageDeliveryUpdatedV1` to Service Bus
   - inbound replies → publish `GuardianReplyReceivedV1` to Service Bus
5) Opt-out detection:
   - if reply matches opt-out keywords → update ConsentState to OPTED_OUT
   - publish `GuardianOptOutRecordedV1`

## Acceptance criteria
- A `SendMessageRequestedV1` results in either:
  - sent message recorded + event emitted, OR
  - blocked due to consent with an audit trail
- Delivery updates change message status
- Reply creates a timeline event + opt-out if applicable

---

# Step 4 — Workflow (unexplained absences + cases)

## Goal
Workflow service can:
- consume `AttendanceMarksIngestedV1`
- detect same-day unexplained absences after cutoff (policy/default per school)
- create/open an attendance case per student (if needed)
- request comms (`SendMessageRequestedV1`)
- write case timeline events
- evaluate Tier 2 escalation thresholds (simple v0.1)
- create safeguarding alerts when policy triggers hit

## Cursor tasks
1) Implement “cutoff time” logic:
   - school timezone
   - default cutoff if not configured
2) Unexplained absence detection:
   - absent/unknown with no accepted reason
3) Case creation:
   - create if no OPEN ATTENDANCE case exists
4) Message request:
   - pick guardian(s) to contact (primary guardian first; v0.1 simple)
   - publish `SendMessageRequestedV1`
5) Tier 2:
   - minimal threshold rule from policy pack defaults
   - attach Tier 2 checklist requirement
6) Safeguarding:
   - evaluate policy triggers using the same semantics as your policy pack tests
   - create safeguarding case/alert with severity + checklist mapping

## Acceptance criteria
- After ingestion event, Workflow generates message requests for today’s unexplained absences
- Creates cases + timeline entries
- Safeguarding alerts created when test-like metrics are met

---

# Step 5 — Minimal staff endpoints (pilot-grade)
(Do this only after steps 0–4 work end-to-end)

## Goal
Staff can see what’s happening without a full UI:
- list open cases
- list today’s unexplained absences
- view case timeline
- view consent status
- mark checklist items complete

## Acceptance criteria
- Pilot school staff can operate daily workflow via minimal screens or API client

---

## Definition of Done for v0.1 pilot
- End-to-end loop works for at least one school (Ireland)
- Policy packs govern consent, safeguarding triggers, Tier 2 gating
- All services run locally against JHP SQL Server
- GitHub Actions green
- No secrets in repo

---

## Questions / decisions (safe defaults)
Proceed using these defaults unless changed later:

1) **Service Bus topology:** Topics + subscriptions  
2) **Webhook host:** ApiGateway (confirmed)  
3) **Local user management:** admin-created only in v0.1  
4) **Guardian contact selection:** primary guardian only (config later)  
5) **Message templates storage:** config/policy pack (DB later)

---

## How to proceed (Cursor execution plan)
Cursor should implement the backlog in this order:
1) Step 0: Auth baseline
2) Step 1: Service Bus wiring
3) Step 2: Wonde ingestion skeleton
4) Step 3: Comms SMS skeleton (webhooks in ApiGateway)
5) Step 4: Workflow unexplained absences + cases
6) Step 5: Minimal staff endpoints

At the end of each step, Cursor must:
- build solution
- run policy validation + tests
- provide “how to run locally” instructions
- commit with a scoped commit message
