# Cursor Bootstrap Instructions — Anseo Connect (.NET 10 / Azure SQL / Service Bus / App Service)

## Goal
Bootstrap a mono-repo for Anseo Connect with:
1) repo skeleton + solution structure
2) policy pack schemas (JSON Schema) + validation + policy tests
3) a PolicyPackTool CLI that validates and runs tests in CI

Do NOT build product features yet. Focus only on repo structure + policy governance tooling.

## Tech constraints
- .NET 10 (preview OK if required by SDK)
- Azure SQL target (EF Core)
- Azure Service Bus integration will come later (only define contracts folder now)
- Windows-friendly commands and paths
- Keep code clean, minimal, and testable

---

## Task A — Create folder structure + baseline files
Create these folders (if missing):
- /docs/architecture/c4
- /docs/architecture/workflows
- /docs/adr
- /docs/runbooks
- /docs/cursor
- /policy-packs/schema
- /policy-packs/ie/IE-ANSEO-DEFAULT
- /policy-packs/uk/UK-DEFAULT
- /src/services
- /src/shared
- /infra/bicep/modules
- /infra/env
- /tools/PolicyPackTool

Copy existing artifacts from /mnt/data (if present in workspace) into:
- docs/Design_Brief_Full_v1_2.md
- docs/architecture/c4/*.mmd
- docs/architecture/workflows/*.mmd
- policy-packs/ie/IE-ANSEO-DEFAULT/<version>/ (place JSON policy packs)

Add placeholder README files where helpful.

Acceptance:
- repo builds in VS on Windows (even if services are empty)
- docs and policy packs are in sensible paths

---

## Task B — Create .NET solution + projects
Create solution: /src/AnseoConnect.sln

Create projects:
- /tools/PolicyPackTool/PolicyPackTool.csproj (console app)
- /src/shared/AnseoConnect.PolicyRuntime/AnseoConnect.PolicyRuntime.csproj (class library)
- /src/shared/AnseoConnect.Contracts/AnseoConnect.Contracts.csproj (class library)
- /src/shared/AnseoConnect.Data/AnseoConnect.Data.csproj (class library)

Wire references:
- PolicyPackTool references PolicyRuntime
- PolicyRuntime can reference Contracts (if needed)

Acceptance:
- `dotnet build src/AnseoConnect.sln` succeeds

---

## Task C — Define JSON Schemas for policy packs
Create JSON Schema files in /policy-packs/schema:
- policy-pack.schema.json (root)
- safeguarding.schema.json
- consent.schema.json
- barriers.schema.json
- reason-taxonomy.schema.json
- attendance-plan.schema.json

These must validate existing packs (located in /policy-packs/ie/...).
Use JSON Schema draft 2020-12.

Rules:
- policyPackId: string
- version: semver pattern `^\d+\.\d+\.\d+$`
- scope: { type: COUNTRY|TENANT, code: string }
- effectiveFrom: ISO date string
- modules optional, but if present must match module schemas
- tests: array of objects with fields { name, input, expected }

Acceptance:
- all current policy packs validate against the schema with no warnings

---

## Task D — Implement PolicyPackTool CLI
Commands:
1) `policypack validate <path>`
   - scans for *.json in directory (recursively)
   - validates against schema(s)
   - exit code non-zero on failure
2) `policypack test <path>`
   - loads policy packs
   - runs deterministic tests defined in `tests[]`:
     - for now: simply check that `expected` keys exist after applying simple evaluation rules:
       - consent tests: evaluate allow/deny based on consentModel rules
       - checklist tests: allowPromoteTier false if checklist not complete
       - safeguarding tests: createAlert true when pattern trigger met
   - output a summary table
   - exit code non-zero if any tests fail

Keep the test runner small and deterministic. No network calls.

Acceptance:
- Running validate and test against /policy-packs succeeds on clean repo
- Failing schema or test causes non-zero exit

---

## Task E — Add CI workflow (GitHub Actions)
Create .github/workflows/ci.yml that runs on PR:
- setup dotnet (10)
- dotnet restore/build/test
- run:
  - `dotnet run --project tools/PolicyPackTool -- validate policy-packs`
  - `dotnet run --project tools/PolicyPackTool -- test policy-packs`

Acceptance:
- CI passes when policy packs valid
- CI fails on schema break or failing policy test

---

## Output format
After changes:
- list created files
- list commands to run locally
- note any assumptions made
