# ADR-0004: Messaging provider abstraction (Twilio/Zoho/ACS)

## Status
Accepted

## Context
Schools will choose providers. We need a consistent interface and audit.

## Decision
Implement Comms service with provider adapters:
- ISmsProvider (Twilio, ACS)
- IEmailProvider (Zoho)
- optional IWhatsAppProvider/IVoiceProvider
All sends go through:
- consent gate
- templating
- delivery tracking
- audit logging

## Consequences
- Provider-specific webhooks mapped to a common delivery/opt-out event model
- Provider secrets stored in Key Vault; per-tenant config in DB/policy

## Links
- docs/architecture/c4/C4_Container_*.mmd
