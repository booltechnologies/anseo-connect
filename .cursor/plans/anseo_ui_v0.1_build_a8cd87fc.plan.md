---
name: anseo_ui_v0.1_build
overview: Implement DevExpress Blazor web UI per v0.1 design doc, covering auth, dashboards, cases, safeguarding, messaging, students, and settings/admin flows.
todos:
  - id: scaffold-ui
    content: Add DevExpress Blazor host, shared RCL, typed client
    status: completed
  - id: auth-shell
    content: Build layout/nav, auth wiring, school switcher
    status: completed
  - id: shared-components
    content: Create grids/timeline/checklist/badges/message modal
    status: completed
  - id: pages-core
    content: Implement Today, Cases list/detail, Safeguarding
    status: completed
  - id: pages-support
    content: Implement Messages, Students, Settings/Admin, runbook
    status: completed
---

# Build AnseoConnect UI v0.1

- Set up DevExpress Blazor skeleton: add RCL `src/UI/AnseoConnect.UI.Shared` for shared components and host `src/Web/AnseoConnect.Web` for routing/theme; add typed client `src/UI/AnseoConnect.Client` using `AnseoConnect.Contracts` DTOs and wire auth (Entra + local) + school context via ApiGateway auth endpoints; update `AnseoConnect.sln` to include projects.
- Layout & navigation: implement `AppLayout` with sidebar (Today, Cases, Messages, Safeguarding, Students, Reports, Settings, Admin), topbar (school switcher, search, notifications, user menu), and detail drawer pattern using DevExpress panels; configure router with role-gated routes.
- Shared components: DevExpress DataGrid wrapper with filters/saved views, `CaseTimeline`, checklist with gating, consent/status chips (message status, severity, consent badge), policy decision panel, message compose modal (SMS/Email with consent preview), reusable detail drawer.
- Pages (priority order): Auth (Sign-In, School Switcher); Today dashboard (absences/tasks/failures/safeguarding alerts + bulk send); Cases list & detail (overview/timeline/checklist/messages tabs, tier/close/note actions); Safeguarding queue/detail with acknowledge/assign; Messages list/detail with delivery timeline and provider IDs + compose modal; Students directory/profile (tabs for attendance, guardians/consent, cases, messages); Settings (school cutoffs, policy pack assignment, roles/recipients, integrations status); Admin (tenant provisioning, audit log, feature flags); minimal Reports for contact/response/opt-out/Tier2 counts.
- Docs: add `docs/RUNBOOK_LOCAL.md` covering DevExpress setup, env vars (Sendmode/SendGrid), and steps to run `AnseoConnect.Web` with ApiGateway.