# Initial Skeleton — Stockly

Status: 2026-11-23

This document describes the initial skeleton that actually exists in the
repository. It is deliberately limited to the ratified Phase 1 boundary. It is
not a claim that the SaaS product, authentication, warehouse workflows or PWA are
implemented.

## Startable foundation

- Solution: `Stockly.slnx` targeting .NET 10.
- Projects: `Stockly.Domain`, `Stockly.Application`, `Stockly.Infrastructure`,
  `Stockly.Api`, and six test projects.
- API host: `src/Stockly.Api/Program.cs` starts against the canonical `Stockly`
  database and serves:
  - `GET /health/live`
  - `GET /health/ready`
  - `GET /api-docs/openapi.json`
- Cross-cutting host components:
  - controlled RFC 9457 `ProblemDetailsWriter`;
  - `GlobalExceptionHandler` with a safe fallback;
  - correlation-id middleware;
  - the nine documented security headers;
  - forwarded-IP resolution;
  - ADR-0034 scope handler;
  - HTTP claim-based tenant context.
- Database: SQL Server only; two applied migrations
  (`InitialSchema`, `TenantIsolation`), 66 entities across 12 schemas, RLS,
  `SESSION_CONTEXT` enforcement and tenant-scoped `CHECK` constraints.

## What the skeleton does not contain

- No authentication handlers, JWT validation, login, refresh, PIN login or
  lockout. These are Phase 2/4.
- No RBAC seeding, role derivation or permission evaluation. Entity classes and
  permission codes are schema/design only. Phase-boundary tests fail if these are
  implemented early.
- No tenant, user, warehouse, product, stock or demo-data seeding.
- No `/api/v1` business controllers: receiving, issuing, transfers, stocktake,
  master data, events, food, reports and notifications are not implemented.
- No audit collector drain inside a business transaction.
- No production frontend/PWA, service worker, shared terminal UI or offline support. An early mocked/static skeleton now exists under `client/`; see `docs/INITIAL_FRONTEND_IMPLEMENTATION.md`.
- No validated container deployment or production target. Local reference
  `Dockerfile`, `docker-compose.yml`, `infra/nginx` and CI workflow exist but
  have not been run.

## Tenant isolation

Tenant scope is read only from the authenticated principal's `scp`, `tid` and
`mid` claims. No header, route value or body field can nominate a tenant.
`TenantScopeConnectionInterceptor` writes the scope to SQL Server
`SESSION_CONTEXT` on every physical connection. Query filters, composite keys
and RLS predicates deny cross-tenant reads and writes.

Because authentication is not implemented until Phase 2, requests in this
skeleton have no authenticated claims. No business data route is exposed,
so this does not create an unauthenticated data-access surface.

## Verification

On 2026-11-24:

- `dotnet tool restore` passed with pinned `dotnet-ef` 10.0.12 and ReportGenerator 5.6.0.
- `dotnet format Stockly.slnx --verify-no-changes --no-restore` passed.
- `dotnet build Stockly.slnx --no-restore -warnaserror` passed with 0 warnings and 0 errors.
- EF model-drift check passed with `src/Stockly.Infrastructure` as startup project.
- 440 tests passed: 322 unit, 51 architecture, 40 integration, 20 security,
  3 concurrency, 4 migration.
- A real API process previously returned 200 from liveness, readiness and OpenAPI.
- Docker/Compose/Nginx and the GitHub Actions workflow have not been executed;
  no container validation is claimed.

See `docs/IMPLEMENTATION_AUDIT.md` for the module-by-module audit and
`docs/NEXT_DEVELOPMENT_STEPS.md` for the phase-aligned handoff.
