# Implementation Audit — Stockly

Audit date: 2026-11-23; frontend addendum: 2026-11-24

## 1. Scope and authority

This audit compares the approved design in `docs/PROJECT_BLUEPRINT.md`, `docs/DEVELOPMENT_STATUS.md`, `docs/DECISIONS.md`, `docs/ARCHITECTURE.md`, `docs/SECURITY_ARCHITECTURE.md`, `docs/ROLES_PERMISSIONS.md`, `docs/API_CONVENTIONS.md` and related design documents with the actual repository.

The implemented foundation is **Phase 1 — Foundation + Architecture + Database + Security Core**, not a finished product. The blueprint v1.1 is ratified, but Phase 1 is still open and later phases cannot be represented as complete:

- Phase 2: authentication, sessions, RBAC evaluation, rate limiting, lockout.
- Phase 3: master data and core inventory engine.
- Phase 4: device enrolment and terminal PIN flow.
- Phase 5: batches, expiry, FEFO, transfers, stocktake, barcode.
- Phase 6: events and food.
- Phase 7: reports, notifications, SaaS, search, customization.
- Phase 8: production frontend/PWA, shared terminal UI, hardening (the early mocked/static skeleton is not the production PWA).

There is no approved document named “Level 1”; this report maps the requested initial skeleton to Phase 1 and records later-phase functionality as missing/deferred rather than pretending it is implemented.

## 2. Verified baseline and final validation

Commands run from `H:\Stockly`:

```powershell
dotnet format Stockly.slnx --verify-no-changes
dotnet build Stockly.slnx --no-restore
$env:STOCKLY_TEST_SQLSERVER='Server=localhost\SQLEXPRESS;Integrated Security=true;TrustServerCertificate=true'
dotnet test Stockly.slnx --no-build --no-restore --logger 'console;verbosity=minimal'
```

Baseline result before this implementation slice: 420 passing tests (322 unit, 49 architecture, 29 integration, 20 security); build clean; API host failed at startup.

Final result on 2026-11-23:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)

Passed!  - Failed:     0, Passed:    51, Skipped:     0, Total:    51 - Stockly.ArchitectureTests.dll (net10.0)
Passed!  - Failed:     0, Passed:   322, Skipped:     0, Total:   322 - Stockly.UnitTests.dll (net10.0)
Passed!  - Failed:     0, Passed:    33, Skipped:     0, Total:    33 - Stockly.IntegrationTests.dll (net10.0)
Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3 - Stockly.ConcurrencyTests.dll (net10.0)
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4 - Stockly.MigrationTests.dll (net10.0)
Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20 - Stockly.SecurityTests.dll (net10.0)
```

Total: **433 passed, 0 failed, 0 skipped**. `dotnet tool restore` and
`dotnet ef migrations has-pending-model-changes` also passed.

A real process was also started with `dotnet run --project src/Stockly.Api --no-build --no-restore --no-launch-profile` against the canonical database:

```text
live=200
ready=200
openapi=200
```

`Stockly.MigrationTests` and `Stockly.ConcurrencyTests` are populated and pass
against real SQL Server. ISS-10 is resolved; a green solution-level run now
includes concurrency and migration behaviour.

## 3. Implemented foundation

### 3.1 Domain and persistence — IMPLEMENTED

- 66 entities across 12 schemas: `platform`, `identity`, `master`, `inventory`, `purchasing`, `events`, `food`, `notifications`, `files`, `extensibility`, `audit`, `ops`.
- `StocklyDbContext` applies schema mapping, composite tenant alternate keys, restricted delete behaviour, tenant root foreign keys, tenant query filters and soft-delete hiding.
- EF configurations exist for every module.
- Two migrations exist:
  - `20261001174239_InitialSchema`
  - `20261001184551_TenantIsolation`
- Migration `InitialSchema` creates 66 tables, 177 foreign keys, 96 `CHECK` constraints and 261 indexes.
- `TenantIsolation` adds SQL Server RLS, `SESSION_CONTEXT`, per-table tenant predicate functions, security policies and tenant ownership checks.
- Tenant injection is scoped correctly through `TenantScopeConnectionInterceptor` and a tenant-aware `IModelCacheKeyFactory`.

### 3.2 API host — PARTIALLY IMPLEMENTED

- `src/Stockly.Api/Program.cs` registers controllers, OpenAPI, the controlled ProblemDetails writer, exception handler, HTTP context accessor, `ITenantContext`, scoped `StocklyDbContext`, audit collector, scope handler and forwarded-IP resolver.
- The startup defect ISS-09 is fixed. `UseExceptionHandler()` now has a fallback delegate that writes through `ProblemDetailsWriter` and refuses to write to aborted or already-started responses.
- `GET /health/live` returns liveness.
- `GET /health/ready` verifies SQL Server connectivity and that all EF migrations are applied. The documented outbox-backlog portion is **not implemented**.
- `GET /api-docs/openapi.json` is mapped with a fixed title/version and no server list. Swagger UI is not added.
- `ProblemDetailsWriter`, `GlobalExceptionHandler`, `CorrelationIdMiddleware`, `SecurityHeadersMiddleware`, `ForwardedIpResolver` and `ScopeRequirementHandler` exist.
- `HttpTenantContext` reads only authenticated `scp`, `tid` and `mid` claims; tenant scope cannot be supplied by a header, route or body.
- There are no `/api/v1` business controllers, authentication handlers, authorization policies, rate limiting or localization. Phase-boundary tests forbid adding JWT authentication in this phase.

### 3.3 Tests — IMPLEMENTED FOR CURRENT PHASE BOUNDARY

- 440 passing tests exist: 322 unit, 51 architecture, 40 integration, 20 security, 3 concurrency, 4 migration.
- All DB-backed tests run against the canonical local `Stockly` database with `STOCKLY_TEST_SQLSERVER` set.
- API host tests verify startup, liveness, readiness, OpenAPI sanitisation, correlation ids, security headers and anonymous access to health endpoints.
- Architecture tests assert the exception-handler fallback, forbidden early-phase features, layering and committed-secret scanning.
- Concurrency tests assert stale-rowversion rejection, single-winner unique-key races and rollback; migration tests assert migration state, schema shape and tenant-isolation artifacts. ISS-10 is resolved.

### 3.4 Frontend/PWA — MOCKED STATIC SKELETON IMPLEMENTED

- `client/` contains a React + TypeScript + Vite application with 34 built Arabic RTL routes, a three-context shell, local interactions, i18n, isolated demo data, a PWA manifest and 5 passing smoke tests.
- There is no service worker, offline write, API client, DTO contract, auth/RBAC evaluation, persistence or shared-terminal flow. All mutations are local preview state.
- Frontend production/PWA delivery remains Phase 8. The skeleton is an authorised early preview under `PROJECT_BLUEPRINT.md` §25.2 and must not be represented as a data-connected client; see `docs/INITIAL_FRONTEND_IMPLEMENTATION.md`.

### 3.5 Deployment/CI — LOCAL REFERENCE SCAFFOLD EXISTS

- `Dockerfile`, `.dockerignore`, `docker-compose.yml`, `infra/nginx/nginx.conf`, `.config/dotnet-tools.json` and `.github/workflows/ci.yml` exist.
- The scaffold keeps SQL Server and the API internal, injects secrets from the local environment, uses a non-root read-only API container and applies migrations as a separate manual profile.
- It encodes no licence, hosting target, domain/certificate source or rate-limit decision.
- Docker is unavailable on this workstation, so no image build, Compose startup, Nginx route or container scan has been claimed. The CI workflow has not been executed by GitHub in this pass.

## 4. Module-by-module status

| Area/module | Status | Evidence and gap |
|---|---|---|
| Domain model (66 entities) | IMPLEMENTED | Entity classes exist under `src/Stockly.Domain/Modules` and build clean. |
| Database schema and migrations | IMPLEMENTED | 2 migrations, 12 schemas, 66 tables; local DB is at head. |
| Tenant isolation / RLS | IMPLEMENTED | Query filters, composite keys, checks, RLS and `SESSION_CONTEXT` interception exist and are security-tested. |
| Cross-cutting API middleware | IMPLEMENTED | Correlation, headers, controlled ProblemDetails and exception handler operate in a startable host. |
| API host endpoints / OpenAPI | PARTIALLY IMPLEMENTED | Liveness, outbox-aware readiness and sanitised OpenAPI are mapped; no `/api/v1` business routes and no business producer populates real outbox events. |
| Platform/SaaS | BACKEND ONLY | `Plan`, `Subscription`, `TenantEntitlement`, `SubscriptionUsage`, `AppUser` tables exist; no platform API or UI. |
| Identity and session model | BACKEND ONLY | Auth/session/PIN tables exist; no login, refresh, PIN verification or session middleware. Phase 2/4. |
| RBAC model | BACKEND ONLY | 105 permission codes, roles and warehouse assignments are designed in docs; schema exists; no seeding or evaluator implementation. Phase 2. Architecture tests forbid adding them now. |
| Organization/warehouses | BACKEND ONLY | Tenant, warehouse, membership and assignment schema exists; no tenant/warehouse admin API or UI. |
| Master data | BACKEND ONLY | Category, unit, product, barcode and setting schema exists; no CRUD API or UI. Phase 3. |
| Inventory core | BACKEND ONLY | Balance, lot, document, transaction and count schema exists; no receiving, issuing, transfer or stocktake logic. Phases 3 and 5. |
| Purchasing | BACKEND ONLY | Supplier, PO, goods receipt and approval schema exists; no workflow implementation. Phase 5+. |
| Events | BACKEND ONLY | Event/requirement/consumption/attendance schema exists; no API or UI. Phase 6. |
| Food/recipes | BACKEND ONLY | Meal type, recipe, items and cost snapshots exist; no API or UI. Phase 6. |
| Reports/notifications | BACKEND ONLY | Tables exist; no API, reporting engine, notification delivery or UI. Phase 7. |
| Audit | PARTIALLY IMPLEMENTED | `AuditLog` table, mapper and collector exist; no call site drains the collector into the business transaction. |
| Frontend/PWA/shared terminal | MOCKED STATIC SKELETON | 34 local RTL routes exist in `client/`; production PWA, auth, persistence and shared terminal remain Phase 8. |
| Docker/Nginx/CI | REFERENCE SCAFFOLD | Local files and workflow exist; no Docker/CI execution or production target decision. Phase 8 hardening remains. |
| ADR-0051 documentation | RATIFIED 2026-11-24 | ADR authored and `Accepted`; §6.1 of `DATABASE_DESIGN.md` and §2.1 of `SECURITY_ARCHITECTURE.md` added. ISS-08 closed. |
| Migration/concurrency coverage | IMPLEMENTED | 4 migration and 3 concurrency tests pass against real SQL Server; ISS-10 resolved. |
| Backend API contract implementation | DOCUMENTED GAP | `docs/BACKEND_API_CONTRACT_GAPS.md` records the absent `/api/v1` surface, DTOs and route conflicts. |

## 5. Security assessment

No new credential, token, PIN, secret, connection string or seed identity was found in source or test output. Baseline security tests pass.

The implemented security posture is strong at the persistence and claim-resolution layers but incomplete at the HTTP boundary:

1. Tenant scope is not accepted from client input.
2. Cross-tenant reads/writes are blocked by EF filters and DB RLS/checks.
3. Service-owned connection string resolution fails fast and does not echo the value.
4. The exception handler writes a controlled RFC 9457 body and cannot leak an exception through it.
5. Security headers and correlation ids are emitted by the host; no secret is present in the sanitised OpenAPI document.
6. There is no authentication handler, so `HttpContext.User` is never authenticated by the application; authenticated claims are theoretical until Phase 2.
7. There is no authorization policy attached to any route and no permission evaluator.
8. There is no rate limiting or lockout implementation.
9. There is no audit write path from use cases.
10. Health readiness now inspects the outbox backlog through a read-only platform principal and fails closed; the dispatcher foundation exists but no concrete handler is registered.
11. Local Docker/Nginx configuration keeps SQL Server and the API internal and
    injects secrets, but it has not been exercised; no container-level security
    claim is made.

No security guarantee should be weakened to make a skeleton pass. Authentication, RBAC evaluation, PIN flow, lockout and rate limiting require phase-authorized implementation and resolution of TBD-01, TBD-03, TBD-04, TBD-05 and TBD-11 before product behaviour can be finalized.

## 6. Implemented scope for this slice

Only Phase 1-safe work was implemented:

1. Fixed API startup without adding forbidden Phase 2 components.
2. Added health endpoints `/health/live` and `/health/ready`.
3. Added the documented OpenAPI document endpoint without Swagger UI or vulnerable transitive packages.
4. Added endpoint and source-level regression tests for host startup, health, OpenAPI, ProblemDetails handling and security headers.
5. Populated the concurrency and migration tiers with real DB-backed assertions and pinned local EF/coverage tools.
6. Added local Docker/Nginx reference scaffolding, a manual migration profile and a CI workflow without choosing undecided licence, hosting, TLS source or rate-limit values.
7. Documented the missing backend API contracts from the ratified design rather than inventing endpoints.
8. Kept authentication, RBAC seeding/evaluation, PIN auth, business CRUD and frontend explicitly deferred until their approved phases and owner TBD decisions are available.

## 7. Explicit non-goals for this audit

- ADR-0051 and ADR-0052 were not ratified **in the audited 2026-11-23 slice**; both were ratified on 2026-11-24 (ISS-08 and ISS-12 closed).
- No unresolved `TBD` was answered.
- No permission grants, roles, tenants, users, warehouses or demo stock were seeded.
- No client-supplied tenant, price, quantity, cost or approval state was added.
- No offline writes, self-registration, AI assistant, location/bin stock scope, FX, in-transit transfers or stock reservations were added.
- No Docker validation was claimed.

## 8. Conclusion

The repository contains a substantial persistence/security foundation, a startable API host with health/OpenAPI endpoints, complete current-phase test tiers and local reference deployment/CI scaffolding, but it is not an operational SaaS product. The largest functional gaps are authentication/RBAC evaluation, all business endpoints, the production frontend/PWA and validated deployment. Those gaps are deliberate phase boundaries or owner decisions, not reasons to silently invent unapproved behaviour.
