# ADR-0002: Consent baseline and opt-out capture

## Status
Accepted

## Context
Schools need same-day attendance messaging. We must avoid sending on channels where the guardian has opted out.

## Decision
Default rules:
- SMS and Email allowed for service attendance/admin messages unless opted-out
- WhatsApp and Voice require explicit opt-in if enabled by a school
Platform capabilities (phase 1):
- platform can record OPTED_OUT events (guardian reply STOP, provider webhook, staff action)
- platform does not implement full opt-in collection UX in phase 1

## Consequences
- Must implement provider webhooks (Twilio/ACS) for opt-out signals
- Must implement staff UI action to mark opted out + audit trail
- Channel fallback order is configurable per school via policy pack

## Links
- policy-packs/ie/IE-ANSEO-DEFAULT/1.1.0/barriers-consent.json
- docs/architecture/workflows/Comms_Consent_and_Tier2_Barriers_*.mmd
