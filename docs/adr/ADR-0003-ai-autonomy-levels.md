# ADR-0003: AI autonomy levels (A0/A1/A2)

## Status
Accepted

## Context
AI can help drafting, classification, summarisation, and prioritisation, but attendance improvement is the goal and staff remain accountable.

## Decision
Adopt A0/A1/A2 autonomy model:
- A0: AI suggests only; human approves every send/change
- A1: AI acts within strict templates/rules; human review available and default for high-risk actions
- A2: AI can execute low-risk actions automatically (e.g., send templated SMS) with audit and rollback

Guardrails:
- consent gate always enforced
- safeguarding triggers always route to human review
- full audit trail for AI-influenced decisions

## Consequences
- Every AI action must record “AI involvement” metadata
- Schools can configure autonomy level per module/tenant

## Links
- docs/Design_Brief_Full_v1_2.md
