# Anseo Connect UI Design Document v0.1 (Cursor Build Guide)

This document is written to be **actionable for Cursor**. It defines the **page inventory, navigation, roles, UI layout**, and the **API + integration expectations** needed to build the staff web UI for Anseo Connect v0.1.

> **Channel requirement (v0.1):** **SMS + Email are required from day one.**  
> SMS provider: **Sendmode**  
> Email provider: **Twilio SendGrid**

---

## 1) Goals and non-goals

### Goals (outcomes-led)
- Reduce unexplained absences via **same-day contact** and structured follow-up.
- Provide **work queues** for attendance staff, year heads, and safeguarding leads.
- Implement **Tier 2 attendance plan** workflows (checklists + tasks).
- Implement **Safeguarding alert** workflows with **always-human review**.
- Ensure all actions are **auditable** (case timeline + message timeline).

### Non-goals (v0.1)
- Advanced analytics dashboards (keep reports minimal).
- Student/guardian portals (phase 2+).
- Multi-SIS beyond Wonde (phase 1 uses Wonde integration).
- WhatsApp/Voice channels (present in consent model but not required for v0.1 delivery).

---

## 2) Users, roles, and access control

### Roles (v0.1)
- **Tenant Admin** (platform-level)
- **School Admin**
- **Attendance Officer**
- **Year Head**
- **DLP / Safeguarding Lead**
- **Principal / Deputy**

### Authorization approach
- Use Microsoft Entra for SSO when available.
- Support non-SSO staff accounts (local login) for schools without SSO.

**Role mapping**
- Entra users: roles via **Entra groups** → mapped to app roles.
- Local users: roles stored in app DB (Identity roles).

### Safeguarding sensitivity
- Safeguarding cases and queues must be **role gated**.
- Safeguarding actions must record:
  - who acknowledged/reviewed
  - when
  - what decision/action was taken

---

## 3) Authentication UX: Do we need school selection on sign-in?

### Short answer
**Usually no.** You only need a school selector if a staff user can access multiple schools.

### Recommended approach (v0.1)
1. User signs in (Entra or Local).
2. API returns `accessibleSchoolIds[]` for the user.
3. If exactly **one** school:
   - auto-select it and set as active context.
4. If **multiple** schools:
   - show a **School Switcher** page (or modal) once per session.
5. Persist last selected school per user (local storage or server-side preference).

**Implementation detail**
- Include `TenantId` and `SchoolId` in the user session context:
  - For Entra JWT: derive tenant/school from app mapping table.
  - For local JWT: include tenant/school claims in issued token.

---

## 4) Information architecture and navigation

### Layout (modern SaaS)
- **Left Sidebar** (primary navigation) + badge counts
- **Top Bar**
  - School switcher (if multi-school)
  - Global search (student/case/guardian)
  - Notifications bell
  - User menu (profile/sign out)
- **Main content** area
- Use “List → Detail” pattern with either:
  - full page navigation for details, OR
  - a right-side detail drawer (recommended for queues)

### Primary navigation (sidebar)
1. **Today**
2. **Cases**
3. **Messages**
4. **Safeguarding** (badged, role-gated)
5. **Students**
6. **Reports** (minimal)
7. **Settings** (role-gated)
8. **Admin** (tenant-level only)

---

## 5) Page inventory (v0.1)

### 5.1 Auth
#### Page: Sign In
- Buttons:
  - “Sign in with Microsoft” (Entra)
  - “Sign in with email and password” (local)
- Error states:
  - invalid credentials
  - not authorized
  - disabled account

#### Page: School Switcher (conditional)
- Show only if user can access >1 school.
- Select school → sets active context.

---

### 5.2 Today (operations)
#### Page: Today Dashboard
Purpose: “What must we do today to reduce absences?”

Sections:
1) **Unexplained Absences (Today)**
- list with filters (Year group, “Not contacted”, “Messaged”, “Replied”)
- actions:
  - Send message (SMS/Email)
  - Open case

2) **Tasks Due Today**
- follow-up tasks (Tier 2, call backs, meetings)

3) **Message Failures / Missing Contact**
- guardians missing mobile/email
- provider delivery failures

4) **Safeguarding Alerts**
- only for safeguarding roles
- quick link to queue

Bulk actions:
- select multiple students → “Send SMS”, “Send Email” (policy/consent checked)

---

### 5.3 Cases
#### Page: Cases List
Filters:
- Status: Open/Closed
- Type: Attendance / Safeguarding
- Tier: 1/2/3
- Year group
- Assigned role
- Overdue tasks

Columns:
- Student
- CaseType + Tier
- Status
- Last activity
- Next action due
- Badges: safeguarding/high risk

#### Page: Case Detail
Tabs:
1) **Overview**
- student summary
- case status/tier
- “why this case exists” (policy explanation)
- guardian contacts + consent states

2) **Timeline**
- case events feed: detection, messages, replies, staff notes, tier changes

3) **Checklist**
- checklist items from policy pack (Tier 2 plan / safeguarding)
- required items clearly marked
- enforce gating on tier transitions

4) **Messages**
- threaded view of outbound + inbound
- show channel, delivery state, timestamps

Actions:
- Send message (SMS/Email)
- Add note
- Change tier (role-gated)
- Close case (role-gated)

---

### 5.4 Messages
#### Page: Messages List
Filters:
- Channel: SMS / Email
- Status: Queued / Sent / Delivered / Failed
- MessageType
- Date range
- “Failed only” toggle

Message detail includes:
- rendered body
- template + tokens used
- provider message id(s)
- delivery timeline
- linked student/case

#### Modal: Compose Message
Inputs:
- Channel (SMS/Email)
- Template selector
- Preview rendered text/body
- Choose guardians (default primary guardian)

Consent decision panel (must show):
- Allowed? yes/no
- Why (policy explanation)
- What to do if blocked (e.g., opt-in required, or opted-out)

---

### 5.5 Safeguarding
#### Page: Safeguarding Queue
Role-gated.

Columns:
- Severity (HIGH/MED/LOW)
- Student
- Trigger summary (short)
- Created at
- Acknowledged by/time

Actions:
- Acknowledge (required)
- Assign to role/person
- Open safeguarding case detail

#### Page: Safeguarding Case Detail
Same pattern as Case Detail, but:
- checklist is mandatory
- restricted visibility
- always-human review controls

---

### 5.6 Students
#### Page: Student Directory/Search
Search by:
- name
- year group
- external student id

List shows:
- basic info
- risk badge
- last 5-day attendance mini-summary

#### Page: Student Profile
Tabs:
- Attendance snapshot
- Guardians + consent
- Cases
- Messages

Actions:
- Create/open attendance case
- Add staff note

---

### 5.7 Reports (minimal v0.1)
- same-day contact rate
- response rate (SMS + Email)
- opt-out rate
- Tier 2: opened/closed counts

---

### 5.8 Settings (school-level)
Role-gated (School Admin).

1) School Settings
- timezone
- cutoff times (AM/PM messaging cutoffs; defaults from policy)
- channel order defaults (SMS then Email by default; configurable)

2) Policy Pack Assignment
- view current pack + version
- change version (with warnings + audit trail)

3) Roles & Recipients
- assign staff users to roles
- safeguarding recipient mapping per severity

4) Integrations
- Wonde status + last sync
- Sendmode status (masked config)
- SendGrid status (masked config)
- Webhook health indicator

---

### 5.9 Admin (tenant-level)
Tenant Admin only:
- create tenant/schools
- staff provisioning
- Entra group mapping
- audit log viewer
- feature flags

---

## 6) UI components (reusable)
- DataTable with filters + saved views
- Detail drawer/page shell
- Timeline component (CaseEvents)
- Checklist component (policy-driven, required vs optional)
- Consent badge component (UNKNOWN/OPTED_OUT/OPTED_IN)
- Message status chip (QUEUED/SENT/DELIVERED/FAILED)
- Severity badge (LOW/MED/HIGH)
- Policy “Explain decision” component (why allowed/blocked)

---

## 7) API endpoints required by UI (high-level)

### Auth
- `POST /auth/local/login`
- `GET /auth/me` (returns user + accessible schools + roles)
- `POST /auth/select-school` (if you keep school context server-side)

### Today
- `GET /today/unexplained-absences`
- `GET /today/tasks`
- `GET /today/safeguarding-alerts`

### Cases
- `GET /cases`
- `GET /cases/{id}`
- `GET /cases/{id}/timeline`
- `POST /cases/{id}/note`
- `POST /cases/{id}/tier`
- `POST /cases/{id}/close`

### Checklist
- `GET /cases/{id}/checklist`
- `POST /cases/{id}/checklist/items/{itemId}/complete`

### Messages
- `GET /messages`
- `POST /messages/send` (creates SendMessageRequestedV1)
- `GET /messages/{id}`

### Students
- `GET /students`
- `GET /students/{id}`

### Settings/Admin
- `GET/PUT /settings/school`
- `GET/PUT /settings/policy`
- `GET/PUT /settings/roles`
- `GET /integrations/status`
- `POST /integrations/wonde/sync`

---

## 8) Messaging channels (day one)

### Channel behavior requirements
- Both channels must support:
  - consent gating (policy + consent state)
  - audit trail (why allowed/blocked)
  - provider status updates
  - link messages to case timeline

### Sendmode SMS
- Implement sender in Comms service.
- Webhooks hosted in ApiGateway:
  - delivery status updates → publish `MessageDeliveryUpdatedV1`
  - inbound replies → publish `GuardianReplyReceivedV1`

### Twilio SendGrid Email
- Implement sender in Comms service.
- Delivery events via SendGrid Event Webhook (ApiGateway):
  - map to `MessageDeliveryUpdatedV1`
- Inbound email replies are optional for v0.1 (many schools won’t reply by email).
  - If implemented, treat as `GuardianReplyReceivedV1`.

---

## 9) Cursor build instructions (planning + sequencing)

### Project order (must follow)
1) Auth baseline (Entra + local)
2) Messaging provider contracts + DB tables for messages/consent
3) Implement Sendmode SMS + SendGrid Email sending
4) Implement ApiGateway webhooks for both providers
5) Implement UI pages in this order:
   - Today dashboard (unexplained absences + send message)
   - Case list + Case detail (timeline + messages + checklist)
   - Safeguarding queue + detail
   - Messages list + message detail
   - Students search + profile
   - Settings (cutoffs + policy + roles + integrations)

### UI tech recommendation (Cursor should implement)
- **Web UI**: Blazor (DevExpress if you want speed), OR React + ASP.NET Core API.
  - If already leaning Blazor: use DevExpress components for tables and forms.
- **Mobile** (later): MAUI or React Native (out of scope v0.1).

### Required UX enhancements for Cursor to include
- Every queue page supports:
  - saved filters
  - quick actions
  - keyboard navigation basics
- Every “Send message” action shows:
  - consent decision + explanation
  - preview rendered content
- Every safeguarding item requires:
  - acknowledge + audit event

### Quality gates
Cursor must ensure:
- Build succeeds
- Policy pack validation/tests pass
- Minimal UI smoke flow works:
  - sign in → today → open case → send sms/email → see message status

---

## 10) Acceptance criteria for UI v0.1
- Staff can sign in with Entra or Local login.
- If user has only one school, no school selection is shown.
- Today page shows unexplained absences and allows sending SMS/Email.
- Case detail shows timeline + messages + checklist.
- Messages page shows delivery status; failed messages are visible and actionable.
- Safeguarding queue is role-gated and supports acknowledge + checklist.
- Settings page allows configuring cutoff times + policy assignment + escalation roles.
- No secrets committed; configuration via environment variables.

---

## 11) Deliverables
- Implement Web UI pages listed above.
- Implement API endpoints listed above.
- Ensure messaging channels (Sendmode SMS + SendGrid Email) work end-to-end.
- Provide a short runbook in `docs/RUNBOOK_LOCAL.md`.
