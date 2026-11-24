# Implementation Status — Stockly

Status date: 2026-11-23

## Current state

**Phase 1 — in progress, not complete.** The persistence foundation,
tenant-isolation enforcement, API cross-cutting components, a minimal startable
host, real concurrency/migration test tiers, local Docker/Nginx reference
scaffolding and a CI workflow exist. Authentication, business endpoints and the
frontend remain in later phases.

The API serves `/health/live`, `/health/ready` and `/api-docs/openapi.json`.
ISS-09, ISS-10 and ISS-11 are resolved. `/health/ready` verifies SQL Server
connectivity and migration state; it does not yet check the outbox backlog.

## Verification evidence

- `dotnet tool restore`: pass, `dotnet-ef` 10.0.12 and ReportGenerator 5.6.0.
- `dotnet format Stockly.slnx --verify-no-changes --no-restore`: pass.
- `dotnet build Stockly.slnx --no-restore -warnaserror`: pass, 0 warnings, 0 errors.
- `dotnet ef migrations has-pending-model-changes --project src/Stockly.Infrastructure --startup-project src/Stockly.Infrastructure`: pass, no changes.
- Full solution test run with
  `STOCKLY_TEST_SQLSERVER='Server=localhost\SQLEXPRESS;Integrated Security=true;TrustServerCertificate=true'`:
  433 passed, 0 failed, 0 skipped — 322 unit, 51 architecture, 33 integration,
  20 security, 3 concurrency, 4 migration.
- A real API process previously returned 200 from liveness, readiness and OpenAPI.
- Docker/Compose/Nginx and the GitHub Actions workflow have not been executed;
  no container validation is claimed.

## Implemented

| Area | Status |
|---|---|
| Domain model and EF configurations | Complete for the 66-entity schema |
| Migrations | Two migrations, applied to canonical database; conformance tier passes |
| Tenant isolation | Query filters, RLS, `SESSION_CONTEXT`, tenant checks |
| API foundation | Host starts; error, correlation, header, scope and tenant plumbing |
| Health/OpenAPI | Liveness, basic readiness, sanitised OpenAPI document |
| Tests | All six test projects contain real, passing tests |
| Local infrastructure | Dockerfile, Compose, Nginx config, migration profile, pinned tools |
| CI | Static and DB-backed workflow gates; coverage artifacts collected |

## Partial

| Area | Gap |
|---|---|
| Health readiness | No outbox-backlog check |
| Audit | Collector exists but no business call site drains it |
| API routes | Health and OpenAPI only; no `/api/v1` business endpoints |
| API contract | `docs/BACKEND_API_CONTRACT_GAPS.md` records all missing DTOs/policies |
| CI | Frontend, nightly, release, dependency/container scans and coverage thresholds are not implemented |
| Deployment | Local reference scaffold only; no validated container or target environment |

## Not started

- Authentication, sessions and token handling (Phase 2).
- RBAC evaluation, seeding and lockout/rate limiting (Phase 2).
- PIN/device flow (Phase 4).
- Master data, inventory, purchasing, events, food and reports APIs (Phases 3–7).
- Frontend/PWA and shared terminal UI (Phase 8).
- Production deployment and Docker validation.

## Open issues

- ISS-08: ADR-0051 is cited by code but was never authored or ratified.
- ISS-12: ADR-0052 remains `Proposed`; ratification is an owner action.
- Outbox readiness access model and thresholds are undecided.
- S1–S12 require Phase 2 authentication and are not reportable now.
- Docker validation is impossible on this workstation and is not claimed.

No production readiness is claimed.
