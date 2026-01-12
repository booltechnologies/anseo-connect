# Anseo Connect – Local UI Runbook

## Prerequisites
- .NET SDK 10.0 (templates were generated with the 10.0 SDK).
- DevExpress NuGet feed access (restore pulls `DevExpress.Blazor` 25.2.3).
- API base URL configured via `AnseoApi:BaseAddress` in `src/Web/AnseoConnect.Web/appsettings*.json` (defaults to `https://localhost:5001/`).
- Required env vars for the API (examples):  
  - `ANSEO_JWT_SECRET` for ApiGateway JWT issuance.  
  - Sendmode / SendGrid keys provided to Comms service if exercising live messaging.

## One-time setup
```bash
dotnet restore AnseoConnect.sln
```

## Run backend services (API + Comms)
```bash
# launch ApiGateway (uses Kestrel default ports)
dotnet run --project src/Services/AnseoConnect.ApiGateway

# in a second terminal, optional Comms sender
dotnet run --project src/Services/AnseoConnect.Comms
```

## Run the Blazor UI
```bash
dotnet run --project src/Web/AnseoConnect.Web
```

Then browse to the URL printed by Kestrel (usually `https://localhost:5003` or `http://localhost:5002`).

## Authentication
- Navigate to `/signin` for local username/password login (posts to `/auth/local/login` on ApiGateway).
- School switcher is available at `/school-switcher` if the user has multiple schools.
- Auth/session state is managed in-memory; tokens are attached to outbound API calls via `BearerTokenHandler`.

## Navigation overview
- Today dashboard: `/today`
- Cases list/detail: `/cases`, `/cases/{id}`
- Safeguarding queue: `/safeguarding`
- Messages list/detail: `/messages`, `/messages/{id}`
- Students list/profile: `/students`, `/students/{id}`
- Settings (school-level): `/settings`
- Admin (tenant-level shell): `/admin`
- Reports (minimal v0.1): `/reports`

## Notes
- UI uses DevExpress Blazor components (25.2.3). No additional static asset steps are needed; `_Host` links the DevExpress CSS.
- The UI clients fall back to stub data when the API is unavailable so that pages render during early development. When the API is reachable, live data is used.
- Update `appsettings.Development.json` if the ApiGateway base URL changes.
