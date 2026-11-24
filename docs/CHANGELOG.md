# STOCKLY — CHANGELOG

All notable changes to Stockly are recorded here. Trivial formatting changes are
not recorded. Security-relevant and database-relevant changes are **always**
recorded, even in early phases.

**Format:** Keep a Changelog — `Added` / `Changed` / `Deprecated` / `Removed` /
`Fixed` / `Security` / `Database` / `Tests` / `Breaking`.

---

## [Unreleased]

### Added — 2026-11-24 (initial frontend skeleton)

- **`client/` React + TypeScript + Vite frontend skeleton.** Implements 34
  Arabic RTL page routes (8 platform, 8 organization, 18 warehouse), the
  three-context shell, breadcrumbs, command palette, responsive sidebar,
  context selectors, notifications, toasts, reusable tables/forms/dialogs/charts,
  typed i18n modules, isolated `origin: 'demo'` data and 5 smoke tests.
  All page content is built; no page renders `PlaceholderPage`.
- **`docs/INITIAL_FRONTEND_IMPLEMENTATION.md`** records the implemented,
  local-only and not-started status, verification evidence and file map.
- **`.gitignore`** now allows the committed hand-written
  `client/public/manifest.webmanifest` while still ignoring generated PWA assets.

### Tests — 2026-11-24 (initial frontend skeleton)

- Frontend gates pass: `npm run format:check`, `npm run typecheck`,
  `npm run lint`, `npm test` (5 passed) and `npm run build`.
- Production-build browser scan verified all 34 routes as RTL with a heading and
  zero placeholder components, plus context switching, toast, command palette,
  search empty state, modal and no console/page errors.
- Backend regression re-run after the frontend work: clean format, clean
  `-warnaserror` build, clean EF model-drift check and **440 passed / 0 failed /
  0 skipped** with `STOCKLY_TEST_SQLSERVER` set. Docker validation remains
  unavailable and unclaimed.

### Fixed — 2026-11-24 (initial frontend skeleton)

- Generic demo organisation name and code replace the first-target customer name
  in frontend demonstration data, preserving the blueprint rule that no tenant
  name is hardcoded in source.
- Frontend README and smoke-test wording no longer describe the built pages as
  placeholders.

### Added — 2026-11-24 (Phase 1 ADR/outbox closure)

- **ADR-0051 — database-enforced tenant isolation.** Authored and ratified to
  record the already-applied RLS, TenantScope `CHECK` and `SESSION_CONTEXT`
  design cited by seven code sites (ISS-08 closed). Added §6.1 to
  `DATABASE_DESIGN.md` and §2.1 to `SECURITY_ARCHITECTURE.md`. No schema or
  product count changed.
- **ADR-0052 ratified.** The one-canonical-`Stockly`-database convention is now
  `Accepted` (ISS-12 closed). The ratified ADR count is **52**, not 50.
- **Outbox readiness and dispatcher foundation.** `/health/ready` now gates on an
  `ops.OutboxMessage` snapshot and returns `503` when the monitor is unavailable,
  any message is `DeadLetter`, or a `Pending`/`Processing` row is older than the
  configurable stale threshold (default 15 minutes). Added an options model, a
  read-only platform-scoped monitor, a write-capable platform-scoped coordinator,
  a background dispatcher, handler abstraction and DI registration. Thresholds,
  lease and poll interval are configurable.
- **Outbox environment template.** `.env.example` now includes separate
  `StocklyOutboxMonitor` (read-only) and `StocklyOutboxWorker` (write-capable)
  connection strings and the `Outbox__*` options. No secret value was added.

### Tests — 2026-11-24 (Phase 1 ADR/outbox closure)

- Added seven DB-backed `OutboxIntegrationTests`: transactional commit/rollback
  with a business row, once-only dispatch, retry/backoff and dead-letter,
  stale-lease recovery, unknown-handler release, readiness degrade/recovery, and
  fail-closed readiness without a monitor.
- Updated the existing API host tests to configure the platform monitor and
  disable the dispatcher. Full solution total is now **440 tests** (322 unit,
  51 architecture, 40 integration, 20 security, 3 concurrency, 4 migration), all
  passing against real SQL Server.

### Fixed — 2026-11-24 (Phase 1 ADR/outbox closure)

- **Dockerfile restore defect.** The image restored `Stockly.slnx` without copying
  the test projects it references. It now restores and publishes
  `src/Stockly.Api/Stockly.Api.csproj` directly. Both commands were run locally;
  Docker itself remains unavailable, so no container build/run is claimed.

### Added — 2026-11-23 (Phase 1 verification and reference infrastructure)

- **Local deployment reference scaffold.** Added `Dockerfile`, `.dockerignore`,
  `docker-compose.yml`, `infra/nginx/nginx.conf`, `infra/nginx/certs/.gitkeep`
  and a manual `migrate` Compose profile. The scaffold keeps SQL Server and the
  API internal, injects secrets from the local environment, uses a non-root
  read-only API container and applies migrations only as a separate gated step.
  It deliberately encodes no licence, hosting target, domain/certificate source
  or rate-limit decision. Docker is unavailable locally, so no container build
  or Compose run has been claimed.
- **CI workflow.** Added `.github/workflows/ci.yml` for format, warnings-as-errors
  build, local tool restore, EF model drift, committed-secret scanning, unit and
  architecture tests, and sequential DB-backed integration, security,
  concurrency and migration tests with coverage artifacts. Frontend, nightly,
  release, dependency-scan and container-scan gates remain unimplemented and are
  reported as such.
- **Pinned local tools.** Added `.config/dotnet-tools.json` with `dotnet-ef`
  10.0.12 and `dotnet-reportgenerator-globaltool` 5.6.0. The EF tools version
  warning observed with the global 10.0.9 tool is gone.
- **`docs/BACKEND_API_CONTRACT_GAPS.md`** — a source-of-truth audit of the
  ratified API contract versus the implemented host, including per-area missing
  DTOs, permissions, warehouse-scope enforcement and conflicting route names.

### Security — 2026-11-23 (Phase 1 verification and reference infrastructure)

- `System.Security.Cryptography.Xml` was raised to `10.0.13` to close
  `GHSA-7hqp-7c5m-4vq6`; `System.Security.Cryptography.Pkcs` is pinned to
  `10.0.13`. Restore and warnings-as-errors build pass with no advisories
  suppressed.
- Adding `Microsoft.EntityFrameworkCore.Design` to the API project was rejected:
  restore exposed transitive NU1510/NU1902/NU1903 vulnerabilities. The safe EF
  startup path is `src/Stockly.Infrastructure`, now pinned through the local
  tool manifest.

### Tests — 2026-11-23 (Phase 1 verification closure)

- ISS-10 resolved. `Stockly.ConcurrencyTests` now contains 3 real DB-backed
  tests (stale `rowversion`, duplicate business-key race and rollback), and
  `Stockly.MigrationTests` contains 4 real conformance tests.
- Full solution run: **433 passed, 0 failed, 0 skipped** — 322 unit,
  51 architecture, 33 integration, 20 security, 3 concurrency, 4 migration —
  against `localhost\SQLEXPRESS`.
- `dotnet format Stockly.slnx --verify-no-changes --no-restore`,
  `dotnet build Stockly.slnx --no-restore -warnaserror`, `dotnet tool restore`
  and `dotnet ef migrations has-pending-model-changes` all passed.

### Fixed — 2026-11-23

- ISS-11 resolved. The documented model-drift command uses
  `--startup-project src/Stockly.Infrastructure` and a pinned local EF tool;
  no vulnerable Design package was added to the API.
- ISS-10 resolved. A green solution run now includes real concurrency and
  migration coverage instead of empty test projects.

### Added — 2026-11-23

- **Initial Phase 1 API host endpoints.** `GET /health/live`, `GET /health/ready`
  and `GET /api-docs/openapi.json` are mapped and covered by API host integration
  tests. Readiness verifies database connectivity and applied migrations; the
  documented outbox-backlog check is not implemented and is reported in
  `DEVELOPMENT_STATUS.md`.
- **`docs/IMPLEMENTATION_AUDIT.md`** — a source-of-truth audit of the approved
  design versus the implemented code, including module-by-module status and the
  explicit Phase 1 / later-phase boundary.
- **`docs/INITIAL_SKELETON.md`, `docs/IMPLEMENTATION_STATUS.md` and
  `docs/NEXT_DEVELOPMENT_STEPS.md`** — the requested truthful skeleton handoff:
  what exists, what is only a model/schema, what is missing and why.

### Fixed — 2026-11-23

- **ISS-09 resolved — the API now starts.** `UseExceptionHandler()` has a fallback
  delegate that writes through the existing `ProblemDetailsWriter` and refuses to
  write to aborted or already-started responses. A real `dotnet run` process
  returned `200 OK` from all three new endpoints. No authentication, RBAC
  evaluator, rate limiter or catalogue seeder was added to bypass the Phase 1/2
  boundary.

### Tests — 2026-11-23

- **426 tests pass against real SQL Server** (`localhost\SQLEXPRESS`): 322 unit,
  51 architecture, 33 integration, 20 security; 0 failed, 0 skipped. Four new API
  host tests verify startup, health, OpenAPI sanitisation, correlation ids and
  security headers; two architecture tests guard the exception-handler fallback.
- `dotnet format Stockly.slnx --verify-no-changes` and `dotnet build Stockly.slnx
  --no-restore` are clean. `Stockly.MigrationTests` and
  `Stockly.ConcurrencyTests` remain empty (ISS-10), so a solution-level green run
  is still not coverage of those tiers.

### Changed — 2026-11-23

- `DEVELOPMENT_STATUS.md` and `TESTING_STRATEGY.md` updated from the verified
  2026-10-03 baseline to the current host/endpoint state and 426-test count.

### Added — 2026-10-03

- **ADR-0052 — one canonical `Stockly` database for development and all test
  tiers (`Proposed`, awaiting owner ratification).** There is exactly one Stockly
  database, created only by EF Core migrations. Integration and security tiers
  both target it and isolate by tenant, never by database. Includes the schema
  conformance gate (`tools/Stockly.VerifyDatabase.sql`) and the `STOCKLY_TEST_SQLSERVER`
  targeting contract.
- **`tools/Stockly.VerifyDatabase.sql`** — asserts the canonical catalogue
  invariants: 13 schemas, 66 domain tables, 185 FKs, 156 `CHECK`s, 60 TenantScope
  `CHECK`s, 60 RLS filter predicates, and 0 untrusted or disabled constraints.
- **Verified state of the canonical database** — both migrations
  (`InitialSchema`, `TenantIsolation`) applied to `localhost\SQLEXPRESS`; schema at
  head; `dotnet ef migrations has-pending-model-changes` reports no changes.

### Database — 2026-10-03

- **Removed 12 redundant databases.** `Stockly_Phase1_Constraints`,
  `Stockly_Phase1_Engine`, `Stockly_Phase1_Fidelity`, `Stockly_Phase1_Fks`,
  `Stockly_Phase1_Migrations`, `Stockly_Phase1_SecTenantIsolation`,
  `Stockly_Phase1_Tables`, `Stockly_Phase1_TenantIsolation`, `Stockly_Phase1_Test`,
  `Stockly_FreshTest`, `Stockly_DdlProbe`, `Stockly_PermProbe`.
  Justification: they were unreferenced, and inspection showed the stale copies
  carried **0 RLS policies and 0 TenantScope `CHECK` constraints** against the
  canonical database's 1 policy and 60 scope checks. Unrelated databases on the
  instance (`DormMaster`, `Diakonia`, `HMS`, `HMS_E2E`) were not touched.
- **Removed 2 malformed databases** named `Server=localhost;Database=master;...`
  and `Server=localhost\SQLEXPRESS;...` — artifacts of the fixture defect below.
- **Removed leaked test residue from the canonical database**: 290 tenants and 276
  units left behind by earlier test runs. The RLS policy was verified still enabled
  afterwards, and the catalogue invariants are intact.

### Fixed — 2026-10-03

- **Test fixture targeted the wrong database.** `SqlServerFixture` passed an entire
  connection string as `InitialCatalog`, so SQL Server created databases literally
  named `Server=localhost;...`, and suites ran against empty freshly-migrated
  schemas instead of the canonical one. This made DB-backed isolation tests
  **false greens**.
- **`sp_getapplock` timeout was passed in the wrong unit.** `@LockTimeout` is
  milliseconds; seconds were supplied, so the lease was acquired for ~900 ms and
  concurrent suites failed spuriously. Suites now pass three consecutive concurrent
  runs.
- **Integration tests leaked rows and could not clean up.** `TenantIsolationTests`
  now implements `IAsyncLifetime`, tracks only the tenants/warehouses/units/products
  it creates, and deletes children before parents using `IgnoreQueryFilters()`.
- **Test data teardown is now load-bearing.** Because all tiers share one database
  (`ADR-0052`), a test that leaks rows corrupts later tests, not just the catalogue view.
- **`dotnet format --verify-no-changes` now passes.** The documented CI gate (step 1)
  was failing on 51 whitespace diagnostics across 10 files; corrected.
- **`Program.cs` cited the wrong ADR for the database predicates**, pointing at an
  identifier that now denotes a different decision. Restored to the identifier the
  other seven call sites use.

### Security — 2026-10-03

- **Documented the tenant-isolation enforcement gap.** Tenant isolation (RLS,
  per-table TenantScope `CHECK`s, `SESSION_CONTEXT` scope channel) is implemented,
  applied and covered by 20 passing security tests, but **has no ADR and no
  section in `DATABASE_DESIGN.md` or `SECURITY_ARCHITECTURE.md`**. Seven code
  sites cite it as *ADR-0051*, an identifier that has never existed in
  `DECISIONS.md`. ADR-0051 is now explicitly **reserved** and must not be reused.
  Tracked as **ISS-08**; authoring and ratifying it is an owner action.
- **Never claim the security suites as passing without saying which database they
  ran against.** `TESTING_STRATEGY.md` §1 now records that a passing run is only
  meaningful against the schema that actually carries the enforcement.

### Tests — 2026-10-03

- **420 tests pass against real SQL Server** (`localhost\SQLEXPRESS`): 322 unit,
  49 architecture, 29 integration, 20 security; 0 failed, 0 skipped.
- **Zero residual test rows** after a full run.
- **`Stockly.StocklyDbTestSupport` marked `IsTestProject=false`** so a solution-wide
  `dotnet test` no longer launches a test host for the shared fixture library.
- **ISS-03 resolved.** A local SQL Server is available without Docker, so the
  DB-backed tiers can be executed and claimed. Docker itself is still unavailable,
  so container-based validation remains unclaimed.
- **New defects recorded, not fixed here:** **ISS-09** — the API does not start
  (verified: `UseExceptionHandler()` is configured with neither
  `ExceptionHandlingPath` nor `ExceptionHandler`, throwing from
  `ApplicationBuilder.Build()`); **ISS-10** — `ConcurrencyTests` and
  `MigrationTests` contain no tests, yet `dotnet test` exits 0 for them, so a green
  solution run is a false green; **ISS-11** — the documented CI step 5 command
  cannot run against `Stockly.Api` and needs `--startup-project
  src/Stockly.Infrastructure`.

### Changed — Phase 1 persistence foundation (2026-10-01)

### Added — Phase 1 persistence foundation (2026-10-01)

The first implementation code in the repository. **Phase 1 is in progress, not
complete.** No security spine, API pipeline, Docker/Nginx or CI exists yet, and
no SQL Server-dependent test has been written or run (ISS-03).

- **Solution scaffold** — `Stockly.Domain`, `Stockly.Application`,
  `Stockly.Infrastructure`, `Stockly.Api`, and six test projects
  (`UnitTests`, `IntegrationTests`, `SecurityTests`, `ConcurrencyTests`,
  `ArchitectureTests`, `MigrationTests`). `Directory.Build.props` centralises
  `net10.0`, nullable, implicit usings, deterministic build and
  **warnings-as-errors**.
- **Domain** — 66 entities across the 12 documented schemas, and the shared bases
  that carry the tenant contract: `ITenantScoped`, `IGlobalEntity`,
  `IHasRowVersion`, `ISoftDelete`, `IAuditable`.
- **Infrastructure** — `StocklyDbContext` (66 `DbSet`s, schema mapping, global
  tenant query filter, soft-delete filter), `ITenantContext` /
  `FixedTenantContext`, 66 EF Core configurations, and
  `DesignTimeStocklyDbContextFactory`.
- **`InitialSchema` migration** — 66 tables, 185 foreign keys (122 composite
  `(XId, TenantId)`), 96 CHECK constraints, 70 unique indexes (34 filtered),
  53 `rowversion` columns, and **2** `ON DELETE CASCADE` — both on
  `identity.MembershipRoleWarehouse`, as the design allows.
- **Security spine** — the four Phase 1 security deliverables
  (`PROJECT_BLUEPRINT.md` §25):
  - **Error contract** (`Common/Errors`) — all 27 documented codes with their wire
    string, HTTP status and RFC 9457 `type` pinned to `BUSINESS_RULES.md` §14;
    `ApiProblem`; `BusinessException`. `account_locked` is reserved and cannot be
    emitted at all. `500`/`503` carry no `code`, because §6.3 defines those statuses
    and §14 defines no code for them — none was invented.
  - **RFC 9457 writer + global exception handler** (`Stockly.Api/Common`) — one
    place a problem body is produced, over a closed exception mapping, so
    `ErrorSafetyTests` (S22) is structural rather than a review item. `Retry-After`
    is set on `429` and `Cache-Control: no-store` on every error body.
  - **Correlation id** (`CorrelationIdMiddleware`) — echoes `X-Correlation-Id`,
    generates one otherwise, and rejects an inbound value that is over-long or
    contains anything outside `[A-Za-z0-9-_.:]`, since it is written back into a
    header and into logs.
  - **Audit secret redactor** (`AuditSecretRedactor`) — refuses a payload carrying
    a secret, by whole-token name match *and* by value shape (JWT, connection
    string), at any nesting depth, in object graphs, dictionaries, collections and
    `JsonElement`. Matches whole tokens rather than substrings so `shipping` and
    `saltiness` are not false positives.
  - **Session/scope model** (`RouteScopeResolver`, `RequestScope`) — ADR-0034 as a
    pure function: a `scp=platform` token is rejected by every tenant route even
    when the same user holds an active tenant membership, and a `scp=tenant` token
    is rejected inside `/api/v1/platform/**`. `RequestScope` is a sealed record
    with init-only members, so an in-flight request cannot be re-pointed at another
    tenant, and `RequireTenantId()` refuses rather than returning a null a caller
    might forget to check.
  - **`HttpTenantContext`** — the only binder from `HttpContext.User` claims to
    `ITenantContext`. Deliberately in the API project, not Infrastructure, so the
    data layer takes no web dependency.
- **168 tests pass, 0 fail, 0 skipped** (`dotnet test Stockly.slnx`):
  - **136 unit** — 14 EF model/tenant-isolation guards, plus 122 covering the error
    catalogue, RFC 9457 shape, the reserved code, the redactor (false positives
    included) and the ADR-0034 scope rule.
  - **32 architecture** — layering (`Domain` depends on nothing Stockly;
    `Application` depends on neither `Infrastructure` nor `Api`; `Infrastructure`
    depends on neither `Api` nor ASP.NET Core; only `Api` touches ASP.NET Core),
    plus the **absence tests** for all five 2026-09-30 owner decisions. These are
    the highest-value tests in the suite because each decision is a *prohibition*,
    and a prohibition cannot be verified by reading a list of what exists. Each
    carries a counter-test so it cannot be "fixed" by widening a rule until it
    rejects legitimate fields: `ShelfLifeDays` is expiry and not a location,
    `UnitConversion` is a mass/volume ratio and not FX, and
    `PurchaseRequest.ConvertedPurchaseOrderId` is document state and not a currency
    conversion.

No design count changed: 66 entities / 12 schemas · 105 permissions ·
11 + 1 authorities · 147 rules · 27 error codes · 19 workflows · 8 phases ·
50 ADRs.

### Added — audit pipeline, security headers and committed-secret scanning (2026-10-01)

Continuing Phase 1. Still **not complete**: the audit *writer* transaction
integration, DB-backed suites, Docker/Nginx and CI do not exist yet, and no
SQL Server-dependent test has been written or run (ISS-03).

- **Audit entry mapping** (`Application/Auditing/AuditEntryMapper`) — `PROJECT_BLUEPRINT.md`
  §16 implemented as a pure function, so the rules are provable with no database.
  A `Security` event that did not succeed can never be filed as `Info`; a failed
  login with no session records the **absence** of an actor rather than a
  zero GUID; every before/after payload passes `AuditSecretRedactor` and the whole
  write is **refused** if it carries a secret, because an append-only table cannot
  be scrubbed afterwards.
- **Audit collector** (`IAuditCollector` / `AuditCollector`) — request-scoped
  staging that deliberately **does not save**. §16 rule 6 requires the row to
  commit or roll back with the business change, which is only possible if the
  caller drains the buffer into its own change set. Draining clears the buffer so
  a retry cannot double-write.
- **Caller-IP resolution** (`ForwardedIpResolver`) — `SECURITY_ARCHITECTURE.md` §6:
  `X-Forwarded-For` is walked right-to-left and the first **non**-trusted hop is
  taken, so an injected address left of an untrusted hop is discarded. Rotating a
  fake header cannot change the resolved address, which is the bypass S10 exists to
  prevent. With no configured proxy the socket address is used. IPv4-mapped IPv6
  is collapsed so one client cannot hold two identities in the throttle ledger.
- **Route-scope authorization handler** (`ScopeRequirementHandler`) — the ASP.NET
  bridge to the existing `RouteScopeResolver`. The scope comes from the request
  path, never from a claim, so a caller cannot nominate its own scope. A mismatch is
  `403 permission_denied`, not `401`, and the body does not say which scope was
  required.
- **Security headers** (`SecurityHeadersMiddleware`) — all nine headers of
  `SECURITY_ARCHITECTURE.md` §12 with the exact specified values. HSTS only over
  TLS; `Cache-Control: no-store` only on authenticated responses so a probe is not
  defeated. Values are constants, not configuration: a weaker value must not be
  expressible by a deployment that is merely misconfigured.
- **Canonical `scp` parser** (`SessionScopeClaim`) — one place a `scp` claim value
  becomes a `SessionScope`. The documented wire values are lowercase `tenant` /
  `platform`; `SessionScope.Platform.ToString()` is `"Platform"`, and comparing a
  claim against the enum member name silently classified real platform tokens as
  tenant tokens.
- **Committed-secret scanning** (`NoCommittedSecretTests`, architecture project) —
  `AGENTS.md` §4 made a security regression a build failure, so a committed
  credential is now a build failure too. Scans `appsettings*.json`, launch
  settings, `.http`, Docker and compose files, not just compiled assemblies.
  Patterns are specific enough not to fire on ordinary C# (`Password = value` in
  `Enums.cs` is not a credential) because a scan that cries wolf gets switched off
  and then catches nothing. **Verified to fail** on a planted
  `Password=hunter2` connection string; the probe file was removed.
- **API host wiring** (`Program.cs`) — `StocklyDbContext` is now registered, so
  the tenant-filtered data access layer is actually reachable. The connection
  string is resolved once at startup from `ConnectionStrings:StocklyDb` and a
  missing value **aborts the boot** rather than failing on the first request. The
  key name was taken from the committed `.env.example` template, which is
  authoritative; no connection string is in any committed file.

**264 tests pass, 0 fail, 0 skipped** (`dotnet test Stockly.slnx`):

- **229 unit** — was 136; +93 covering the audit mapper and collector, the
  `scp` parser, `HttpTenantContext` (including that a `X-Tenant-Id` header or a
  `scp` header cannot set or escalate anything), the forwarded-IP trust rule, and
  every §12 header asserted as an exact string.
- **35 architecture** — was 32; +3 committed-secret guards.

### Fixed — isolation defects found while implementing the model (2026-10-01)

Each of these was a real defect in the first implementation pass. They are
recorded because the corrections are structural, not cosmetic.

- **`identity.Tenant` carried a `TenantId` it must not have.** It inherited a
  tenant-scoped base, which added a column absent from `DATABASE_DESIGN.md` §4.2
  and — worse — subjected the tenant root to its own tenant query filter, so a
  tenant could fail to appear in its own session. Now derives from
  `SoftDeleteEntity`.
- **No `UNIQUE (Id, TenantId)` existed anywhere.** §5.1 rule 1 requires every
  child to reference its parent as `FOREIGN KEY (ParentId, TenantId)`, but SQL
  Server can only reference a declared key, so no composite key could be emitted
  at all. Applied as one `StocklyDbContext` convention rather than 66 declarations
  that could each be forgotten.
- **`TenantId` was a free-floating GUID.** Most tables had no foreign key to
  `identity.Tenant`, so the database would accept a row claiming a tenant that
  does not exist and the query filter would match it. Now 57 keys, `RESTRICT`.
- **84 cascading deletes.** EF defaults to `CASCADE` for required relationships,
  which included `master.Product` cascading into its own batches — the opposite of
  what the ledger tables exist to guarantee. Now `RESTRICT` everywhere except the
  two assignment keys on `identity.MembershipRoleWarehouse`.
- **Six root keys pointed at the wrong table.** `FK_Product_Tenant`,
  `FK_Role_Tenant`, `FK_Warehouse_Tenant`, `FK_Batch_Tenant`,
  `FK_Supplier_Tenant`, `FK_DocumentSequence_Tenant` and others referenced the
  dependent's own table instead of `identity.Tenant`.
- **`Tenant.LegalName` was `NOT NULL`.** `DATABASE_DESIGN.md` §4.2 specifies
  `nvarchar(300) NULL`.
- **Nullable provenance keys had no foreign key.** `GoodsReceiptLine.BatchId`, the
  typed source ids on `inventory.StockDocument` and `MealTypeId` were unvalidated
  `uniqueidentifier`s, so the database accepted rows pointing at nothing.
- **The blanket cascade rule deleted the tenant.** `MembershipRoleWarehouse`
  declares the tenant-root key like every other tenant-scoped table, so excluding
  it by declaring type alone left `ON DELETE CASCADE` on
  `FK_MembershipRoleWarehouse_Tenant_TenantId`. Deleting a tenant would have
  deleted its role assignments, and the schema no longer matched the documented
  "RESTRICT everywhere except two assignment keys". The rule now also excludes any
  foreign key whose principal is `identity.Tenant`; the generated SQL shows
  exactly 2 cascades.

### Fixed — scope and header defects found while wiring the API (2026-10-01)

Found by the tests written alongside the new code, not by review.

- **`HttpTenantContext` compared `scp` against the wrong string.** It compared the
  claim to `SessionScope.Platform.ToString()`, which is `"Platform"`, while
  `API_CONVENTIONS.md` and `SECURITY_ARCHITECTURE.md` §4.4 define the value as
  lowercase `platform`. Every genuine platform token was therefore classified as a
  **tenant** token: `IsPlatformScope` was false, and the context then read `tid`/`mid`
  from a platform principal, producing an empty result set instead of the
  documented platform behaviour. Both the context and the new authorization handler
  now go through `SessionScopeClaim`, and
  `HttpTenantContextTests.The_platform_scope_matches_the_documented_lowercase_claim`
  pins it.
- **A missing or invalid `scp` bound a tenant.** `IsPlatformScope` was a two-state
  answer, so an unparseable scope collapsed to `false` — meaning "tenant" — and the
  `tid` claim was then honoured. `TESTING_STRATEGY.md` S25 requires such a token to
  be **rejected**. The three outcomes (tenant / platform / invalid) are now kept
  distinct and only a validated tenant scope resolves a tenant, so the failure is
  closed in both directions.
- **`Enum.TryParse` would have accepted `scp="2"` as platform authority.** The
  handler used it during development; numeric text parses to the enum value with
  the matching ordinal. Tokens are never issued with a numeric scope, so honouring
  one would be an escalation. `SessionScopeClaim` accepts only the two documented
  values.
- **`X-Forwarded-For` was trusted unconditionally.** It is attacker-controlled, so
  keying a rate limiter on it hands the caller a free bypass by rotating the header
  per request. Now resolved through `ForwardedIpResolver`, which trusts it only from
  configured proxy hops and takes the first untrusted hop.
- **`StocklyDbContext` was never registered.** The tenant-filtered data-access
  layer existed but was unreachable from the API host, so no endpoint could have
  been isolated correctly.

### Changed — vulnerable transitive dependencies upgraded (2026-10-01)

Warnings are errors in this repository, so these were build failures rather than
advisories. Each was **fixed by upgrading**, never by suppressing `NU1903`.

- `System.Security.Cryptography.Xml` → `10.0.12` (pinned with `PrivateAssets`).
- `Microsoft.OpenApi` → `2.7.5` for CVE-2026-49451 / GHSA-v5pm-xwqc-g5wc (high):
  a circular `$ref` pair in a parsed OpenAPI document recursed until the stack
  overflowed and the process died. Fixed in 2.7.5 on the 2.x line.
- Removed the unused `Openapi` (SwaggerUI) package. `Program.cs` never referenced
  it and it pulled in `RestSharp 103.4.0` (advisory GHSA-9pq7-rcxv-47vq, high) and
  a net4x-only restore.

### Fixed — Phase 1 scope contradiction, §25 is authoritative (2026-09-30)

Found at the start of Phase 1 implementation, before any code was written.
Two ratified documents placed the same deliverable in different phases.

- `PROJECT_BLUEPRINT.md` §25 puts **"all 105 permissions seeded, 11 tenant roles
  seeded by the D1–D5 derivation"** in **Phase 2**.
- `DEVELOPMENT_STATUS.md` §11.2 previously listed the 105-permission seed, the
  plan catalogue, and the 11 tenant roles as **Phase 1** deliverables.

- **Resolved (owner decision, 2026-09-30): `PROJECT_BLUEPRINT.md` §25 governs.**
  Phase 1 does **not** seed the 105 permissions, does **not** seed the plan
  catalogue, does **not** seed the 11 tenant roles, and does **not** execute the
  D1–D5 derivation. RBAC and login remain Phase 2 per §25. §25.1 corroborates
  this ("Identity primitives in 1, RBAC and login in 2").
- Verified before correcting that **no ADR overrides §25**: ADR-0040 defers to it
  explicitly, ADR-0044 fixes the derivation *mechanism* without assigning a
  phase, ADR-0012 fixes the catalogue model without assigning a phase.
- `PROJECT_BLUEPRINT.md` §25 was **not** modified. The status document, which was
  the lower-authority document, was corrected (`DEVELOPMENT_STATUS.md` §11.2,
  new §11.2a recording the contradiction and its resolution).
- **No ADR was created for this.** It resolves an interpretation conflict
  between two ratified documents, not an architecture decision; ADR-0040 and
  ADR-0044 already carry the substance. A new ADR would also invalidate the
  ratified count of **50** ADRs.
- Counts unchanged: 66 entities / 12 schemas · 105 permissions / 13 namespaces ·
  11 + 1 authorities · 147 rules · 27 error codes · 19 workflows · 8 phases ·
  50 ADRs.

### Changed — FINAL OWNER RATIFICATION, blueprint v1.1 (2026-09-30)

**STOCKLY BLUEPRINT v1.1 — RATIFIED / READY FOR PHASE 1.** This is a documentation
governance transition only. **No implementation code was introduced** — the
repository still contains documentation, nothing else.

- **ADR-0001 → ADR-0044 promoted `Proposed` → `Accepted`** by the product owner.
  All 50 ADRs are now `Accepted`; **0 remain `Proposed`**. No decision substance
  was altered: nothing was created, merged, split, renumbered or rewritten, and
  ADR-0045 → ADR-0050 were already `Accepted` and are untouched.
- **Security limitations acknowledged.** SEC-KNW-01 → SEC-KNW-09 were explicitly
  accepted by the owner as **known V1 limitations**. All nine remain registered;
  none was removed, reworded, downgraded or fixed. Acknowledging them is not a
  remediation.
- **Status synchronized** in `PROJECT_BLUEPRINT.md` (status line, header, §23.1
  gate note, §27 approval record), `DEVELOPMENT_STATUS.md` (§1, §2, §6, §11,
  §12, §19, ISS-01), `SECURITY_ARCHITECTURE.md` (§15 acknowledgement),
  `ARCHITECTURE.md` (§12), `DECISIONS.md` (index, header, status summary),
  `README.md`, and `AGENTS.md`.
- **Ratification resolved no TBD.** All 16 unresolved items stay unresolved with
  their existing gates — Phase 2: TBD-01/03/04/05/11 · Phase 3: TBD-06 ·
  Phase 4: TBD-08/09 · Phase 5: TBD-07 · Phase 6: TBD-10 · Phase 7: TBD-12/15 ·
  Phase 8: TBD-14 · deferred, no phase: TBD-13/23/26. The register is unchanged
  at **26 = 5 decided + 16 unresolved + 4 design + 1 assumption**.
- **Phase 1: authorised, ready to begin, not started.** Zero decision blockers.
  Nothing is implemented: no code, no migrations, no tests, no frontend, no CI.
  **No test has been run**, and no production-readiness claim is made.

### Fixed

- **Blueprint ratification gate — self-contradiction removed.** The status header
  and §27 both stated that Phase 1 must not begin until the owner has "answered
  or explicitly deferred the 16 open items" in §23.1. That wording contradicted
  the same documents' own statement that **no missing decision blocks Phase 1**
  and that every remaining item is phase-gated or explicitly deferred. The
  requirement is **removed from both locations**. §27 now requires exactly three
  things: ratification of blueprint v1.1 and ADR-0001 – ADR-0044, acknowledgement
  of SEC-KNW-01 – SEC-KNW-09 as accepted for V1, and confirmation that no Phase 1
  decision blocker remains. Both locations now state explicitly that there is
  deliberately **no** precondition to answer or defer the 16 open items before
  Phase 1.
- **Phase 1 decision blockers: none.** The five genuine Phase 1 gates
  (TBD-19, 21, 22, 24, 25) remain decided. **Owner ratification remains the only
  authorization gate.**

### Unchanged by this correction

- **The remaining 16 TBDs are untouched** and keep their existing phase gates —
  Phase 2: `TBD-01`, `TBD-03`, `TBD-04`, `TBD-05`, `TBD-11` · Phase 3: `TBD-06` ·
  Phase 4: `TBD-08`, `TBD-09` · Phase 5: `TBD-07` · Phase 6: `TBD-10` ·
  Phase 7: `TBD-12`, `TBD-15` · Phase 8: `TBD-14` · deferred, no phase scheduled:
  `TBD-13`, `TBD-23`, `TBD-26`. None was silently decided and none is now
  required before Phase 1.
- **No owner decision was added or changed.** **No ADR was added, edited or
  promoted**: ADR-0001 – ADR-0044 stay `Proposed`, ADR-0045 – ADR-0050 stay
  `Accepted`. No entity, permission, role, business rule, error code, workflow,
  phase, or security decision was touched. Counts remain 66 entities /
  12 schemas · 105 permissions / 13 namespaces · 11 + 1 authorities ·
  147 rules · 27 error codes · 19 workflows · 8 phases · 50 ADRs ·
  26 register items (5 decided + 16 unresolved + 4 design + 1 assumption).
- **No implementation work was performed.** Still no application code, no
  migrations, no tests, no frontend, no CI/infra. Documentation only, and not
  committed.

---

## [0.3.0] — 2026-09-30 — Phase 0 · Owner decisions recorded, blueprint v1.1

Documentation-only. **Still no application code, no migrations, no tests.**
The owner decided the five questions that were blocking the Phase 1 foundations.
**No open decision now blocks Phase 1**; the remaining gate is owner ratification
of the blueprint and ADR-0001 – ADR-0044.

### Added — decision records for the 2026-09-30 owner decisions

| ADR | Status | Records |
|---|---|---|
| **ADR-0045** | `Accepted` | TBD-19 — warehouse sub-locations. V1 has none; V1 stock is warehouse-level. V1.1 **descriptive** sub-locations only, owning no stock. Location-level **stock ownership is a V2 redesign** |
| **ADR-0046** | `Accepted` | TBD-21 — currency. One currency per Tenant. **No FX table, no rate, no conversion logic.** Currency columns are informational only |
| **ADR-0047** | `Accepted` | TBD-22 — transfers. **Instantaneous.** No in-transit warehouse and no transfer state machine. Source decrement + destination increment in one atomic transaction, both warehouses of the same tenant, fully audited |
| **ADR-0048** | `Accepted` | TBD-24 — legal entities. One `Tenant` is one legal/business entity. No parent-tenant hierarchy, no entity subdivision; multi-entity is a future non-goal |
| **ADR-0049** | `Accepted` | TBD-25 — scale target. ~50 warehouses, ~100 k products, ~10 M inventory ledger rows, ~200 concurrent users per tenant. **Engineering and capacity targets, not subscription or commercial limits** |
| **ADR-0050** | `Accepted` | The **creation and deferral of TBD-26**. Stock reservation semantics (hard vs soft, release timing, lifecycle) are separated out of the overloaded TBD-10 and remain **unresolved future scope**, classified as a V2 candidate |

`DECISIONS.md` now holds **50** ADRs: `ADR-0001` – `ADR-0044` `Proposed`,
`ADR-0045` – `ADR-0050` `Accepted`.

### Changed

- **Blueprint status → `v1.1`:** `STOCKLY BLUEPRINT v1.1 — OWNER DECISIONS RECORDED
  (5 DECIDED, 16 OPEN); AWAITING FULL RATIFICATION`. It is no longer "blocked by
  owner decisions" — it is blocked only on ratification.
- **§23 register restructured.** 26 tracked items = 5 decided + 16 unresolved +
  4 disposed design decisions + 1 safe assumption. §23.1 now separates *Phase 1
  blockers* (**none**), *phase-gated decisions* (13, each tied to Phase 2–8),
  and *future/deferred decisions* (3: TBD-13, TBD-23, TBD-26). §23.4 records the
  five decisions with their effective scope; §23.5 records the reservation
  deferral.
- **TBD-10 split.** It now covers **only** recipe serving/yield semantics, the
  consumption-planning basis and the related pack-size rounding. Stock
  reservations are the separate **TBD-26**. Every prior citation of "TBD-10" for
  reservations was corrected.
- **Phase 5 title corrected:** `Batches + Expiry + Waste + Transfers + Stocktake +
  **Locations** + Barcode/QR` → `… + Stocktake + Barcode/QR`. Locations were never
  a Phase 5 deliverable. §25.0 now states why.
- **§24 tiering corrected.** "warehouse sub-locations" moved from a bare V1.1 line
  to "descriptive sub-locations (ADR-0045)", reservations moved from an
  unqualified "V2" entry to an explicit **V2 candidate pending TBD-26**, and
  location-level stock was added to V2 as a redesign.
- **§2.1** now records that a tenant is one legal entity. **§11.2** records the
  instantaneous transfer as decided. **§14** records the single-currency rule.
  **§18** and the concurrency section record the scale target as an engineering
  requirement, not a subscription limit.
- **ADR-0040 amended** in `DECISIONS.md` to match the Phase 5 title, with a
  pointer to ADR-0045.

### Fixed — contradictions found while propagating the decisions

- **False "owner decision" dispositions corrected.** `DATABASE_DESIGN.md`
  DB-TBD-06 and DB-TBD-09 and eight `BR-TBD-*` rows described TBD-14, TBD-08,
  TBD-09, TBD-01, TBD-06, TBD-07, TBD-12 and TBD-15 as settled "owner decisions"
  or "provisional" values. Those items are **still open** and are now labelled
  with their real phase gate. A document that claims an owner decision that was
  never taken is a defect.
- **`WORKFLOWS.md` §7** called the transfer in-transit model an open TBD with an
  assumption; it is now a decision, with the reason the in-transit model was
  rejected and the requirement that both legs remain audited.
- **`ROLES_PERMISSIONS.md` §4** called the second authorization axis "location".
  After ADR-0045 the only warehouse-level scope axis is `WarehouseId`; the
  separation is unchanged, the vocabulary was wrong.
- **`ROLES_PERMISSIONS.md` RP-TBD-04** previously said a future warehouse
  hierarchy would be "a filter inside `MembershipWarehouse`". That would have made
  a location a display-level filter on an already-authorized warehouse — a
  privilege-escalation shape. It now states that location-scoped stock must be a
  **new assignment axis with its own authorization check**.
- **`ROLES_PERMISSIONS.md` / `SECURITY_ARCHITECTURE.md`** tied the reservation
  permission to "feature is V2". It is now tied to TBD-26/ADR-0050 as
  **unresolved**, which is the actual state.
- **`ARCHITECTURE.md` §12** referred to "43 records" in `DECISIONS.md` (already
  stale at 44). Now 50, with the six accepted ones named.
- **`DEVELOPMENT_STATUS.md` §16** said "all 10 `DB-TBD` items". There are now
  12, five of which are decided decisions.
- **`DEVELOPMENT_STATUS.md` §20** listed all 20 open decisions as blocking. §20
  is now split into §20.1 phase-gated, §20.2 deferred, §20.3 actually blocking,
  and §20.4 no longer blocked.

### Added — testable obligations

- **`TESTING_STRATEGY.md` §3.2 absence tests.** Each owner decision is a
  *prohibition*, and a prohibition cannot be verified by reading a list of what
  exists. Added schema/migration assertions that no location key exists on stock
  tables, no FX column exists anywhere, no in-transit state exists, no legal-entity
  layer exists, and no `StockReservation` exists with the permission granted to
  zero roles. A failure is a build failure.
- **`TESTING_STRATEGY.md` §3.5** transfer-failure cases: injected failure between
  the two legs leaves both balances unchanged and no orphan ledger row;
  cross-tenant transfer is unrepresentable.
- **`DEVELOPMENT_STATUS.md` §13** a table of the security effect of each decision
  — all five reduce attack surface rather than adding to it.

### Counts — unchanged, with one deliberate exception

| Count | Before | After |
|---|---|---|
| Architecture decision records | 44 | **50** |
| Everything else (66 entities, 12 schemas, 105 permissions, 13 namespaces, 11+1 roles, 147 rules, 27 error codes, 19 workflows, 33 security classes, 9 limitations, 8 phases) | — | **unchanged** |

The ADR count is the one number that moved, because blueprint §27 requires a
decision record for every decision and six were recorded. No entity, permission,
role, business rule, error code, workflow or phase was added, removed or
renumbered — each decision **forbids** a V1 feature rather than introducing one.
The business-rule count stayed at 147 because each decision was folded into an
existing rule (`BR-UNI-02`, `BR-UNI-09`, `BR-ORG-07`, `BR-RPT-06`, `BR-INV-04`,
`BR-INV-10`, `BR-INV-11`) instead of adding a new rule ID.

### Process note

During this pass `docs/PROJECT_BLUEPRINT.md` was corrupted by a PowerShell
`Get-Content -Raw | Set-Content -Encoding UTF8` round-trip, which destroyed its
UTF-8 encoding (the Arabic product name and box-drawing characters). It was
restored from git, losing the in-progress blueprint edits, which were then
re-applied with a UTF-8-safe write. The lesson is recorded in `AGENTS.md` §10.
The nested repository `H:\Stockly\Stockly` was not touched.

---

## [0.2.0] — 2026-09-27 — Phase 0 · Blueprint v1.0 finalisation

Documentation-only. **Still no application code, no migrations, no tests.**
The blueprint reached a final, internally consistent draft and is now
**BLOCKED BY OWNER DECISIONS** rather than "unfinished".

### Fixed — defects found while reconciling the blueprint against the design docs

- **ISS-04 resolved by owner decision.** `.gitignore` now ignores the empty
  nested repository `H:\Stockly\Stockly` (commit `fe2eb7a`), which contradicted
  the then-current rule in `AGENTS.md` §10 and ISS-04. The owner confirmed the
  ignore was intentional, so the rule was rewritten to "do not delete or commit
  it" instead of "do not ignore it", and ISS-04 was closed in
  `DEVELOPMENT_STATUS.md` §7 and §12 and removed from the pending-work and
  blocked-items lists. The repository itself is untouched and was never tracked.

- **`inventory.InventoryLot` unique index was dropped from the indexing table.**
  The entity definition declared two filtered unique indexes, but
  `DATABASE_DESIGN.md` §7 listed only the `BatchId IS NOT NULL` variant. An
  implementer working from §7 would have lost the
  `(TenantId, WarehouseId, ProductId) WHERE BatchId IS NULL` guarantee, which is
  the single row the guarded `UPDATE` in §8.1 targets for untracked products —
  and SQL Server's NULL-distinct semantics would then have permitted duplicate
  untracked lots. Both indexes are now named (`UX_InventoryLot_NonBatched`,
  `UX_InventoryLot_Batch`, `UX_StockBalance`) and §4.5 and §7 agree.
- **`CHANGELOG.md` cited a `LEGACY-PHASE-MAP` that exists nowhere** and put the
  superseded plan at "legacy 18" phases, while ADR-0040 and the blueprint both
  record **eleven**. Corrected to 11, pointing at `PROJECT_BLUEPRINT.md` §25.1.
- **ADR count was 43 in three places** after ADR-0044 was added. Corrected to
  **44** in `docs/CHANGELOG.md` and `AGENTS.md`. The `[0.1.0]` entry is a
  historical record and still says 31, which is correct for that release.
- **`AGENTS.md` pointed at `DEVELOPMENT_STATUS.md` §2 for the final counts**; the
  reconstructed file carries them in §5. Reference fixed.

### Changed — status

- `docs/PROJECT_BLUEPRINT.md` status is now exactly
  `STOCKLY BLUEPRINT v1.0 — BLOCKED BY OWNER DECISIONS`. §23 is a decision
  register with a disposition for every item; §27 states the approval gate.
- The 25 §23 items are reclassified: **20 owner decisions, 4 design decisions,
  1 safe assumption**. Design and assumption items are no longer counted as
  blocking.
- Every secondary open-question register now carries a disposition instead of
  being an open question: 10 `DB-TBD`, 6 `RP-TBD`, 7 `API-TBD`, 14 `BR-TBD`,
  6 `TEST-TBD`.
- All 44 ADRs remain **Proposed** and take effect only on ratification.

### Changed — counts (supersede the 0.1.0 figures)

| Quantity | 0.1.0 | 0.2.0 |
|---|---|---|
| Entities | 66 across 11 schemas | **66 across 12 schemas** (`ops` split out) |
| Permissions | 107 codes / 10 namespaces | **105 codes / 13 namespaces** (one duplicate removed) |
| System roles | 12 | **11 tenant + 1 platform operator authority** |
| Business error codes | 25 | **27** (26 emitted + reserved `account_locked`) |
| ADRs | 31 | **44** |
| Delivery phases | legacy 11 | **8** (+ the explicit legacy mapping in `PROJECT_BLUEPRINT.md` §25.1) |
| Workflow sections | 17 | **19** |
| Gating security test classes | 24 | **33** (S1–S33) |
| Security limitations | 7 | **9** (SEC-KNW-01…09) |

### Security — design changes

- **Platform authority is a separate token scope.** Access tokens carry a
  required `scp` claim (`tenant` or `platform`) validated against the live
  `AuthSession`. A platform token is rejected by every tenant route and a tenant
  token by every `/platform/**` route, with no scope accumulation and no
  cross-scope switch (ADR-0034). `AuthSession` gained `SessionScope`, a
  nullable `TenantId`, and nullable `MembershipId`/`DeviceId`.
- **Terminal addressing changed from a guessable code to a rotatable one.**
  `Device.DeviceCode` (human label) is no longer an authentication key. A
  server-generated 128-bit base32url `Device.EnrollmentCode` addresses all
  terminal routes; an unknown code, a revoked device, and a rotated-away code
  are indistinguishable `404`s (ADR-0035).
- **Throttle identifiers are HMAC-hashed.** `LoginAttempt` stores
  `IdentifierHash = HMAC-SHA256(identifier, serverPepper)` instead of a plain
  hash, because a plain hash of an email address is reversible by dictionary
  attack. Startup **fails closed** when the pepper is missing (ADR-0036).
- **`404` instead of `403`** for a `warehouseId` the caller is not assigned to,
  including on list queries, so an invalid parameter cannot probe another
  warehouse.
- Password/PIN hashing is now specified as **Argon2id** (m=64 MiB, t=3, p=2) with
  a documented PBKDF2 fallback, replacing "Argon2id or BCrypt".
- `scp`, tenant impersonation, and platform-flag escalation were added to the
  privilege-escalation table, and each new identity rule has a named regression
  test.
- Two new limitations were registered honestly rather than absorbed:
  **SEC-KNW-08** (the terminal identities endpoint discloses a staffing list to
  the holder of an enrollment code) and **SEC-KNW-09** (`IsPlatformAdmin` is
  all-or-nothing in V1).

### Database

- Every tenant-scoped unique index leads with `TenantId`, and the global-unique
  exceptions are enumerated instead of implicit (ADR-0032).
- Nullable unique keys get one filtered index per populated variant so SQL
  Server never limits a nullable key to a single `NULL` row (ADR-0041).
- `CHECK` constraints are restricted to **absolute** invariants. Bounds the
  product allows to exceed (over-receipt, over-consumption) moved to guarded
  `UPDATE`s plus an audit record, removing a contradiction where the schema
  forbade a documented business rule (ADR-0039).
- Provenance is modelled with typed nullable foreign keys instead of a
  polymorphic `(SourceType, SourceId)` pair (ADR-0038).
- Append-only tables carry no `RowVersion` and no `IsCurrent`; a mutable
  "current" flag was removed from event food-cost snapshots (ADR-0037).
- Barcode ownership moved from `Product` to `master.ProductBarcode`, which
  removes the single-barcode limitation and gives per-symbology validation
  (ADR-0033, ADR-0043).
- Global search settled on SQL Server full-text (`Arabic_CI_AS`) plus a trigram
  fallback, with an explicit note that a filtered index cannot call
  `SYSUTCDATETIME()` (ADR-0042).
- `StockBalance`/`InventoryLot` rebuild scope made explicitly partial, with
  `IncomingQuantity` sourced from open purchase orders.
- FEFO corrected to use persisted `ExpirySortKey`, then `FirstReceivedAtUtc`,
  then `Id`.

### Fixed

- `Product.Barcode` is gone; `ProductBarcode` is the single barcode source.
- `Event.CostSnapshotId` removed; the current snapshot is the newest
  `CalculatedAtUtc`.
- `Supplier.Rating` and `StockDocumentLine.BalanceAfter` removed as
  unjustifiable columns.
- `AuthSession` platform fields, `LoginAttempt` HMAC/enrollment fields, and
  `Device.EnrollmentCode` added; a broken composite unique constraint on
  `PinCredential` fixed.
- `OPS`-schema naming, the `TBD-14` retention reference, the
  `DEVELOPMENT_STATUS.md §18` cross-reference, and several stale role/permission
  counts corrected.

### Tests

- No test exists. Nine security test classes were added to the *specification*
  (S25–S33) covering token scope, platform login, terminal enrollment, terminal
  disclosure, identifier hashing, warehouse probing, unique-index shape,
  append-only enforcement, and `CHECK` scope. They are obligations, not results.
- `Stockly.MigrationTests` added to the proposed solution structure: schema
  guarantees such as "every unique index leads with `TenantId`" can only be
  proven by SQL Server, not by code review.

### Environment / repository

- Git repository was initialised upstream: commit `9121044` "Initial commit"
  (this entry corrects the 0.1.0 claim that Git was not initialised).
- An **empty nested Git repository** exists at `H:\Stockly\Stockly`. It was
  recorded as ISS-04 and left untouched: it is either an accidental nested
  repository or an intended application root, and that is an owner decision, not
  a cleanup task.
- **ISS-04 is RESOLVED** (owner decision, 2026-09-27): the nested repository is
  ignored through `.gitignore` (commit `fe2eb7a`). It remains on disk, untracked
  and empty — nothing was deleted and nothing was ever committed. `git status` is
  clean again. It must not be deleted or committed without a new owner decision.

### Breaking changes

None to code (none exists). Breaking **to the design contract**, all recorded in
`DECISIONS.md`:

- Terminal routes no longer accept `deviceCode`/`deviceId`.
- `platform.*` routes require a platform-scoped token.
- `pin_change_required` moved from `403` to `428`.
- `account_locked` is reserved and not emitted by V1 endpoints.
- `Product.Barcode` removed in favour of `ProductBarcode`.

### Known gaps

- 20 owner decisions unanswered (ISS-02); blueprint unratified (ISS-01).
- Licence not chosen (ISS-05); all ADRs still `Proposed` (ISS-06).
- Docker unavailable, so DB-backed suites remain unrunnable locally (ISS-03).

---

## [0.1.0] — 2026-09-27 — Phase 0 Bootstrap

> **Superseded in part.** The figures below describe the documents **as they were
> written on 2026-09-27 during bootstrap**. For the current counts, decisions and
> status see `[0.2.0]` above and `docs/DEVELOPMENT_STATUS.md`. This entry is kept
> as the historical record and is not retro-edited.

Repository bootstrap. **No application code was created.** The repository was
inspected and found empty; the Stockly project-memory documentation was created
from scratch.

### Added — documentation (project memory)

- `docs/PROJECT_BLUEPRINT.md` — master specification covering all 24 required
  blueprint areas: roles, permissions, modules, entities, relationships, tenant
  and warehouse scope, device/session scope, business rules, workflows,
  authentication, PIN, session, device, audit, subscription, entitlement, API
  boundaries, validation, concurrency, files, localization, and V1/V1.1/V2/V3
  boundaries. Includes 25 explicitly unresolved `TBD` items.
- `docs/ARCHITECTURE.md` — modular monolith rationale, solution structure,
  dependency direction, module boundaries, three-layer tenant-isolation
  defence, authorization evaluation pipeline, authentication architecture,
  error handling/observability, outbox-based background processing, frontend
  and deployment direction.
- `docs/SECURITY_ARCHITECTURE.md` — STRIDE-derived threat model, trust
  boundaries, password and PIN controls, token model, authorization chain,
  rate limiting, cryptography and secrets, input validation, inventory
  concurrency security, audit non-repudiation, session/device security,
  security headers, a 7-item known-limitation register, and a per-phase review
  checklist.
- `docs/DATABASE_DESIGN.md` — schema layout, column conventions, and
  column-level design for 66 entities across 11 schemas, including composite
  tenant foreign keys, filtered unique indexes, `rowversion`, `CHECK`
  constraints, indexing strategy, canonical transaction patterns, idempotency,
  document numbering, retention, and migration policy.
- `docs/ROLES_PERMISSIONS.md` — 107 permission codes in 10 namespaces, 12 system
  roles with authority levels, role-to-permission matrix, role/warehouse scope
  model with worked examples, and five privilege-escalation controls.
- `docs/BUSINESS_RULES.md` — 147 rules with stable identifiers across identity,
  PIN, organization, inventory, purchasing, events, food/costing, subscription,
  reporting, notification, and audit domains, plus a validation catalogue and
  25 stable business error codes.
- `docs/WORKFLOWS.md` — 17 end-to-end workflows with preconditions,
  permissions, transaction steps, invariants, and failure handling.
- `docs/API_CONVENTIONS.md` — endpoint map for all modules, headers, keyset
  pagination, filtering, idempotency, optimistic concurrency via
  ETag/`If-Match`, RFC 9457 error format, status-code policy, rate limits, and
  OpenAPI policy.
- `docs/TESTING_STRATEGY.md` — six test projects, unit/architecture/integration/
  security/concurrency/integrity taxonomy, 24 gating security test classes
  (S1 to S24), concurrency scenarios, database integrity assertions, test data
  strategy, CI gates, coverage policy, definition of done, and 13 manual
  scenarios.
- `docs/DEVELOPMENT_STATUS.md` — current state, repository verification
  results, detected environment tooling, completed/pending work, known issues,
  known security issues, explicit "no test has been run" statement, security
  assumptions, next action, and blocked items.
- `docs/DECISIONS.md` — 31 architecture decision records (ADR-0001 to ADR-0031)
  with decision, reason, alternatives, consequences, and affected components,
  plus the ADR change procedure.
- `docs/CHANGELOG.md` — this file.
- `README.md` — repository overview, reading order for the project memory,
  permanent product rules, and technology direction.
- `AGENTS.md` — binding operating rules for AI coding agents: repository is the
  source of truth, no work without approval, documentation update obligations,
  security rules, honest reporting, git safety, no in-product AI, and phase
  completion criteria.
- `.gitignore` — secret, build artifact, IDE, database, log, and container
  exclusions.
- `.env.example` — non-secret configuration template.

### Architecture (decisions recorded, not yet implemented)

- Modular Monolith with schema-per-module naming (ADR-0001, ADR-0016).
- Global `AppUser` identity + `TenantMembership` model (ADR-0003).
- Tenant context derived exclusively from the authenticated session
  (ADR-0004).
- Warehouse authorization as data, independent of roles (ADR-0005).
- Devices bound to exactly one warehouse; sessions bind user + device
  (ADR-0006).
- RS256 short-lived JWT plus rotating opaque refresh tokens with family reuse
  detection (ADR-0007).
- PIN authentication with memory-hard hashing, three-axis throttling, and
  per-membership version invalidation (ADR-0008).
- Immutable stock ledger with balance projections, atomic guarded updates,
  `rowversion`, and `CHECK` constraints (ADR-0009, ADR-0010).
- Transactional outbox for notifications/reports; synchronous security audit
  (ADR-0011).
- Server-side permission resolution with immediate cache invalidation, so
  revocation is not delayed by token lifetime (ADR-0013).
- Append-only audit with restricted database grants (ADR-0015).
- Moving weighted average valuation, FEFO allocation, `decimal(18,4)`, UTC
  storage (ADR-0017, ADR-0018, ADR-0024, ADR-0029).
- No AI assistant inside the product (ADR-0023).
- No offline writes in V1 (ADR-0021).

### Security

- Recorded 7 design-time security limitations (SEC-KNW-01 to SEC-KNW-07) rather
  than claiming none exist.
- Defined the mandatory authorization chain: user, tenant, membership,
  entitlement, permission, warehouse assignment, resource scope.
- Defined cross-tenant existence protection (`404` instead of `403`).
- Defined mass-assignment defence (unknown-field rejection).
- Defined idempotency-key protection against duplicate stock receipts.
- Defined secret-redaction requirements for logs and audit payloads.

### Database

- Designed 66 entities across `platform`, `identity`, `ops`, `master`,
  `inventory`, `purchasing`, `events`, `food`, `notifications`, `files`,
  `extensibility`, and `audit` schemas.
- **No migrations were created.** No database exists.

### Tests

- **No tests were written or executed.** No code exists to test.
- Test strategy, suites, and 24 gating security test classes are specified in
  `docs/TESTING_STRATEGY.md` for implementation in Phase 1 onward.

### Environment (verified, not assumed)

| Tool | Version | Status |
|---|---|---|
| .NET SDK | 10.0.201 | Available |
| Node.js | v24.14.0 | Available |
| Git | 2.53.0.windows.1 | Available |
| Docker | — | **Not available** |

Recorded consequence: integration, security, concurrency, and migration tests
require a real SQL Server and therefore **cannot be executed on this workstation
until Docker or a local SQL Server is installed**. This is tracked as ISS-03 and
TEST-TBD-06.

### Breaking changes

None (no released code).

### Known gaps

- Blueprint not yet reviewed or approved by the product owner (ISS-01).
- 25 `TBD` requirements unanswered (ISS-02).
- Git repository not initialised (ISS-04).
- Licence not chosen (ISS-05).

---

[Unreleased]: https://example.invalid/stockly/compare/v0.2.0...HEAD
[0.2.0]: https://example.invalid/stockly/compare/v0.1.0...v0.2.0
[0.1.0]: https://example.invalid/stockly/releases/tag/v0.1.0
[0.1.0]: https://example.invalid/stockly/releases/tag/v0.1.0
