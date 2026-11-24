# STOCKLY — DEVELOPMENT STATUS

> **Read this file first.** It is the current, authoritative state of Stockly.
> If it disagrees with the code, investigate before changing anything.

| Field | Value |
|---|---|
| Last updated | 2026-11-24 |
| Updated by | Initial frontend implementation and final verification |
| Product | Stockly (ستوكلي) |
| Blueprint version | **1.1.0 — `STOCKLY BLUEPRINT v1.1 — RATIFIED / READY FOR PHASE 1`** |
| Released code version | None — Phase 1 implementation is in progress |

---

## 1. Current Phase

**Phase 1 — IN PROGRESS.** The persistence foundation (solution scaffold, domain
model, `DbContext`, EF configurations, two migrations), the tenant-isolation
enforcement (RLS + TenantScope + `SESSION_CONTEXT`), the API cross-cutting
plumbing (RFC 9457 writer, correlation id, security headers, forwarded-IP
resolver, fail-fast connection resolution), the transactional-outbox readiness
and dispatcher foundation, local Docker/Nginx reference scaffolding and a CI
workflow are implemented and build clean. An early mocked/static frontend
skeleton exists under `client/`, but no business endpoints or outbox handlers
exist and container validation has not been performed. Phase 1 is **not complete**.

The blueprint was **ratified in full by the product owner on 2026-09-30**:
ADR-0001 – ADR-0050 are all `Accepted`, and SEC-KNW-01 – SEC-KNW-09 are
explicitly acknowledged as accepted V1 limitations. ADR-0051 and ADR-0052 were
ratified on 2026-11-24, so all 52 written ADRs are now `Accepted`.

**Blueprint: RATIFIED.** Design work is complete and the authority gate is
closed.

**Implementation: PARTIAL (see §14–§17).** The solution, domain model, EF model,
both migrations, the isolation enforcement, all six DB-backed/unit/architecture
test tiers, local Docker/Nginx reference scaffolding and a CI workflow exist and
build clean. There is an **early mocked/static frontend skeleton**, but **no
business API and no Docker validation**.

**The API starts.** The exception-handler configuration defect recorded as ISS-09
was fixed on 2026-11-23 by supplying a fallback request delegate without adding
`AddProblemDetails` or bypassing the existing `ProblemDetailsWriter`. A real
`dotnet run --no-launch-profile` process served `/health/live`,
`/health/ready` and `/api-docs/openapi.json` with `200 OK` against the canonical
database. `UseAuthorization()` still has no authentication handler in Phase 1;
this is a phase boundary, not a new startup failure. No route is protected yet.

**A local SQL Server instance IS available** — `localhost\SQLEXPRESS` (Windows
authentication), which closes the environment gap that previously blocked all
DB-backed verification. DB-backed tests have therefore been **executed against
real SQL Server and pass** (§4). **Docker remains unavailable**, so
container-based validation still cannot be claimed.

**Phase 1: READY TO BEGIN.** **Zero Phase 1 decision blockers.** On 2026-09-30
the owner decided the five items that genuinely gated the Phase 1 foundations
(TBD-19, TBD-21, TBD-22, TBD-24, TBD-25).

**Ratification did not resolve any TBD.** The 16 open questions are open exactly
as before — **phase-gated or deferred, none of them blocking Phase 1** (§23.1).
Ratification approves the specification that governs the work; it does not decide
every question that will arise while doing it.

**Security: blueprint-level security architecture ratified.** The 9 registered
limitations were accepted as known V1 limitations, not as remediated findings.

**Two separate questions, two separate answers.** Do not conflate them
(§11.1a):

| Question | Answer |
|---|---|
| **Phase 1 authorisation** — may Phase 1 start? | **READY.** Zero decision blockers; blueprint and all 52 written ADRs ratified |
| **Phase 1 database-dependent verification** — may Phase 1 be called *complete*? | **PARTLY DONE.** SQL Server is available and all DB-backed tiers pass, including real concurrency and migration tests; the API starts and serves health/OpenAPI endpoints; outbox readiness and dispatcher foundations are tested. **Not yet complete**: audit drain call site, route-level scope policy, business API/outbox handlers, Phase 2-gated S1–S12 and Docker validation remain outstanding |

A missing environment is not a missing authorisation. Phase 1 may begin now;
it may not be declared finished while required SQL Server-dependent tests are
unexecuted.

## 2. Phase Status

| Item | Status |
|---|---|
| Blueprint drafted | ✅ Complete — **v1.1.0** |
| Blueprint internally consistent | ✅ Verified (see §5) |
| Five Phase 1 blocking decisions | ✅ **DONE 2026-09-30** (TBD-19, 21, 22, 24, 25 → ADR-0045 – ADR-0049) |
| Blueprint reviewed / ratified by owner | ✅ **DONE 2026-09-30 — RATIFIED** |
| ADRs ratified | ✅ **52 of 52 written ADRs `Accepted`** (ADR-0001 – ADR-0044 promoted 2026-09-30; ADR-0045 – ADR-0050 already `Accepted`; ADR-0051 authored and ADR-0052 ratified 2026-11-24) |
| Security limitations acknowledged | ✅ **DONE 2026-09-30** — SEC-KNW-01 – SEC-KNW-09 accepted as V1 limitations |
| Open owner decisions answered | ⏳ **5 decided, 16 open** — none blocks Phase 1, none resolved by ratification |
| Phase 1 (implementation) | 🟡 **In progress** — persistence foundation, tenant-isolation enforcement, API host, health/OpenAPI endpoints with outbox readiness, outbox dispatcher foundation, local Docker/Nginx reference scaffold and CI workflow done; no authentication/business endpoints, outbox handlers or Docker validation. ISS-08, ISS-09, ISS-10, ISS-11 and ISS-12 resolved |
| Phase 1 database-dependent verification | 🟡 **SQL Server available and all tiers executed** — integration 40/40, security 20/20, concurrency 3/3 and migration 4/4 pass against the canonical database |
| Code written | Domain (66 entities, 12 schemas) + Infrastructure (DbContext, 66 configurations, 2 migrations, RLS/TenantScope enforcement, design-time factory) + Application (error contract, audit mapper/collector, redactor, scope model) + API (RFC 9457 writer, correlation id, security headers, forwarded-IP resolver, scope handler, tenant context, host wiring) + test support (`StocklyDbTestSupport`) |
| Migrations written | **2 — `InitialSchema`, `TenantIsolation`** — both **applied to `localhost\SQLEXPRESS`**; schema verified at head |
| Tests written | **440** — 322 unit, 51 architecture, 40 integration, 20 security, 3 concurrency, 4 migration |
| Tests executed / passing | **440 PASS / 0 fail / 0 skipped** against real SQL Server (2026-11-24). `dotnet format` clean; build 0 warnings; `has-pending-model-changes` clean with local EF tools 10.0.12 |
| Frontend | **Mocked/static skeleton implemented** under `client/` — 34 Arabic RTL routes, local interactions, no API/auth/persistence; Phase 8 PWA not complete (§3b) |
| Frontend skeleton handoff | 🟡 **B — READY WITH DOCUMENTED NON-BLOCKING LIMITATIONS** — mocked/static shell only; data-connected UI blocked by absent auth and `/api/v1/**` endpoints (§3a) |
| Infrastructure | Local reference scaffold exists — `Dockerfile`, `.dockerignore`, `docker-compose.yml`, `infra/nginx`, migration profile and `.github/workflows/ci.yml`; **not Docker-validated** |
| Production readiness | **Not claimed. Nothing is deployed.** |

## 3. Last Completed Milestone

**Phase 1 ADR/outbox closure slice** (2026-11-24): the owner closed ISS-08 and
ISS-12 — ADR-0051 was authored to ratify the already-implemented tenant-isolation
design and ADR-0052 was ratified, bringing the ratified ADR count to 52. The
owner approved the outbox backlog policy (any `DeadLetter` or stale
`Pending`/`Processing` older than 15 minutes degrades readiness; thresholds
configurable; readiness uses a read-only platform principal). The API now gates
`/health/ready` on an `ops.OutboxMessage` snapshot, and a dispatcher/coordinator
foundation supports claim, retry/backoff, dead-letter, stale-lease recovery and
unknown-handler release. Seven DB-backed outbox tests were added. The Dockerfile
restore defect was fixed by restoring/publishing the API project directly.
Verification: clean format, clean `-warnaserror` build, clean EF model-drift
check, API-project restore/publish and 440 passing tests against real SQL Server.
Docker execution remains unavailable and is not claimed.

## 3a. Frontend Skeleton Handoff Review (2026-11-24)

**Outcome: B. READY WITH DOCUMENTED NON-BLOCKING LIMITATIONS.**

The repository is ready to start a **non-production, mocked/static frontend
skeleton**: layout, routing, design system, localization scaffolding and mocked
service adapters. It is **not** ready for a data-connected UI, a live login
flow, real tenant/warehouse/user administration, business CRUD, or client-side
authorization decisions. `PROJECT_BLUEPRINT.md` §25 keeps the complete frontend
in Phase 8; §25.2 expressly permits the frontend to start early against a mocked
or partial API. This handoff does **not** change that phase boundary and does
**not** make Phase 1 complete.

| Requirement reviewed | Evidence | Status |
|---|---|---|
| ISS-08 — tenant-isolation decision | `DECISIONS.md` ADR-0051 is `Accepted`; `DATABASE_DESIGN.md` §6.1 and `SECURITY_ARCHITECTURE.md` §2.1 document RLS, TenantScope `CHECK`s and `SESSION_CONTEXT`; 20 `TenantIsolationTests` pass against the canonical database | **RESOLVED** |
| ISS-12 — canonical database decision | `DECISIONS.md` ADR-0052 is `Accepted`; `STOCKLY_TEST_SQLSERVER` is mandatory and all DB-backed tiers ran against the canonical `Stockly` database | **RESOLVED** |
| Outbox readiness and dispatcher | `Program.cs:70`, `Program.cs:108-153`, `OutboxMonitor.cs:29-62`, `OutboxCoordinator.cs:27-180`, `OutboxDispatcher.cs:57-92`; seven DB-backed tests cover transactional commit/rollback, once-only dispatch, retry/backoff/dead-letter, stale-lease recovery, unknown-handler release, readiness degrade/recovery and fail-closed readiness | **PARTIAL — foundation operational, business integration absent** |
| Outbox event producers / concrete handlers | No production code creates `ops.OutboxMessage` rows or implements `IOutboxMessageHandler`; only the integration tests do so | **MISSING by design until the first business side effect exists** |
| Authentication, sessions and rate limiting | No `AddAuthentication`, login, refresh, lockout or rate limiter; `PhaseBoundaryTests.cs` fails if these are introduced early | **Phase 2 boundary; not a mocked-skeleton blocker, but a live-auth UI blocker** |
| Business API surface | `Program.cs` maps only `/health/live`, `/health/ready` and `/api-docs/{documentName}.json`; `MapControllers()` has no controllers; no `/api/v1/**` routes or operation-level OpenAPI metadata exist | **Design ready; executable contract absent** |
| Docker / Compose / Nginx | `Dockerfile`, `.dockerignore`, `docker-compose.yml`, `infra/nginx` and `.github/workflows/ci.yml` exist; the API-project restore path is fixed and local restore/publish succeeded. No Docker build, Compose startup, Nginx request or container scan was run here; GitHub Actions has not executed the workflow | **Reference scaffold only; validation unavailable locally** |
| Frontend contract readiness | `API_CONVENTIONS.md`, `WORKFLOWS.md`, `ROLES_PERMISSIONS.md` and `BACKEND_API_CONTRACT_GAPS.md` define intended routes and permissions, but there are no DTOs, typed clients or executable business operations. `BACKEND_API_CONTRACT_GAPS.md` §§2–3 still describe `/health/ready` as database/migration-only; that row is stale and must be corrected before that file is treated as the handoff contract source | **Mocked shell ready; data-connected UI blocked** |

Non-blocking limitations for a mocked skeleton:

1. Mock adapters must be clearly labelled and must not freeze unverified JSON
   shapes as decisions; DTOs belong to the owning backend phase.
2. No client may supply or infer identity, tenant, warehouse scope, permission,
   role, price, quantity, cost or approval state. These stay server-side.
3. The UI may show shell states for auth and administration, but it must not
   claim working login, RBAC evaluation or live CRUD.
4. Outbox-backed side effects cannot be exercised yet because no producer or
   concrete handler exists.
5. Docker, Compose, Nginx and hosted CI execution remain unvalidated; no
   production-readiness claim is permitted.

Handoff validation: `dotnet tool restore`, `dotnet format Stockly.slnx
--verify-no-changes --no-restore`, `dotnet build Stockly.slnx --no-restore
-warnaserror`, `dotnet ef migrations has-pending-model-changes --project
src/Stockly.Infrastructure --startup-project src/Stockly.Infrastructure` and
`dotnet test Stockly.slnx --no-build --no-restore` with
`STOCKLY_TEST_SQLSERVER` set all passed on 2026-11-24: **440 passed, 0 failed,
0 skipped** (322 unit, 51 architecture, 40 integration, 20 security, 3
concurrency, 4 migration). No new code, ADR, phase decision or commit was made by
this review.

## 3b. Initial Frontend Implementation (2026-11-24)

The mocked/static frontend authorised by §3a is now implemented in `client/`.
It contains a React 18 + TypeScript + Vite application with 34 page routes
(8 platform, 8 organization, 18 warehouse), an Arabic RTL shell, responsive
navigation, breadcrumbs, command palette, context selectors, notifications,
toasts, reusable tables/forms/dialogs/charts, typed i18n modules and isolated
`origin: 'demo'` data. All 34 routes have built content; no page renders
`PlaceholderPage`.

Implemented interactions are local-only: context switching, notification state,
command-palette navigation, search/filter/sort/pagination, create/edit/delete
previews, validation, confirmation dialogs, tabs, toasts and simulated
loading/empty/error states. There is **no** API call, DTO contract, login,
refresh, token storage, RBAC evaluation, persistence, service worker, offline
write, push subscription or shared-terminal flow. Demo data shapes are
illustrative only and are not frozen backend decisions. The first-target
customer name is not hardcoded in frontend source.

Verification on 2026-11-24: `npm run format:check`, `npm run typecheck`,
`npm run lint`, `npm test` (5 passed) and `npm run build` all passed. A
production-build browser scan of all 34 routes reported `dir="rtl"`, a non-empty
`h1`, `placeholders: 0`, working context switch (`org-1` → `org-2`) with toast,
command palette, product-search empty state and modal, and zero console/page
errors. The backend gates were re-run after the frontend work: tool restore,
clean format, clean `-warnaserror` build, clean EF model-drift check and **440
passed / 0 failed / 0 skipped**. Docker/Compose/Nginx and hosted CI remain
unvalidated. Full detail, including implemented/not-started status by category,
is in `docs/INITIAL_FRONTEND_IMPLEMENTATION.md`. This does **not** make Phase 1
or Phase 8 complete, and no commit was made.

**Before that — Phase 1 verification closure and reference infrastructure** (2026-11-23):
populated `Stockly.ConcurrencyTests` (3 tests) and `Stockly.MigrationTests`
(4 tests), resolved ISS-10; pinned local `dotnet-ef` 10.0.12 and ReportGenerator
5.6.0 tools, verified the model-drift gate and resolved ISS-11; added local
Docker/Nginx/Compose reference scaffolding with a manual migration profile and a
CI workflow; documented missing backend API contracts in
`docs/BACKEND_API_CONTRACT_GAPS.md`. Final verification: clean format, clean
`-warnaserror` build, clean EF model-drift check and 433 passing tests against
real SQL Server. Docker itself was unavailable, so no container build/run was
claimed.

**Before that — API host repair and endpoint foundation of Phase 1** (2026-11-23): fixed the
`UseExceptionHandler()` startup defect (ISS-09), added `/health/live`,
`/health/ready`, the sanitised `/api-docs/openapi.json` document, four API host
integration tests and two source-level exception-handler regression tests.
Verified with `dotnet format`, a clean solution build, 426 passing tests at the
time of that slice and a real `dotnet run` smoke test (`live=200`, `ready=200`,
`openapi=200`). The current total is 433 after the concurrency and migration
tiers were populated. Readiness currently verifies database connectivity and applied
migrations; the documented outbox-backlog check is not implemented. No
authentication, RBAC evaluation, seeding, business CRUD, frontend or deployment
work was added.

**Before that — security spine, audit pipeline and API host wiring of Phase 1** (2026-10-01):
the audit entry mapper and request-scoped collector (`PROJECT_BLUEPRINT.md` §16,
including the refuse-don't-redact rule and the "staged, not saved" transaction
contract); `SessionScopeClaim` as the single `scp` parser; the ADR-0034
`ScopeRequirementHandler`; `ForwardedIpResolver` (`SECURITY_ARCHITECTURE.md` §6);
all nine §12 security headers; `StocklyDbContext` registered in the API host with
a fail-fast connection string; and committed-secret scanning as a build failure.
**264 tests passed at the time of that slice** (229 unit, 35 architecture), 0 fail,
0 skipped. Solution build was clean (0 warnings, 0 errors). Four security defects
were found and fixed in the process — see `CHANGELOG.md`, "Fixed — scope and
header defects".

**Before that — persistence foundation of Phase 1** (2026-10-01): solution
scaffold of 4 src and 6 test projects; 66 domain entities across 12 schemas;
`StocklyDbContext` with tenant query filters; 66 EF Core configurations;
design-time factory; the `InitialSchema` migration; the error contract
(`ApiProblem` / `BusinessErrorCode` / `BusinessException`); the audit secret
redactor; the RFC 9457 writer, correlation-id middleware and global exception
handler; and 168 passing tests (136 unit, 32 architecture) including the
absence tests for all five 2026-09-30 owner decisions.

**Before that — blueprint correction to v1.1** (2026-09-30): recorded the five owner decisions
as ADR-0045 – ADR-0049, created ADR-0050 for the reservation deferral, split the
overloaded TBD-10 so that stock reservations are now the separate open item
TBD-26, removed warehouse locations from the Phase 5 title, restructured the §23
register into decided / phase-gated / deferred, and propagated the consequences
through all eight detailed design documents. No design count changed.

**Before that:** blueprint finalization to v1.0 (2026-09-27), which reconciled
the master specification against every detailed design document and resolved the
`InventoryLot`/`StockBalance` unique-index defect.

## 4. Current Work

Done so far: the persistence foundation and `InitialSchema` migration; the error
contract; the RFC 9457 writer and global exception handler; correlation ids; the
audit secret redactor; the audit entry mapper and collector; the ADR-0034
session/scope model and its authorization handler; `HttpTenantContext`; the
forwarded-IP resolver; the §12 security headers; the committed-secret scan; and
the API host wiring that makes the tenant-filtered data-access layer reachable.

Remaining Phase 1 work, in order:

1. **Call-site integration of the audit writer.** The mapper and collector exist and
   are tested, but no application service drains `IAuditCollector` into its own
   `DbContext` change set yet. That drain-inside-the-transaction call site is what
   actually satisfies §16 rule 6, and it does not exist.
2. **Route-level application of the scope policy.** `ScopeRequirementHandler` is
   registered, but no business endpoint exists to attach
   `ScopeRequirementHandler.ForRequest` to, so ADR-0034 is not yet enforced on any
   business route. Controllers arrive with the first use case.
3. **Concrete outbox handlers and producers.** The readiness gate, coordinator and
   dispatcher exist and are tested; no business event has a registered
   `IOutboxMessageHandler` yet, so messages with unhandled types are released for
   later processing rather than delivered.
4. Final migration-to-design fidelity review (column-level, not counts).
5. Add localization and idempotency infrastructure when their owning phases begin.
6. Validate the local Docker/Nginx scaffold on a workstation where Docker is
   available, and complete the remaining CI gates documented as pending in
   `TESTING_STRATEGY.md` §5. S1–S12 require Phase 2 authentication and are not
   reportable now.

The health, OpenAPI, API-host, concurrency and migration tests are real and
passing; they do not make the unimplemented items above reportable.

## 5. Verified Design Counts (2026-09-30)

These counts are the contract. If any of them changes, the change must be made
in the same commit in **every** document that repeats it, plus a `CHANGELOG.md`
entry. A stale count is a defect, not a cosmetic issue.

| Item | Count | Source of truth |
|---|---|---|
| V1 entities | **66**, across **12 schemas** | `DATABASE_DESIGN.md` §4 |
| Permission codes | **105**, in **13 namespaces** | `ROLES_PERMISSIONS.md` |
| Roles | **11 tenant roles + 1 platform authority** | `ROLES_PERMISSIONS.md` |
| Decisive / derived role grants | 33 decisive + 72 derived by D1–D5 | ADR-0044 |
| Business rules | **147** | `BUSINESS_RULES.md` |
| Business error codes | **27** (26 emitted + reserved `account_locked`) | `BUSINESS_RULES.md` |
| Architecture decision records | **52 ratified `Accepted`** (ADR-0001–ADR-0052) | `DECISIONS.md` |
| Workflows | **19** sections | `WORKFLOWS.md` |
| Gating security test classes | **33** (`S1`–`S33`) | `TESTING_STRATEGY.md` |
| Known security limitations | **9** (`SEC-KNW-01`–`SEC-KNW-09`) | `SECURITY_ARCHITECTURE.md` §15 |
| Implementation phases | **8** (ADR-0040) | `PROJECT_BLUEPRINT.md` §25 |
| Tracked decision items | **26** | `PROJECT_BLUEPRINT.md` §23 |
| Owner decisions **DECIDED** | **5** (TBD-19, 21, 22, 24, 25) | `PROJECT_BLUEPRINT.md` §23.4 |
| Owner decisions **UNRESOLVED** | **16** (1–15, 23, 26) | `PROJECT_BLUEPRINT.md` §23.1 |
| Disposed design decisions | 4 (`TBD-02`, `TBD-17`, `TBD-18`, `TBD-20`) | `PROJECT_BLUEPRINT.md` §23.2 |
| Disposed safe assumptions | 1 (`TBD-16`) | `PROJECT_BLUEPRINT.md` §23.3 |

**The ADR count moved from 44 to 50 on 2026-09-30 and from 50 to 52 on
2026-11-24; nothing else moved.** The five owner decisions, the reservation
deferral, the tenant-isolation ratification and the canonical-database
ratification each required a decision record, which `PROJECT_BLUEPRINT.md` §27
mandates. No entity, permission, role, business rule, error code, workflow or
phase was added, removed or renumbered: 66 / 12 / 105 / 13 / 11+1 / 147 / 27 /
19 / 33 / 9 / 8 are all unchanged. Verified mechanically after the edit.

Entity distribution: `platform` 6, `identity` 16, `ops` 2, `master` 7,
`inventory` 8, `purchasing` 10, `events` 5, `food` 5, `notifications` 3,
`files` 1, `extensibility` 2, `audit` 1 = **66**.

## 6. Documentation Structure

| File | Purpose | Status |
|---|---|---|
| `docs/PROJECT_BLUEPRINT.md` | Master specification (v1.1.0, 28 sections) | ✅ **RATIFIED 2026-09-30** |
| `docs/ARCHITECTURE.md` | Solution structure, layering, module boundaries, runtime/deployment topology | Final draft |
| `docs/SECURITY_ARCHITECTURE.md` | Threat model, trust boundaries, controls, 9 known limitations | ✅ Ratified 2026-09-30 (limitations acknowledged, not remediated) |
| `docs/DATABASE_DESIGN.md` | 66-entity catalogue, column-level design, FK/index/constraint rules, migration policy | Final draft |
| `docs/ROLES_PERMISSIONS.md` | 105-permission catalogue, 13 namespaces, 11+1 roles, matrix, D1–D5 derivation, escalation controls | Final draft |
| `docs/BUSINESS_RULES.md` | 147 rules across all domains, 27 business error codes | Final draft |
| `docs/WORKFLOWS.md` | 19 end-to-end workflow sections | Final draft |
| `docs/API_CONVENTIONS.md` | REST contract, headers, pagination, idempotency, concurrency, error format, route scopes | Final draft |
| `docs/BACKEND_API_CONTRACT_GAPS.md` | Implemented-versus-documented API surface, missing DTOs/policies and route conflicts | Current implementation audit |
| `docs/TESTING_STRATEGY.md` | Test projects, taxonomy, 33 gating security test classes, concurrency/integrity suites, CI gates | Final draft |
| `docs/DEVELOPMENT_STATUS.md` | This file | Current |
| `docs/INITIAL_FRONTEND_IMPLEMENTATION.md` | Initial mocked/static frontend implementation status and verification evidence | Current |
| `docs/DECISIONS.md` | ADR-0001 – ADR-0052 ratified, all `Accepted` | ✅ Ratified 2026-09-30; ADR-0051 and ADR-0052 ratified 2026-11-24 |
| `docs/CHANGELOG.md` | Change history | Current |
| `README.md` | Repository overview, documentation reading order | Current |
| `AGENTS.md` | Binding operating rules for AI agents | Final |
| `.gitignore` | Secret/build exclusion rules | Final |
| `.env.example` | Non-secret configuration template | Final |

## 7. Repository State (verified 2026-11-23)

| Check | Result |
|---|---|
| Directory `H:\Stockly` | Exists |
| Application source code | Domain, Application, Infrastructure and API projects under `src/` |
| Migrations | **2** — `InitialSchema`, `TenantIsolation`; both applied |
| Tests | Six active test projects plus the shared DB fixture library; **440 tests passing** |
| Frontend | Untracked `client/` React + TypeScript + Vite skeleton; 34 routes and 5 frontend smoke tests passing |
| Local infrastructure | `Dockerfile`, `.dockerignore`, `docker-compose.yml`, `infra/nginx`, `.config/dotnet-tools.json`, `.github/workflows/ci.yml` |
| Git repository | **Initialised** — branch `main` |
| Baseline commit | `9121044` "Initial commit" |
| Finalization checkpoint | `949a873` "docs: checkpoint surviving blueprint finalization work" |
| Blueprint finalization commits | `67b26e6` (blueprint v1.0 + status + readme + changelog + DB index fix), `fe2eb7a` (ignore nested repo) |
| Owner-decision correction (v1.1), all Phase 1 code/infrastructure and the 2026-11-24 closure slice | **Uncommitted working-tree changes**. Nothing has been committed because the owner has not asked for a commit |
| Nested repository `H:\Stockly\Stockly` | **Empty repo, no commits. Never tracked. Ignored via `.gitignore` by owner decision (ISS-04 — resolved).** Still on disk; do not delete or commit. **Not touched during this pass.** |
| Unrelated user work destroyed | **None** |

## 8. Technology Detected in the Environment

| Tool | Version | Status |
|---|---|---|
| .NET SDK | 10.0.201 | Available |
| Node.js | v24.14.0 | Available |
| Git | 2.53.0.windows.1 | Available |
| Docker | — | **NOT AVAILABLE** |
| Local SQL Server | `localhost\SQLEXPRESS` | **Available** — Windows/integrated authentication |

**Consequence (honest statement):** integration, security, concurrency, and
migration tests require a real SQL Server. They **are executed on this
workstation** against the canonical `Stockly` database using
`STOCKLY_TEST_SQLSERVER`; no in-memory, SQLite or mocked substitute is used.
Docker is unavailable, so the local Compose/Nginx scaffold and any container
image build remain unvalidated here.

## 9. Technology Direction (decided, not yet implemented)

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core Web API, C#, **.NET 10 (LTS)**, EF Core, SQL Server 2022 |
| API | REST, OpenAPI / Swagger, RFC 9457 ProblemDetails (ADR-0027) |
| Frontend | React + TypeScript + Vite, PWA — mocked/static skeleton implemented; production PWA later |
| Architecture | Modular Monolith, schema-per-module (ADR-0001, ADR-0016) |
| Infrastructure | Docker, Nginx, HTTPS/TLS, CI/CD — **built in Phase 1** (ADR-0040) |
| Test stack | xUnit, FluentAssertions, WebApplicationFactory, Testcontainers (MSSQL) |

Delivery is **8 implementation phases**, not 11 (ADR-0040). The mapping from the
superseded eleven-phase plan is in `PROJECT_BLUEPRINT.md` §25.1.

## 10. Completed Work

- Product vision, name (Stockly / ستوكلي), and scope defined.
- Multi-tenant requirement established: tenant isolation, warehouse
  authorization independent of roles, devices bound to warehouses, sessions
  bound to user + device + scope.
- Modular Monolith decision recorded (ADR-0001).
- Complete entity catalogue defined: 66 entities across 12 schemas,
  column-level for all, including the `ops` schema.
- Permission catalogue (105 codes, 13 namespaces) and 11+1 role model defined,
  with grants made derivable by rule (ADR-0044) rather than hand-maintained.
- 147 business rules and 27 error codes defined.
- 19 operational workflows defined, including platform-operator login and
  device enrollment.
- API conventions defined, including token scopes, `scp`, 409/428 semantics,
  and enrollment-code terminal routes.
- Security architecture, STRIDE-derived threat model, and 9 registered known
  limitations defined.
- Testing strategy with 33 gating security test classes defined.
- Delivery plan consolidated to 8 phases (ADR-0040) with a legacy mapping.
- `AGENTS.md` operating rules established.
- **`inventory.InventoryLot` unique-index defect resolved:** the
  `UX_InventoryLot_NonBatched` filtered index
  `(TenantId, WarehouseId, ProductId) WHERE BatchId IS NULL` was declared on the
  entity but **omitted from the `DATABASE_DESIGN.md` §7 indexing table**. An
  implementer following §7 would have lost the one-row-per-warehouse+product
  guarantee that the guarded `UPDATE` in §8.1 depends on. Both tables now agree
  and both indexes are named (`UX_InventoryLot_NonBatched`,
  `UX_InventoryLot_Batch`, `UX_StockBalance`).

**Implementation (Phase 1, in progress):**

- Solution scaffold: `Stockly.Domain`, `Stockly.Application`,
  `Stockly.Infrastructure`, `Stockly.Api` and six test projects
  (`UnitTests`, `IntegrationTests`, `SecurityTests`, `ConcurrencyTests`,
  `ArchitectureTests`, `MigrationTests`).
- Shared build settings in `Directory.Build.props`; warnings are errors.
- **Domain**: 66 entities across 12 schemas, plus the shared bases that encode the
  tenant contract — `ITenantScoped`, `IGlobalEntity`, `IHasRowVersion`,
  `ISoftDelete`, `IAuditable` and the matching base classes. `TenantScopedEntity`,
  `TenantScopedSoftDeleteEntity`, `TenantRecordEntity`, `AppendOnlyTenantScopedEntity`
  and `SoftDeleteEntity` (for the tenant root).
- **Infrastructure**: `StocklyDbContext` with all 66 `DbSet`s, schema mapping, a
  global tenant query filter and soft-delete filtering; `ITenantContext` /
  `FixedTenantContext`; 66 EF Core configurations; `DesignTimeStocklyDbContextFactory`.
- `InitialSchema` migration generated (66 tables, 185 FKs, 96 CHECKs, 70 unique
  indexes, 34 of them filtered). See §16.
- **Security spine** (`PROJECT_BLUEPRINT.md` §25 Phase 1 deliverable):
  - **Error contract** — `BusinessErrorCode` (all 27 documented codes with their
    wire string, HTTP status and RFC 9457 `type` pinned), `ApiProblem`,
    `BusinessException`. `account_locked` is reserved and *cannot be emitted*: it
    would be an account-enumeration oracle. `500`/`503` carry no `code`, because
    §6.3 defines those statuses and §14 defines no code for them — none was
    invented.
  - **RFC 9457 writer** — one place a problem body is produced, so S22 is a
    structural property rather than a review checklist.
  - **Global exception handler** — a closed mapping. `BusinessException` → its
    catalogue entry; `DbUpdateConcurrencyException` → `412 concurrency_conflict`;
    `DbUpdateException`/`TimeoutException` → `503` naming no dependency;
    everything else → a `500` carrying only a trace id.
  - **Correlation id** — echoes `X-Correlation-Id`, generates one otherwise, and
    **rejects** an inbound value that is over-long or contains anything outside
    `[A-Za-z0-9-_.:]`, because it is written back into a response header and into
    log lines.
  - **Audit secret redactor** — refuses (not redacts) a payload containing a
    secret by name or by shape. Matches whole tokens rather than substrings, so
    `shipping` and `saltiness` are not false positives; also catches JWT-shaped and
    connection-string-shaped values under innocuous names, bounds traversal depth,
    and survives a throwing property getter.
  - **Session/scope model** — `RequestScope` (sealed record, init-only, so an
    in-flight request cannot be re-pointed at another tenant), `RouteScopeResolver`
    and the authorization handler; a `scp=platform` token is rejected by every
    tenant route *even for a user holding an active tenant membership*, and a
    `scp=tenant` token is rejected inside `/api/v1/platform/**`.
  - **`HttpTenantContext`** — the sole binder from `HttpContext.User` claims to
    `ITenantContext`. It lives in the API project, not Infrastructure, so the data
    layer never takes a web dependency; an architecture test enforces that.
  - **Audit entry mapper and collector** — §16 implemented as a pure function and a
    request-scoped staging buffer that is drained into the caller's transaction
    (never saved itself).
  - **Forwarded-IP resolver** — `X-Forwarded-For` trusted only from configured
    proxy hops, the first untrusted hop taken, IPv4-mapped IPv6 collapsed.
  - **Security headers** — all nine headers of §12 with exact values.
  - **Committed-secret scan** — an architecture test that is a build failure if a
    credential is committed.
  - **API host wiring** — `StocklyDbContext` registered with a fail-fast
    connection string.
- 264 passing tests at the time of that milestone: 229 unit, 35 architecture.
  The current verified total is 440; see §14.

**Design deviations found and corrected while implementing** (each was a real
defect in the first implementation pass, not a documentation change):

- `identity.Tenant` inherited a tenant-scoped base, which added a `TenantId`
  column the ratified design does not have and made the tenant root subject to
  its own tenant filter. It now derives from `SoftDeleteEntity`.
- Composite `(XId, TenantId)` foreign keys require `UNIQUE (Id, TenantId)` on
  every tenant-scoped parent. This was missing, so no composite key could be
  emitted at all. Now a single `StocklyDbContext` convention rather than 66
  hand-written declarations that could each be forgotten.
- `TenantId` had no foreign key to `identity.Tenant` on most tables, leaving it a
  free-floating GUID that the filter would happily match. Now a convention, 57
  keys, `RESTRICT`.
- EF's default `CASCADE` produced 84 cascading deletes, including `product.Product`
  cascading into its own batches. Now `RESTRICT` everywhere except the two
  assignment keys on `identity.MembershipRoleWarehouse`.
- Six foreign keys pointed at the dependent's own table instead of the tenant
  root (`FK_Product_Tenant` referenced `master.Product`, not `identity.Tenant`).
- `Tenant.LegalName` was `NOT NULL`; the design says `nvarchar(300) NULL`.
- Several nullable provenance keys (`BatchId` on `GoodsReceiptLine`, typed source
  ids on `StockDocument`, `MealTypeId`) had no foreign key at all, which would
  have let the database accept a row pointing at nothing.
- The blanket cascade rule **deleted the tenant**. `MembershipRoleWarehouse`
  declares the tenant-root key like every other tenant-scoped table, so excluding
  it by declaring type alone left `ON DELETE CASCADE` on
  `FK_MembershipRoleWarehouse_Tenant_TenantId`. The rule now also excludes any
  foreign key whose principal is `identity.Tenant`, and the generated SQL shows
  exactly 2 cascades.

## 11. Pending Work

### 11.1 Design and authorisation gates — CLEARED 2026-09-30

1. ~~**Review and ratify `PROJECT_BLUEPRINT.md` v1.1** and the 44 `Proposed` ADRs
   (`PROJECT_BLUEPRINT.md` §27).~~ ✅ **DONE 2026-09-30 — RATIFIED.** ADR-0001 –
   ADR-0044 were promoted to `Accepted`; ADR-0045 – ADR-0050 were already
   `Accepted`. **50 of the 50 then-written ADRs were `Accepted`.**
   ADR-0051 was authored and ADR-0052 was ratified on 2026-11-24, so **52 of 52
   written ADRs are `Accepted`** (ISS-08 and ISS-12 closed).
2. ~~**Acknowledge the 9 known security limitations** (`SEC-KNW-01`–`SEC-KNW-09`)
   as accepted for V1.~~ ✅ **DONE 2026-09-30.** Acknowledged as known V1
   limitations. They remain registered; none was fixed or closed.

**The authorisation gate is closed. Phase 1 is authorised to begin.**

**Answering the 16 open items was never a Phase 1 precondition and still is not
one.** Each is phase-gated to the phase that implements it, or explicitly
deferred. §20 lists which phase each one waits for. Ratification resolved none of
them.

**No longer blocking — resolved 2026-09-30.** The five items that genuinely
blocked the Phase 1 foundations are decided: TBD-19 (locations), TBD-21
(currency), TBD-22 (transfers), TBD-24 (legal entities), TBD-25 (scale target) →
ADR-0045 – ADR-0049.

*ISS-04 (the empty nested repository `H:\Stockly\Stockly`) is not on this list:
it was resolved by the owner on 2026-09-27 by ignoring it through `.gitignore`.*

### 11.1a Open items, classified — NONE is an authorisation blocker

Pending work splits into two categories that must not be conflated:

| | Category | Blocks ratifying / authorising Phase 1? |
|---|---|---|
| **A** | **Authorisation and design blockers** | Yes — all cleared 2026-09-30, see §11.1 |
| **B** | **Implementation and verification prerequisites** | **No.** They gate *doing and proving* the work, not *starting* it |

Everything still open is category **B**. None of it blocks ratification or the
start of Phase 1, and none of it is a design or authority gap.

1. **Hosting / deployment target** (cloud provider, on-premise, or
   customer-managed). **Not an authorisation blocker** — production hosting is a
   later deployment concern. Affects Docker, backups and TLS termination; needed
   before the first deployment, not before the first commit.
2. **Commercial licence** (README currently `TBD`). **Not an authorisation
   blocker** — commercial/legal licensing is not required to ratify or start
   engineering work unless an actual dependency arises.
3. **Docker / SQL Server availability** (`TESTING_STRATEGY.md` TEST-TBD-06;
   ISS-03). **Not an authorisation blocker** — it never blocked ratifying or
   authorising Phase 1. Local SQL Server at `localhost\SQLEXPRESS` is available,
   so all DB-dependent tiers are executed and pass. Docker remains unavailable,
   with two consequences that stand:
   - **No container build, Compose startup or Nginx route can be claimed here.**
     Local reference scaffolding exists but is unvalidated.
   - **Phase 1 MUST NOT be declared complete while non-DB definition-of-done items
     remain open** (ISS-08, ISS-12, outbox readiness, owner phase gates). See §26
     (`PROJECT_BLUEPRINT.md`).

The ISS-03 caveat is now narrower: Docker is unavailable, but DB-backed tests
run against real SQL Server and are claimed only on that basis.

### 11.2 Phase 1 (now authorised)

Scope is taken verbatim from `PROJECT_BLUEPRINT.md` §25, which **ADR-0040** names
as the authoritative definition of the eight phases ("the eight implementation
phases, defined in `PROJECT_BLUEPRINT.md` §25").

- Solution scaffold: `Stockly.Domain`, `Stockly.Application`,
  `Stockly.Infrastructure`, `Stockly.Api`, test projects.
- `StocklyDbContext` with 12 schemas and the initial migration.
- The tenant-isolation data-access layer.
- The security spine: identity primitives, the session/scope model, audit, and
  the error contract.
- Pipeline: correlation id, security headers, authentication, authorization,
  rate limiting, exception handling, ProblemDetails.
- Docker, Nginx, and CI/CD — infrastructure is a **Phase 1** deliverable
  (`ARCHITECTURE.md` §11).
- Gate: **passing S1–S12** (`TESTING_STRATEGY.md` §3.4).

**Explicitly NOT Phase 1** (§25 assigns these to Phase 2, "Identity +
Tenants/Houses + Warehouses + RBAC"): seeding the 105-permission catalogue,
seeding the plan catalogue, seeding the 11 tenant roles, the D1–D5 derivation
(`ROLES_PERMISSIONS.md` §3.1.1), the full authorization chain, and
login/logout/refresh rotation, lockout and rate limits. Phase 1 builds the
identity *primitives* these depend on; §25.1 maps "Identity primitives in 1,
RBAC and login in 2".

#### 11.2a Scope contradiction found and resolved — 2026-09-30

An earlier revision of this section listed the seed of the 105-permission
catalogue, the plan catalogue, and the 11 tenant roles as **Phase 1**
deliverables. That contradicted `PROJECT_BLUEPRINT.md` §25, which places them in
**Phase 2**. This was a scope contradiction between two ratified documents, not
a styling difference, because it decided whether the D1–D5 role-derivation
engine was built in this phase.

**Resolution (owner decision, 2026-09-30): `PROJECT_BLUEPRINT.md` §25 governs.**
The status document was the lower-authority document and was corrected; §25 was
left untouched, as required by `AGENTS.md` §1 (the repository is the source of
truth and a discrepancy is corrected on the side that was wrong, not by
rewriting correct work). Verified before correcting: no ADR overrides §25 —
ADR-0040 defers to it explicitly, ADR-0044 fixes the *mechanism* of derivation
without assigning a phase, and ADR-0012 fixes the catalogue model without
assigning a phase. §25.1 corroborates §25.

**No new ADR was created for this resolution, and none should be.** The
substantive architecture decision is already carried by ADR-0040 and ADR-0044;
this entry records an interpretation conflict between two ratified documents,
not a new decision. Creating ADR-0051 would also invalidate the ratified count
of **50** ADRs that `AGENTS.md` §10, `PROJECT_BLUEPRINT.md` §0 and the
`DECISIONS.md` header all assert.

*(Superseded in part on 2026-10-03: `ADR-0051` turns out **not** to have been
available — the Phase 1 code had already claimed that identifier for the
tenant-isolation decision, which was never recorded. See §12 ISS-08. The 2026-10-03
engineering decision was therefore recorded as **ADR-0052** instead.)*

**Related pre-existing defect, not fixed here:** `DECISIONS.md` ADR-0012 states
the catalogue holds "107 of them" permissions. That was correct when written
(2026-09-27) and was superseded when the duplicate
`inventory.reason_codes.manage` was removed, taking the count to **105 in 13
namespaces** (`ROLES_PERMISSIONS.md` §2, `docs/CHANGELOG.md`). The stale figure
survives only inside the historical decision sentence and is left untouched
here rather than silently rewriting accepted ADR text; the owner should decide
whether to amend ADR-0012. It has no effect on any phase scope.

### 11.3 Later phases

See `PROJECT_BLUEPRINT.md` §25.

## 12. Known Issues

| ID | Issue | Impact | Plan |
|---|---|---|---|
| ISS-01 | ~~Blueprint is unratified~~ | ✅ **RESOLVED 2026-09-30** — the owner ratified blueprint v1.1 and ADR-0001 – ADR-0050 in full. The authorisation gate is closed and Phase 1 is unblocked | None. Re-opens only if the blueprint is revised |
| ISS-02 | 16 owner decisions unanswered | Behaviour **may** change — but none of them changes the V1 schema, and none blocks Phase 1 | Owner answers at the phase named in §20 |
| ISS-03 | ~~Docker unavailable locally~~ → **DB-backed tests unrunnable** | ✅ **RESOLVED 2026-10-03** — a local SQL Server at `localhost\SQLEXPRESS` (Windows auth) is available and does not require Docker. Integration (40) and security (20) tiers now run against real SQL Server and pass. **Docker itself is still unavailable**, so container-based validation remains unclaimed | None. Re-opens only if containerised CI is required |
| ISS-04 | Empty nested repository at `H:\Stockly\Stockly` (no commits; Git commands fail inside it) | ✅ **RESOLVED** — ignored via `.gitignore` (commit `fe2eb7a`) by owner decision. The repo remains on disk, untracked and empty; nothing was deleted and nothing was ever committed. It no longer pollutes `git status`. | None. Do not delete or commit it without a new owner decision |
| ISS-05 | No licence file | Legal ambiguity | Owner decision |
| ISS-06 | No hosting/deployment target decided | Affects Docker, backups, TLS termination | Owner decision |
| ISS-07 | Reservation semantics undefined (TBD-26) | None for V1 — `ReservedQuantity` is always `0` and no V1 flow uses it. Blocks only a future V2 reservations feature | Owner decision before any V2 reservation work |
| ISS-08 | ~~ADR-0051 was cited by 7 code sites but was never written~~ | ✅ **RESOLVED 2026-11-24** — the owner ratified the existing tenant-isolation design; ADR-0051 is now authored and `Accepted`, and `DATABASE_DESIGN.md` §6.1 and `SECURITY_ARCHITECTURE.md` §2.1 document RLS, TenantScope `CHECK`s and `SESSION_CONTEXT` | None. Re-opens only if the isolation design changes |
| ISS-09 | ~~The API does not start~~ | ✅ **RESOLVED 2026-11-23** — `UseExceptionHandler()` now has a fallback delegate that writes through `ProblemDetailsWriter` and refuses to write to aborted or already-started responses. A real process served `/health/live`, `/health/ready` and `/api-docs/openapi.json` with 200. `UseAuthorization()` still has no authentication handler; that is the approved Phase 1/2 boundary, not a startup defect | None. Re-opens only if the host fails to start again |
| ISS-10 | ~~Concurrency and migration test tiers are empty~~ | ✅ **RESOLVED 2026-11-23** — `Stockly.ConcurrencyTests` now has 3 real DB-backed tests and `Stockly.MigrationTests` has 4; both pass in the full solution run. A green solution run covers them | None. Re-opens only if either tier becomes empty or reports no tests |
| ISS-11 | ~~`dotnet ef` fails when pointed at `Stockly.Api` as startup project~~ | ✅ **RESOLVED 2026-11-23** — the documented command uses `--startup-project src/Stockly.Infrastructure`; local tool manifest pins `dotnet-ef` 10.0.12; model-drift check passes. Adding `Microsoft.EntityFrameworkCore.Design` to `Stockly.Api` was rejected because restore exposed vulnerable transitive packages | None. Re-opens only if the model-drift gate fails |
| ISS-12 | ~~ADR-0052 was `Proposed` and unratified~~ | ✅ **RESOLVED 2026-11-24** — the owner ratified the one-canonical-database convention; ADR-0052 is `Accepted` | None. Re-opens only if the database convention is deliberately superseded |

## 13. Known Security Issues

Code now exists, and four security defects found while writing it were fixed in
the same pass (see `CHANGELOG.md`, "Fixed — scope and header defects"). There are
**no known unfixed security defects in the implemented code**, with these two
limits on that claim:

- Only the security spine and anonymous health/OpenAPI endpoints are implemented.
  No login, session issuance, business endpoint or rate limiter exists yet, so no
  route is actually protected by the authorization code and no limit is enforced.
- The statement below rests on all six test tiers executed against
  `localhost\SQLEXPRESS`, including the populated concurrency and migration
  tiers. Docker validation remains impossible here.

**Inherent design limitations recorded at design time** — the full register is
`SECURITY_ARCHITECTURE.md` §15. None may be removed without a decision, a test,
and a status update:

| ID | Limitation | Severity | Plan |
|---|---|---|---|
| SEC-KNW-01 | Terminal possession is not cryptographically proven; an attacker with the enrollment code and a valid PIN can authenticate from anywhere on the network | Medium | V2 device attestation or per-device client certificate |
| SEC-KNW-02 | Shared terminals are usable by anyone with physical access to the name list | Medium (inherent to the PIN model) | Operational controls; V2 optional biometrics |
| SEC-KNW-03 | Access tokens cannot be revoked before 15-minute expiry (mitigated by per-request session state checks) | Low | Shorter tokens if acceptable |
| SEC-KNW-04 | No MFA in V1 | Medium | V2 TOTP/WebAuthn |
| SEC-KNW-05 | Rate limiting is per-instance in V1 | Low | Shared store before horizontal scaling |
| SEC-KNW-06 | No antivirus scan on attachments in V1 | Low | V1.1 ClamAV |
| SEC-KNW-07 | No WAF/DDoS service in V1 | Low | Phase 8 production hardening |
| SEC-KNW-08 | The terminal identity endpoint discloses the display names of the memberships assigned to a device's warehouse to anyone holding the enrollment code | Medium (reduced from High) | V2 display-name aliases, or require a successful PIN before any name is shown |
| SEC-KNW-09 | `IsPlatformAdmin` is all-or-nothing in V1: an operator holds all 11 `platform.*` codes, including `platform.maintenance` | Medium | V2 split operator roles (RP-TBD-06) |

**Security design decisions already locked in** (no known defect): tenant from
session only, composite tenant foreign keys, `TenantId`-leading unique indexes,
guarded atomic stock updates with `CHECK` constraints on absolute invariants
only, append-only audit with no `RowVersion`, server-side authorization,
`scp`-separated platform and tenant token scopes, HMAC-hashed throttle
identifiers, idempotency keys, mass-assignment rejection, secret redaction.

**Security consequences of the 2026-09-30 owner decisions** — all of these
*reduce* the attack surface rather than adding to it:

| Decision | Security effect |
|---|---|
| TBD-19 / ADR-0045 — warehouse-level stock | The authorization chain ends at the warehouse. There is no location scope to bypass, and an unknown `locationId` is `400`, not a silent ignore. A V2 location redesign **must** add its own assignment check (§3, §9) |
| TBD-21 / ADR-0046 — single currency, no FX | No conversion path exists, so no rounding, rate-tampering or cross-currency-override surface exists. A request carrying a contradicting `currencyCode` is `400` |
| TBD-22 / ADR-0047 — instantaneous transfers | No in-transit state means no partially-received transfer to exploit or reconcile, and the composite FK makes a cross-tenant transfer unrepresentable in the schema. Both legs remain fully audited |
| TBD-24 / ADR-0048 — one tenant = one legal entity | `TenantId` is the only organizational key, so the chain has no intermediate level that could be partially authorized or omitted |
| TBD-26 / ADR-0050 — no V1 reservations | `ReservedQuantity` is `0` and `inventory.reservations.manage` is granted to nobody, so no V1 request can act on a reserved quantity |

`TESTING_STRATEGY.md` §3.2 now specifies **absence tests** for each of these
prohibitions, because a decision expressed as a prohibition cannot be verified by
reading a list of what exists.

## 14. Tests

| Suite | Project | Tests written | Executed | Result |
|---|---|---|---|---|
| Unit | ✅ | **322** | ✅ | **PASS** (322/322) |
| Architecture | ✅ | **51** | ✅ | **PASS** (51/51) |
| Integration | ✅ | **40** | ✅ | **PASS** (40/40) against real SQL Server |
| Security | ✅ | **20** | ✅ | **PASS** (20/20) against real SQL Server |
| Concurrency | ✅ | **3** | ✅ | **PASS** (3/3) against real SQL Server |
| Migration | ✅ | **4** | ✅ | **PASS** (4/4) against real SQL Server |

**440 tests pass; 0 fail; 0 skipped.** Verified 2026-11-24 by
`dotnet test Stockly.slnx --no-build --no-restore` with
`STOCKLY_TEST_SQLSERVER` set to the canonical local instance. `dotnet format
Stockly.slnx --verify-no-changes --no-restore`, `dotnet build Stockly.slnx
--no-restore -warnaserror`, local tool restore and EF model-drift check all
passed in the same verification pass.

**Unit (322)** — domain/EF model, error catalogue, ProblemDetails shape, audit
redaction and mapping, scope parsing, tenant context, headers and forwarded-IP
resolution. These need no database.

**Architecture (51)** — layering, prohibited-feature absence tests, API-host
exception-handler regressions and committed-secret scanning. The secret scan
reads source files from disk, so it covers `appsettings*.json`, launch settings,
`.http`, Docker and Compose files. It passed with the new `Dockerfile`,
`docker-compose.yml`, Nginx configuration and CI workflow present.

**Database-backed (67)** — integration 40, security 20, concurrency 3 and
migration 4. All ran against the canonical `Stockly` database on
`localhost\SQLEXPRESS`. The concurrency tier asserts stale-rowversion rejection,
single-winner unique-key races and transaction rollback. The migration tier
asserts migration declaration/applied state, initial schema shape, tenant-isolation
artifacts and trusted tenant-scope guards. The 7 new integration tests cover
transactional commit/rollback with a business row, once-only dispatch, retry and
dead-letter, stale-lease recovery, unknown-handler release, readiness degrade and
recovery, and fail-closed behaviour when no monitor is configured.

The 33 gating security classes `S1`–`S33` in `TESTING_STRATEGY.md` remain a
**specification of work to be done**; the current 20 security tests are real but
do not claim full S1–S33 coverage.

## 15. Build Status

**PASS.** `dotnet build Stockly.slnx --no-restore -warnaserror` → *Build
succeeded, 0 Warning(s), 0 Error(s)*. `Directory.Build.props` enforces `net10.0`,
nullable, implicit usings, deterministic builds and **warnings-as-errors**, so a
warning is a build failure. Advisories were fixed by upgrading packages rather
than suppressing `NU1903`: `System.Security.Cryptography.Xml` → `10.0.13`,
`System.Security.Cryptography.Pkcs` → `10.0.13`, and `Microsoft.OpenApi` →
`2.7.5` (CVE-2026-49451, GHSA-v5pm-xwqc-g5wc). The unused `Openapi`
(SwaggerUI) package was removed; it pulled a vulnerable `RestSharp` and a
net4x-only restore. Local tools are pinned to `dotnet-ef` 10.0.12 and
`dotnet-reportgenerator-globaltool` 5.6.0 in `.config/dotnet-tools.json`.

## 16. Database / Migrations Status

**Two migrations written and applied: `InitialSchema` and
`TenantIsolation`.** Generated with `dotnet ef migrations add`; idempotent SQL
was inspected and both migrations are applied to the canonical database. No
design count changed.

| Property | Value |
|---|---|
| Tables created | **66** across **12 schemas** (+ `__EFMigrationsHistory`) |
| Per-schema | platform 6 · identity 16 · ops 2 · master 7 · inventory 8 · purchasing 10 · events 5 · food 5 · notifications 3 · files 1 · extensibility 2 · audit 1 |
| Foreign keys | 185 (122 composite `(XId, TenantId)`) |
| `TenantId → identity.Tenant` keys | 57 — one per tenant-scoped table |
| `(Id, TenantId)` alternate keys | 57 |
| CHECK constraints | 96 |
| Unique indexes | 70, of which 34 are filtered (`WHERE IsDeleted = 0` / `IS NULL` / `IS NOT NULL`) |
| `rowversion` columns | 53 — every mutable table, none of the 6 append-only tables |
| `ON DELETE CASCADE` | **2**, both on `identity.MembershipRoleWarehouse` |

**Both migrations have been applied and verified against SQL Server.** The
migration conformance tier checks the catalogue shape and tenant-isolation
artifacts on the real engine; the design-time model-drift check reports no
pending changes. This is real-engine evidence, not a substitute based on an
in-memory provider.

No open database question blocks Phase 1. All 12 `DB-TBD` items are disposed: 5
are decided owner decisions (DB-TBD-02, 04, 05, 10, 11), 4 are design decisions or
a safe assumption (DB-TBD-01, 07, 08 and the trigram fallback), and 3 remain open
but are phase-gated with no migration required either way (DB-TBD-06, 09, 12).

## 17. Infrastructure Status

**Local reference scaffold exists; production deployment does not.** Added
2026-11-23: `Dockerfile`, `.dockerignore`, `docker-compose.yml`,
`infra/nginx/nginx.conf`, `infra/nginx/certs/.gitkeep`, a manual `migrate`
Compose profile, `.config/dotnet-tools.json` and `.github/workflows/ci.yml`.
The scaffold follows `ARCHITECTURE.md` §11 (Nginx TLS termination, internal API,
internal SQL Server, named volumes, injected secrets, gated migrations) and
deliberately encodes no licence, hosting target, domain/certificate source or
rate-limit decision. On 2026-11-24 the restore defect was fixed: the image now
restores and publishes `src/Stockly.Api/Stockly.Api.csproj` directly instead of
restoring `Stockly.slnx` without its test projects. The exact API restore and
Release publish commands were run locally with no errors. Docker is still
unavailable here, so no image build, Compose startup, Nginx request or container
scan has been run. CI workflow YAML has not been executed by GitHub in this pass.

## 18. Security Assumptions (to be confirmed by the owner)

1. Shared terminals are physically inside the warehouse they belong to, and
   access to the physical device is controlled operationally.
2. Selecting a name from a list is not treated as authentication anywhere.
3. PIN secrecy is an operational control; the system compensates with throttling,
   lockout, short sessions, and full auditing.
4. Tenant administrators are trusted within their tenant; cross-tenant access
   requires a platform administrator.
5. Platform operators are trusted but always audited, and are all-or-nothing in
   V1 (SEC-KNW-09).
6. Each organisation operates warehouses in a single country/timezone per tenant
   (multi-timezone display is a V2 consideration). Assumption 6 is consistent
   with, and reinforced by, TBD-24 / ADR-0048: one tenant is one legal entity
   operating its own country, currency and timezone.
7. The organisation accepts that subscription enforcement is application-level,
   not cryptographically tamper-proof against a modified application build.
8. The ~50 warehouse / ~100 k product / ~10 M ledger row / ~200 concurrent user
   figures (TBD-25 / ADR-0049) are **engineering targets the organisation accepts
   as sufficient for V1 sizing**. They are not commercial commitments and are not
   enforced as quotas.

## 19. Next Required Action

**Continue Phase 1 — Foundation + Architecture + Database + Security Core**
(`PROJECT_BLUEPRINT.md` §25). It is authorised and partly implemented, not
complete.

The frontend skeleton may begin now under outcome **B** in §3a, but only as a
mocked/static shell. It must not be presented as a data-connected UI or as
evidence that Phase 8 has started.

Ratification of `docs/PROJECT_BLUEPRINT.md` v1.1 and of ADR-0001 – ADR-0050 was
granted on 2026-09-30, and ADR-0051/ADR-0052 were ratified on 2026-11-24, so no
further authorisation is needed to continue Phase 1. The remaining work is in §4:
audit drain call site, route-level scope policy, concrete outbox handlers,
column-level migration fidelity and Docker/CI validation.

The 16 open decisions still do **not** need to be answered to continue Phase 1 —
each is phase-gated or deferred (§20) — and ratification resolved none of them.
ISS-08 and ISS-12 are closed. Docker is unavailable locally, so no
container-based claim may be made.

## 20. Blocked Items

### 20.1 Waiting on a specific phase (not blocking Phase 1)

| Item | Blocked by | Answers needed by |
|---|---|---|
| Password policy details | TBD-01 | Phase 2 |
| Identity providers for V1 | TBD-03 | Phase 2 |
| PIN brute-force thresholds | TBD-04 | Phase 2 |
| Session timeouts | TBD-05 | Phase 2 |
| Concurrent-session policy per device | TBD-11 | Phase 2 |
| `RETURN` semantics | TBD-06 | Phase 3 |
| Over-receipt tolerance | TBD-08 | Phase 4 |
| Approval threshold model | TBD-09 | Phase 4 |
| Near-expiry window / expired-stock policy | TBD-07 | Phase 5 |
| Costing per serving, consumption basis, pack-size rounding | TBD-10 | Phase 6 |
| Subscription grace period | TBD-12 | Phase 7 |
| Report localisation scope | TBD-15 | Phase 7 |
| Audit retention | TBD-14 | Phase 8 |

### 20.2 Deferred, no phase scheduled

| Item | Blocked by | Notes |
|---|---|---|
| Stock reservation semantics (hard vs soft, release, lifecycle) | TBD-26 | V2 candidate. No V1 impact — `ReservedQuantity` is always `0` |
| Billing provider | TBD-13 | V1 has no payment processing |
| External event registration integration | TBD-23 | No integration in any V1 tier |

### 20.3 Blocking Phase 1 completion claims

| Item | Blocked by |
|---|---|
| Final Phase 1 completion claim | Audit drain call site, route-level scope policy, concrete outbox handlers and column-level migration fidelity |
| S1–S12 completion claim | Phase 2 authentication, sessions and rate limiting; phase-boundary tests forbid adding them now |
| Docker/Compose/Nginx validation | Docker is unavailable on this workstation; the restore defect is fixed but no container has been built |
| Deployment/licence/hosting choices | Owner decisions (ISS-05, ISS-06) |

### 20.4 No longer blocked

These must **not** be listed as open anywhere:

| Item | Resolution |
|---|---|
| Password/PIN hashing | TBD-02 → Argon2id, ADR-0008 |
| Search implementation | TBD-17 → SQL Server full text, ADR-0042 |
| Barcode symbology set | TBD-18 → the six modelled values, ADR-0043 |
| Offline write behaviour | TBD-20 → no offline writes, ADR-0021 |
| Fractional quantities | TBD-16 → safe `decimal(18,4)` assumption, DB-TBD-01 |
| Warehouse sub-locations | TBD-19 → **decided 2026-09-30**, ADR-0045: warehouse-level stock in V1; descriptive locations V1.1; location-level stock is a V2 redesign |
| Currency / FX | TBD-21 → **decided 2026-09-30**, ADR-0046: one currency per tenant, no FX, no conversion |
| In-transit transfers | TBD-22 → **decided 2026-09-30**, ADR-0047: instantaneous, atomic, no in-transit state |
| Multi-entity tenants | TBD-24 → **decided 2026-09-30**, ADR-0048: one tenant is one legal entity |
| Reporting scale target | TBD-25 → **decided 2026-09-30**, ADR-0049: ~50 warehouses, ~100 k products, ~10 M ledger rows, ~200 concurrent users — engineering targets, not subscription limits |
| Stock reservations | Split out of TBD-10 → **TBD-26**, deferred and unresolved (ADR-0050). This is **not** resolved — it moved |
| ADR-0051 traceability | Authored and ratified 2026-11-24 (ISS-08 closed); design sections added to `DATABASE_DESIGN.md` and `SECURITY_ARCHITECTURE.md` |
| ADR-0052 ratification | Ratified 2026-11-24 (ISS-12 closed) |
| Outbox readiness policy | Owner approved 2026-11-24: any `DeadLetter` or `Pending`/`Processing` older than 15 minutes degrades readiness; thresholds configurable; read-only monitor and write-capable dispatcher are separate platform principals |
