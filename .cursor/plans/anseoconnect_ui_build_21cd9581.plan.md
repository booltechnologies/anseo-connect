---
name: AnseoConnect UI Build
overview: Build the complete staff web UI for AnseoConnect v0.1 using Blazor Server with DevExpress components, following the UI Design Document. This includes authentication pages, dashboard, case management, messaging, safeguarding queues, student directory, reports, and settings.
todos: []
---

# AnseoConnect UI Build Plan

## Summary

Build the Blazor Web App UI for AnseoConnect v0.1 with DevExpress components. The backend API is complete - this plan focuses on the UI layer: API client, shared components (RCL), and the Blazor Web App host.

---

## Phase 1: Project Structure Setup

### New Projects to Create

Following the solution structure defined in [`.cursor/rules/000-project.mdc`](.cursor/rules/000-project.mdc):

- **`AnseoConnect.Client`** - Typed HTTP API client with JWT token handling
  - Location: `src/Shared/AnseoConnect.Client/`
  - Contains: `AnseoConnectApiClient.cs`, `AuthenticationHandler.cs`, service registration

- **`AnseoConnect.UI.Shared`** - Razor Class Library (RCL) with reusable components
  - Location: `src/Shared/AnseoConnect.UI.Shared/`
  - Contains: All pages, layouts, and components shared between Web and future Mobile

- **`AnseoConnect.Web`** - Blazor Web App host
  - Location: `src/Web/AnseoConnect.Web/`
  - Contains: Program.cs, wwwroot, DevExpress theme registration, routing host

### DevExpress Package References

```xml
<PackageReference Include="DevExpress.Blazor" Version="25.2.*" />
<PackageReference Include="DevExpress.Blazor.Themes" Version="25.2.*" />
```

---

## Phase 2: API Client (`AnseoConnect.Client`)

Create typed client methods for all API endpoints from [`AuthController`](src/Services/AnseoConnect.ApiGateway/Controllers/AuthController.cs), [`CasesController`](src/Services/AnseoConnect.ApiGateway/Controllers/CasesController.cs), and [`AbsencesController`](src/Services/AnseoConnect.ApiGateway/Controllers/AbsencesController.cs):

```csharp
// Key methods needed:
Task<LoginResponse> LoginAsync(string username, string password);
Task<WhoAmIResponse> GetCurrentUserAsync();
Task<PagedResult<CaseDto>> GetCasesAsync(string status, int skip, int take);
Task<CaseDto> GetCaseAsync(Guid caseId);
Task<TodayAbsencesResponse> GetTodayAbsencesAsync();
Task<ConsentStatusDto> GetConsentStatusAsync(Guid guardianId, string channel);
Task SendMessageAsync(SendMessageRequest request);
```

---

## Phase 3: App Shell and Layout (`AnseoConnect.UI.Shared`)

### Main Layout with DevExpress DxDrawer

```
+--------------------------------------------------+
| Top Bar (DxToolbar)                              |
|  [Menu] Logo  | Search | Notifications | User    |
+------+-------------------------------------------+
|      |                                           |
| Side | Main Content Area                         |
| bar  | (@Body)                                   |
| Nav  |                                           |
|      |                                           |
+------+-------------------------------------------+
```

**Components:**

- `MainLayout.razor` - DxDrawer with DxMenu sidebar, DxToolbar top bar
- `NavMenu.razor` - Navigation items with badge counts (Today, Cases, Messages, Safeguarding, Students, Reports, Settings, Admin)
- `TopBar.razor` - School switcher, global search (DxSearchBox), notifications bell, user menu

---

## Phase 4: Authentication Pages

### 4.1 Sign In Page (`/login`)

- **Components:** DxFormLayout, DxTextBox, DxButton
- **Features:**
  - "Sign in with Microsoft" button (Entra ID - placeholder for v0.1)
  - "Sign in with email and password" form (local JWT)
  - Error states: invalid credentials, not authorized, disabled account
- **Endpoint:** `POST /auth/local/login`

### 4.2 School Switcher Page (`/select-school`)

- Only shown if user has access to multiple schools
- DxListBox or card selection UI
- Stores selection in local storage / session

---

## Phase 5: Core Pages

### 5.1 Today Dashboard (`/`, `/today`)

**Purpose:** "What must we do today to reduce absences?"

**Sections:**

1. **Unexplained Absences (Today)** - DxGrid with filters (Year group, "Not contacted", "Messaged", "Replied")

   - Endpoint: `GET /api/absences/today`
   - Actions: Send message (SMS/Email), Open case
   - Bulk actions with DxGrid selection

2. **Tasks Due Today** - DxGrid of follow-up tasks

3. **Message Failures / Missing Contact** - Alert cards

4. **Safeguarding Alerts** - Quick link card (role-gated)

### 5.2 Cases List (`/cases`)

- DxGrid with server-side paging
- Filters: Status, Type, Tier, Year group, Assigned role, Overdue tasks
- Columns: Student, CaseType+Tier, Status, Last activity, Next action due, Badges
- Endpoint: `GET /api/cases`

### 5.3 Case Detail (`/cases/{caseId}`)

- DxTabs with 4 tabs:

  1. **Overview** - Student summary, case status/tier, policy explanation, guardian contacts
  2. **Timeline** - Timeline component showing case events (DxListBox with custom template)
  3. **Checklist** - Policy pack checklist items (DxCheckbox list)
  4. **Messages** - Threaded message view with delivery status

- **Actions:** Send message, Add note, Change tier, Close case
- Endpoint: `GET /api/cases/{caseId}`

### 5.4 Messages List (`/messages`)

- DxGrid with filters: Channel, Status, MessageType, Date range, "Failed only"
- Message detail drawer/modal
- **Compose Message Modal:**
  - Channel selector (SMS/Email)
  - Template selector (DxComboBox)
  - Preview rendered content
  - Consent decision panel (allowed/blocked with explanation)

### 5.5 Safeguarding Queue (`/safeguarding`) - Role-gated

- DxGrid columns: Severity badge, Student, Trigger summary, Created at, Acknowledged by/time
- Actions: Acknowledge (required), Assign, Open detail
- Visual urgency styling based on severity

### 5.6 Safeguarding Case Detail (`/safeguarding/{alertId}`)

- Same pattern as Case Detail but with mandatory checklist
- Restricted visibility controls
- Always-human review controls with audit

### 5.7 Students Directory (`/students`)

- DxGrid with search: name, year group, external student ID
- Columns: Basic info, Risk