# ADR-0006: Message contract naming + versioning

## Status
Accepted

## Context
Service Bus messages are shared contracts between services. Changing them casually breaks integrations.

## Decision
- Each message type is an immutable contract.
- Versioning is done via type name suffix:
  - e.g. `SendMessageRequestedV1`, `SendMessageRequestedV2`
- The envelope fields are stable and always include:
  - TenantId, SchoolId, CorrelationId, OccurredAtUtc, MessageType, Version
- Do not edit a released message type; introduce a new version type instead.

## Consequences
- Consumers can support multiple versions during upgrades.
- CI/build can safely pin to contract versions.
