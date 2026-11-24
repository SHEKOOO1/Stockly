# Implementation Status — Stockly

Status date: 2026-11-24

## Current state

**Phase 1 — in progress, not complete.** The persistence foundation,
tenant-isolation enforcement, API cross-cutting components, a minimal startable
host, real concurrency/migration test tiers, local Docker/Nginx reference
scaffolding, outbox readiness/dispatcher foundation, a CI workflow and an early
mocked/static frontend skeleton exist. Authentication, business endpoints,
concrete outbox handlers and the Phase 8 production PWA remain in later phases.

The API serves `/health/live`, `/health/ready` and `/api-docs/openapi.json`.
ISS-08 – ISS-12 are resolved. `/health/ready` verifies SQL Server connectivity,
migration state and the outbox backlog through a read-only platform principal.

## Verification evidence

- `dotnet tool restore`: pass, `dotnet-ef` 10.0.12 and ReportGenerator 5.6.0.
- `dotnet format Stockly.slnx --verify-no-changes --no-restore`: pass.
- `dotnet build Stockly.slnx --no-restore -warnaserror`: pass, 0 warnings, 0 errors.
- `dotnet ef migrations has-pending-model-changes --project src/Stockly.Infrastructure --startup-project src/Stockly.Infrastructure`: pass, no changes.
- Full solution test run with
  `STOCKLY_TEST_SQLSERVER='Server=localhost\SQLEXPRESS;Integrated Security=true;TrustServerCertificate=true'`:
  440 passed, 0 failed, 0 skipped — 322 unit, 51 architecture, 40 integration,
  20 security, 3 concurrency, 4 migration.
- API-project restore and Release publish with the same commands used by the
  Dockerfile: pass locally; container execution remains unclaimed.
- A real API process previously returned 200 from liveness, readiness and OpenAPI.
- Frontend gates: `npm run format:check`, `npm run typecheck`, `npm run lint`,
  `npm test` (5 passed) and `npm run build` all pass from `client/`.
- Production-build browser scan: all 34 routes are RTL with a heading and zero
  placeholder components; context switch, toast, command palette, search empty
  state and modal were verified; no console/page errors were captured.
- Docker/Compose/Nginx and the GitHub Actions workflow have not been executed;
  no container validation is claimed.

## Implemented

| Area | Status |
|---|---|
| Domain model and EF configurations | Complete for the 66-entity schema |
| Migrations | Two migrations, applied to canonical database; conformance tier passes |
| Tenant isolation | Query filters, RLS, `SESSION_CONTEXT`, tenant checks |
| API foundation | Host starts; error, correlation, header, scope and tenant plumbing |
| Health/OpenAPI | Liveness, outbox-aware readiness, sanitised OpenAPI document |
| Tests | All six test projects contain real, passing tests |
| Local infrastructure | Dockerfile, Compose, Nginx config, migration profile, pinned tools |
| CI | Static and DB-backed workflow gates; coverage artifacts collected |
| Frontend skeleton | 34 Arabic RTL routes, shell, local interactions, i18n and isolated demo data in `client/` |

## Partial

| Area | Gap |
|---|---|
| Health readiness | Implemented with approved policy; no business producer populates real events yet |
| Outbox handlers | Dispatcher and state machine exist; no concrete `IOutboxMessageHandler` is registered |
| Audit | Collector exists but no business call site drains it |
| API routes | Health and OpenAPI only; no `/api/v1` business endpoints |
| API contract | `docs/BACKEND_API_CONTRACT_GAPS.md` records all missing DTOs/policies |
| CI | Frontend, nightly, release, dependency/container scans and coverage thresholds are not implemented |
| Deployment | Local reference scaffold only; no validated container or target environment |
| Frontend skeleton | Local preview only; no API/DTOs, auth/RBAC, persistence, service worker or English/LTR locale |

## Not started

- Authentication, sessions and token handling (Phase 2).
- RBAC evaluation, seeding and lockout/rate limiting (Phase 2).
- PIN/device flow (Phase 4).
- Master data, inventory, purchasing, events, food and reports APIs (Phases 3–7).
- Phase 8 production PWA, English/LTR locale and shared terminal UI (the mocked/static skeleton is already implemented).
- Production deployment and Docker validation.

## Open issues

- ~~ISS-08: ADR-0051 was cited by code but was never authored or ratified.~~ Resolved 2026-11-24.
- ~~ISS-12: ADR-0052 remains `Proposed`.~~ Resolved 2026-11-24.
- S1–S12 require Phase 2 authentication and are not reportable now.
- Docker validation is impossible on this workstation and is not claimed.
- The frontend is deliberately mocked/static; data-connected UI remains blocked by absent auth and `/api/v1/**` operations.

No production readiness is claimed.
