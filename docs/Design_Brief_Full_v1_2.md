# Attendance Improvement SaaS (Ireland-first) — Design Brief v1
*Date:* 2026-01-07  
*Audience:* internal stakeholders, pilot schools, ETB/trust leadership, procurement, delivery team

## 1. Executive summary
This document specifies a first-pass (v1) design brief for an outcomes-led SaaS platform to reduce student absence through:
- **Fast, reliable attendance signals** (RFID → SIS → Wonde → platform)
- **Consistent, tiered interventions** aligned to **Ireland’s Anseo three-tier model**
- **Two-way, multi-channel guardian communications** with full audit trail
- **Case management and evidence packs** that support compliance and real-world follow-through

Phase 1 targets **Ireland** (post-primary first) while keeping the product open for UK policy requirements and rollout.

## 2. Goals and non-goals
### Goals (outcomes-led)
- Reduce persistent/chronic absence via early detection and timely intervention.
- Reduce staff admin burden: fewer spreadsheets, fewer manual follow-ups.
- Provide consistent, policy-aligned workflows with evidence capture by default.
- Provide multi-buyer value (schools, ETBs/trusts, government-level reporting views) without changing the core product.

### Non-goals (Phase 1)
- Student-facing messaging or apps.
- Period-by-period attendance (Phase 1 uses AM/PM sessions).
- SIS write-back (read-only from SIS; action and logging in platform).
- Automating high-stakes safeguarding decisions (human-first escalation only).

## 3. Scope and assumptions (Phase 1)
- **Attendance granularity:** AM/PM sessions.
- **Audience:** staff and guardians only.
- **Integration:** Wonde is the SIS extraction layer.
- **SIS platforms (Ireland):** VSware, Tyro, Compass Education, Aladdin.
- **Messaging providers:** Twilio (SMS/WhatsApp/Voice), Azure Communication Services, Zoho (email/communications).
- **Data retention:** records retained while student is enrolled (plus configurable post-leaver retention if required later).
- **Tenancy model:** shared database with strict tenant isolation.

## 4. Policy alignment (Ireland-first, UK-ready)
### Ireland anchors
- **Anseo (TUSLA/TESS):** evidence-based actionable attendance tool using a **three-tier model**; schools identify patterns at student/class/whole-school level and respond within tiers.
- **DE national attendance campaign:** includes rollout of the Anseo framework.

### UK readiness (not the initial sales focus)
- Align the workflow engine to be compatible with **DfE “Working together to improve school attendance”** statutory guidance; UK-specific thresholds and notices are implemented via policy packs (no code forks).

## 5. Personas (Phase 1)
- **Attendance Admin / Office staff:** daily lists, contact, logging, escalation.
- **Year Head / Pastoral lead:** targeted interventions, meetings, plans.
- **Principal / Deputy:** oversight, reporting, accountability.
- **Designated Liaison Person (DLP):** safeguarding alerts and response.
- **Guardian:** receives messages, replies with reason/actions.

## 6. Core product concepts
### Entities (minimum)
- Student, Guardian, Enrollment, Class/Year, School (tenant)
- AttendanceSession (AM/PM), AttendanceMark, AbsenceReason
- Case, Tier, Intervention, Task, Checklist, EvidencePack
- Communication (message/call/email), DeliveryStatus, Consent, OptOut
- AuditEvent (immutable)

### Key principles
- **Data → Insight → Action → Follow-up → Evidence** is the product spine.
- **Policy packs** drive thresholds, tiers, templates, channels, and autonomy per school.
- **Human-in-the-loop for sensitive pathways** (especially safeguarding).

## 7. Workflows (catalog)
- W1 Daily absence loop (same-day action)
- W2 Tiered escalation (Anseo-style 3 tiers)
- W3 Two-way communications loop (message → reply → resolution)
- W4 Safeguarding escalation (human-first, deterministic rules + alerts)

(See Mermaid diagrams in `Workflows_and_C4_v1.mmd`.)

## 8. Functional requirements (FR)
### FR-1 Ingestion and reconciliation
- Pull daily (or more frequent) AM/PM attendance via Wonde.
- Reconcile RFID-derived attendance vs SIS marks (detect anomalies; flag for review).
- Provide ingestion health dashboard (last sync, errors, missing entities, mismatch rate).

### FR-2 Daily action list
- “Who needs contact today?” list with filters (year, class, risk band, open cases).
- Configurable cut-off times per school with sensible defaults.

### FR-3 Case management and tiering
- Automatic case creation on threshold breaches.
- 3-tier structure (configurable) with required actions and review windows.
- Evidence pack builder for escalations (attendance history, comms, interventions, outcomes).

### FR-4 Messaging and communications
- Multi-channel: SMS + Email in MVP; WhatsApp and Voice optional/enableable per school.
- Delivery tracking, retries, and channel fallback (configurable).
- Templates with variables and tone constraints.
- Two-way messaging capture; staff can override/annotate classifications.

### FR-5 Reason taxonomy
- Default Ireland-first taxonomy including **TUSLA categories** (configurable per school).
- UK-ready taxonomy (policy pack swap).

### FR-6 Reporting and dashboards
- School dashboard: attendance trend, persistent absence trend, open cases by tier, comms effectiveness.
- ETB/trust dashboard: roll-ups, benchmarking, cohort comparisons (RBAC).
- Exports and audit-friendly logs; support annual attendance reporting needs where applicable.

### FR-7 Security and access control
- Azure AD B2C identity.
- RBAC: teacher/year head/admin/principal/DLP roles.
- Least privilege and tenant isolation; audit trails for every view/change/action.

## 9. Non-functional requirements (NFR)
- Availability: target 99.9%+ for core API and comms pathways.
- Observability: structured logs, metrics, traces; alerting on ingestion/comms failures.
- Performance: daily list loads quickly (seconds), messaging triggers within minutes.
- Privacy: GDPR-first; data minimisation; consent and opt-outs per channel.
- Accessibility: WCAG-aligned for portals where applicable.

## 10. Architecture (summary)
- Event-driven ingestion and processing.
- Operational store (Azure SQL) + analytics lakehouse (ADLS/Fabric/Synapse).
- Workflow/case engine as a service; comms orchestration as a service.
- AI assist services integrated via strict tool-based actions and guardrails.

(See C4 and Structurizr files for detail.)

## 11. AI (supporting role, not the driver)
AI is used only where it measurably reduces admin or improves timeliness/quality of interventions:
- Drafting messages within approved templates and tone rules.
- Summarising case timelines for staff.
- Prioritising daily action lists (“who to contact first”).
- Classifying inbound guardian replies into structured reasons (editable by staff).

Autonomy is configurable per school:
- **A0 Advisory:** drafts/recommendations only.
- **A1 Auto-message:** can send messages within policy constraints.
- **A2 Auto-escalate:** can open/advance cases and schedule tasks within policy constraints (safeguarding always A0).

## 12. Integration design (Phase 1)
### Wonde
- Pull cadence: daily + optional intra-day refresh (configurable).
- Reconciliation: identify mismatches and missing guardians/contacts.
- Fail-safe: if Wonde sync fails, notify admins and pause automated messaging.

### Messaging
- SMS/WhatsApp/Voice: Twilio (primary); Azure Communication Services (optional per school/region).
- Email: Zoho (or alternative) with tracked delivery.
- Abstraction layer so providers can be swapped without changing business logic.

## 13. Decision log (current)
### Decided
- Ireland-first, post-primary-first; AM/PM attendance only in MVP.
- No SIS write-back.
- Staff + guardians only.
- Shared DB multi-tenancy with strict tenant isolation.
- Record retention while student is enrolled.

### Configurable with defaults (policy pack)
- Cut-off times, thresholds, tier rules, comms channel enablement, templates.
- Reason taxonomy (Ireland-first defaults; UK packs later).
- Autonomy level A0/A1/A2 per school and per workflow.

### TBD / requires stakeholder decision
- Safeguarding keyword lists + escalation recipients and rules (must be human-owned).
- Consent and opt-out handling per channel (GDPR + provider constraints).
- Pilot cohort definition and KPI targets (set before live pilot).

## 14. MVP definition (outcomes-led)
### MVP must deliver
- Daily actionable absence list with cut-offs.
- Same-day guardian contact (SMS + Email), two-way replies, reason capture.
- 3-tier case management aligned to Anseo principles (configurable).
- Tasks, review windows, and evidence capture.
- Dashboards: school + ETB/trust roll-ups.
- Audit trails + RBAC + tenant isolation.

### MVP success measures (to set in pilot)
- % same-day contacts for unexplained absences.
- Guardian reply rate within 24 hours.
- Time-to-first-intervention for Tier 2 candidates.
- Reduction in persistent absence vs baseline cohort.
- Staff time saved on attendance follow-up.

## 15. Sources (policy and reference)
- TUSLA/TESS National School Attendance Campaign (Anseo): https://www.tusla.ie/tess/national-school-attendance-campaign/
- DE campaign page: https://www.gov.ie/en/department-of-education/campaigns/school-attendance/
- DE press release re: Anseo rollout: https://www.gov.ie/en/department-of-education/press-releases/minister-mcentee-launches-national-campaign-on-school-attendance/
- TUSLA three-tier model tool (PDF): https://www.tusla.ie/uploads/content/Three-tiered_Model_to_Promote_School_Attendance.pdf
- TUSLA absence categories continuation sheet (PDF): https://www.tusla.ie/uploads/content/EW_Continuation_Sheet_English.pdf
- DfE attendance guidance (PDF): https://assets.publishing.service.gov.uk/media/66bf300da44f1c4c23e5bd1b/Working_together_to_improve_school_attendance_-_August_2024.pdf


## 16. Safeguarding defaults (safe baseline; configurable per school)
Safeguarding is handled with a **human-first, conservative default**. The platform may *flag* concerns, but it does not make safeguarding determinations.

### 16.1 Principles
- **Always-human review:** any safeguarding alert requires review by configured roles (e.g., DLP/principal/year head).
- **Conservative triggers:** prefer false-negatives over false-positives that could overwhelm staff; never “auto-escalate” to external agencies.
- **Deterministic rules first:** alerts are driven by explicit rules and patterns; AI is used only to *summarise* and *highlight* what matched.
- **Restricted access:** safeguarding alerts and related messages are stored in a restricted case type with stricter RBAC.
- **Full audit trail:** who saw what, when, and what actions were taken.

### 16.2 Default triggers (pattern-based)
These triggers create a **Safeguarding Alert** record and notify configured recipients:
1. **Prolonged non-contact:** repeated absence events plus repeated failed contact attempts over a configurable window.
   - Example default: *no guardian response after 3 contact attempts across 3 days AND student absent ≥ 3 sessions in last 5 school days*.
2. **Sudden change:** sharp attendance deterioration relative to baseline.
   - Example default: *attendance drops by ≥ 50% week-over-week* (configurable).
3. **Consecutive absence threshold:** consecutive AM/PM absences exceed a threshold.
   - Example default: *≥ 4 consecutive sessions absent* (configurable).
4. **High-risk combination:** patterns strongly associated with welfare concerns (configurable).
   - Example: *consecutive absences + known prior Tier 3 case + no contact*.

### 16.3 Default “conservative keyword” triggers (messages/replies)
Keywords should be treated as **indicators** only. Any match produces an alert for human review (no autonomous action).
Suggested conservative list (schools can add/remove):
- **Immediate harm/abuse indicators:** “abuse”, “beating”, “hurt me”, “unsafe”, “violence”, “threat”, “rape”
- **Neglect/guardianship indicators:** “left alone”, “no food”, “homeless”, “kicked out”
- **Self-harm indicators:** “self harm”, “cutting”, “suicide”, “kill myself”
- **Coercion/exploitation indicators:** “forced”, “trafficking”, “grooming”

Implementation note: use **exact/phrase matching** and allow optional word-boundaries; avoid broad terms that create noise (e.g., don’t trigger on “hurt” alone).

### 16.4 Recipient routing (per school)
Each school configures:
- Primary: **DLP**
- Secondary: principal/deputy
- Optional: year head / pastoral lead
- Optional: attendance officer

### 16.5 Workflow when triggered
1. Create restricted safeguarding alert record (linked to student/case).
2. Notify recipients via in-app + email (and SMS if the school chooses).
3. Present: what rule matched, evidence links (attendance pattern + message snippet), and recommended next steps checklist.
4. Human records action taken and closes/updates alert.

### 16.6 AI’s role (limited)
- Summarise the timeline and highlight which rule matched.
- Never decide outcomes, never contact external agencies, never message guardians differently without human approval.


### 16.7 Safeguarding playbook checklist (per school; enforced)
Each school can configure a **Safeguarding Playbook** consisting of **severity-based checklists**. When an alert is created, the system attaches the default checklist (based on severity) and enforces completion of required items before the alert can be closed.

**Default checklist behaviour (recommended):**
- Attach checklist automatically (e.g., HIGH vs MEDIUM).
- Require acknowledgement + evidence review + action notes + follow-up date/time.
- Allow partial saves while in progress.
- Store checklist progress in the alert timeline and include it in audits/evidence packs.

**Why this matters:** it standardises response, reduces missed steps, and produces clean evidence without extra admin.

