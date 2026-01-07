# ADR-0001: Shared DB multi-tenancy with tenant_id enforcement

## Status
Accepted

## Context
We will host many schools (tenants). Phase 1 requires shared DB (Azure SQL) for cost and operational simplicity.
Risk: cross-tenant data leakage if enforcement is inconsistent.

## Decision
Use a shared Azure SQL database with:
- tenant_id column on every tenant-owned table
- tenant isolation enforced in code:
  - per-request TenantContext resolved from auth token
  - global query filters in EF Core for tenant-owned aggregates
  - write-time enforcement: tenant_id set server-side only
- tenant_id included in Service Bus message metadata

## Consequences
- Requires consistent entity base class (TenantEntity) and DbContext patterns
- Adds complexity for admin/ETB cross-tenant reporting (explicit “reporting views” only)
- Migration scripts must preserve tenant_id and indexes

## Alternatives considered
- Separate DB per tenant (rejected due to cost/ops complexity for phase 1)
- Separate schema per tenant (rejected due to complexity and tooling friction)

## Links
- docs/architecture/c4/C4_Container_*.mmd
