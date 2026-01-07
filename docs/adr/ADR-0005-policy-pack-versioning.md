# ADR-0005: Policy packs are versioned, immutable, and testable

## Status
Accepted

## Context
Schools need configurable workflows (cut-offs, thresholds, recipients). Changes must be safe and auditable.

## Decision
- Policy packs are versioned using semver (x.y.z)
- Once released, a version is immutable; changes require a new version
- Packs must pass:
  - JSON schema validation
  - policy tests embedded in the pack (tests[])
- Tenant references a specific policy pack version

## Consequences
- Requires schemas + PolicyPackTool CLI + CI checks
- Requires audit when tenant policy version changes

## Links
- policy-packs/schema/
- tools/PolicyPackTool/
