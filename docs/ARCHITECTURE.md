# STOCKLY — ARCHITECTURE

| Field | Value |
|---|---|
| Document | `ARCHITECTURE.md` |
| Version | 1.0.0 |
| Status | FINAL DRAFT — consistent with `PROJECT_BLUEPRINT.md` v1.0 (8 delivery phases) |
| Last updated | 2026-09-27 |
| Related | `PROJECT_BLUEPRINT.md`, `SECURITY_ARCHITECTURE.md`, `DATABASE_DESIGN.md`, `DECISIONS.md` |

---

## 1. Architecture style: Modular Monolith

Stockly is a **modular monolith**: a single deployable ASP.NET Core application
whose code is organised into strongly separated business modules with explicit
boundaries. Modules communicate through in-process application service
interfaces, never by reaching into each other's tables.

Why not microservices (ADR-0001):

- One team, one deployment target for the first customer.
- Transactional consistency is a hard requirement for inventory; a monolith
  gives it for free inside a single SQL Server transaction.
- Operational complexity (service discovery, distributed tracing, network
  failure modes, eventual consistency) is unjustified at this scale.
- Module boundaries are enforced in code now, so extraction is possible later
  **if** a real scaling need appears. The decision is reversible at the module
  level, not at the whole-system level.

Enforcement of module boundaries:

- Each module lives in its own folder tree under `Application/Modules/<Module>`
  and `Infrastructure/Modules/<Module>`.
- A module's entities are `internal` to its namespace by convention; other
  modules only see DTOs and interfaces from `Application/Modules/<Module>/Abstractions`.
- Cross-module read models are served through explicit query interfaces, not
  navigation properties across aggregate roots.
- An architecture test asserts that `Application` does not reference
  `Infrastructure`, and that modules do not reference each other's internal types.

---

## 2. Solution structure

```text
Stockly.sln
├── src/
│   ├── Stockly.Domain/            # Entities, value objects, enums, domain rules. No EF, no ASP.NET.
│   ├── Stockly.Application/       # Use cases, DTOs, validators, abstractions, authorization policies.
│   │   └── Modules/
│   │        Platform/ Identity/ Organization/ MasterData/ Inventory/
│   │        Purchasing/ Events/ Food/ Reporting/ Notifications/ Audit/
│   ├── Stockly.Infrastructure/    # EF Core DbContext, migrations, repositories, file store, outbox, email/push.
│   └── Stockly.Api/               # ASP.NET Core host: controllers/endpoints, middleware, auth, DI.
├── tests/
│   ├── Stockly.UnitTests/
│   ├── Stockly.IntegrationTests/     # requires a real SQL Server
│   ├── Stockly.SecurityTests/
│   ├── Stockly.ConcurrencyTests/     # requires a real SQL Server
│   ├── Stockly.ArchitectureTests/    # no database
│   └── Stockly.MigrationTests/       # applies every migration to an empty DB and
│                                     # asserts the 66 tables, the unique/index
│                                     # catalogue, and the CHECK constraints
├── client/                        # React + TypeScript + Vite PWA (delivered in Phase 8)
├── infra/                         # Docker, Nginx, deployment scripts, CI/CD (built in Phase 1)
└── docs/                          # This project memory
```

> **`Stockly.MigrationTests` added.** The previous structure had no project that
> applies migrations to a real database, yet the schema carries guarantees that
> only a real engine can verify: that every unique index leads with `TenantId`
> (ADR-0032), that each nullable unique key has its `IS NULL` filtered variant
> (ADR-0041), and that each absolute invariant has its `CHECK` (ADR-0039). Those
> assertions belong in a test that talks to SQL Server, not in a code review.

Dependency direction (strictly one way):

```text
Api ──► Application ──► Domain
 │            ▲
 └──► Infrastructure ──► Application, Domain
```

- `Domain` references nothing.
- `Application` references `Domain` only.
- `Infrastructure` references `Application` + `Domain` (implements ports).
- `Api` references `Application` + `Infrastructure` (composition root only).

---

## 3. Layering inside a module (vertical slices)

Each module follows the same shape:

```text
Application/Modules/Inventory/
├── Abstractions/          # Interfaces other modules may use
├── Dtos/                  # Request/response contracts owned by this module
├── UseCases/              # One class per use case (command or query)
│   ├── PostStockOutCommand.cs
│   ├── PostStockOutHandler.cs
│   ├── PostStockOutValidator.cs
│   └── ...
├── Policies/              # Authorization policy definitions for this module
└── Mapping/               # Explicit mapping (no silent automapping of security fields)
```

Use cases are explicit classes with a `Handle`/`Execute` method. We deliberately
avoid heavy generic "repository over everything" abstractions: persistence
access is purposeful and readable, which matters for security review.

**CQRS-lite**, not full CQRS: commands and queries are separated for clarity and
authorization, but both read/write the same SQL Server database. No event
sourcing. No separate read database in V1.

---

## 4. Runtime topology (V1)

```text
                    ┌──────────────────────────────┐
   Browser / PWA ───►│  Nginx (TLS termination)     │
   Shared terminal   │  - static PWA assets          │
                    │  - /api  → reverse proxy      │
                    │  - security headers           │
                    │  - rate limit (edge layer)    │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌──────────────────────────────┐
                    │  Stockly.Api (ASP.NET Core)  │
                    │  - authentication            │
                    │  - authorization             │
                    │  - rate limiting              │
                    │  - use cases                 │
                    │  - audit                     │
                    └───────┬──────────────┬────────┘
                            │              │
                            ▼              ▼
                 ┌────────────────┐  ┌──────────────────┐
                 │ SQL Server     │  │ File store       │
                 │ (single DB)    │  │ (volume / S3)    │
                 └────────────────┘  └──────────────────┘
                            ▲
                            │
                 ┌──────────┴─────────┐
                 │ Outbox worker      │ (same process in V1,
                 │ notifications,     │  hosted service)
                 │ push, audit fanout │
                 └────────────────────┘
```

- **Single API instance** in V1. The design is stateless (no in-memory session
  state), so horizontal scaling is a deployment change, not a rewrite.
- The outbox worker runs as an `IHostedService` in the same process in V1 and
  can be split into a separate container later without code changes.
- SQL Server runs in Docker for development and as a managed/containerised
  instance in production. *(TBD: hosting target.)*

---

## 5. Data access

| Concern | Decision |
|---|---|
| ORM | EF Core (SQL Server provider) |
| DbContext strategy | **Single `StocklyDbContext`**, schema-per-module table naming (ADR-0016) |
| Schemas | `platform`, `identity`, `master`, `inventory`, `purchasing`, `events`, `food`, `notifications`, `files`, `extensibility`, **`ops`**, `audit` — 12 schemas, matching `DATABASE_DESIGN.md` §2. `ops` holds the two infrastructure tables (`OutboxMessage`, `DocumentSequence`); it was previously listed in no schema map, which left those two tables homeless |
| Migrations | EF Core migrations, one migrations assembly, reviewed before applying |
| Global query filters | Tenant filter applied centrally; **supplemented** by explicit predicates in security-sensitive queries |
| Change tracking | AsNoTracking for reads by default; tracking only for the aggregate being modified |
| Raw SQL | Allowed only for guarded balance updates, reporting aggregations, and bulk operations — always parameterised |
| Transactions | Explicit per use case; never rely on implicit save-per-call semantics for multi-table writes |
| Timeouts | Command timeout 30s default; reporting queries 120s with a dedicated read path |

### 5.1 Defence in depth for tenant isolation

Relying on a single global query filter is considered insufficient. Stockly uses
**three independent layers**:

1. **Session-derived context** — the tenant and warehouse come from validated
   token/session claims, never from the request.
2. **Explicit predicates** — every tenant-scoped query in a use case includes
   `TenantId` and, where relevant, `WarehouseId`. Code review checklist item.
3. **Database-level composite keys** — tenant-scoped FKs are composite on
   `(Id, TenantId)` so a cross-tenant reference is physically impossible, and
   `TenantId` leads every unique index.

A security test suite attempts cross-tenant reads/writes for every module and
must fail if any of the three layers is removed.

---

## 6. Authorization architecture

### 6.1 Model

```text
Permission  (global code, e.g. "inventory.stock.out")
    ▲
Role        (tenant-scoped bundle of permissions)
    ▲
MembershipRole  (membership ↔ role, optionally scoped to a warehouse or a set of warehouses)
    ▲
TenantMembership (user ↔ tenant)
    ▲
AppUser
```

Warehouse authorization is a **separate axis**:

```text
MembershipWarehouse (membership ↔ warehouse)  == the "WHERE"
```

### 6.2 Evaluation per request

```text
0. Scope       → does the token scope match the route? A `platform` token is
                  rejected outside /platform/**, a `tenant` token inside it
                  (ADR-0034)
1. Authenticate  → is the token valid, signature/issuer/audience correct, not expired?
2. Session check → is the AuthSession still Active (not revoked/expired/idle)?
3. Membership    → is TenantMembership Active for the token's tenant?
                   (skipped for a Platform session, which has no membership)
4. Entitlement   → does the tenant's subscription allow this module/operation?
5. Permission    → does the membership hold the required permission code?
                   (for a Platform session: does IsPlatformAdmin hold?)
6. Warehouse     → if the operation is warehouse-scoped, is the target warehouse
                   within MembershipWarehouse for this membership?
7. Resource      → does the loaded resource belong to the same tenant AND an
                   authorized warehouse? (checked in the same query, never
                   load-then-check)
```

Failing at step 5 or 6 for a resource the caller could not see MUST return
`404 Not Found` (not `403`) to avoid disclosing existence across tenants.
Failing step 5 for a resource the caller *can* see returns `403 Forbidden`. A
`warehouseId` outside the caller's `MembershipWarehouse` is `404` — including for
a plain list query — so an invalid warehouse parameter cannot be used to probe
for the existence of another warehouse (`ROLES_PERMISSIONS.md` §4.1).

### 6.3 Implementation

- ASP.NET Core authorization with **policy-per-permission**:
  `["RequirePermission:inventory.stock.out"]`.
- A custom `PermissionAuthorizationHandler` resolves the effective permission
  set for `(membership, warehouse)` through `IPermissionResolver`.
- `IPermissionResolver` caches per membership with a short TTL (60 s) and is
  **invalidated immediately** when roles, permissions, assignments, membership
  status, or session state change. Revocation must never wait for cache expiry.
- Permissions are **not** embedded in the access token (ADR-0013), so a
  permission removal takes effect immediately instead of after token expiry.
- A `WarehouseScopeFilter` (EF Core query filter or explicit predicate helper)
  ensures list endpoints cannot return rows from unauthorized warehouses even
  if a handler forgets the filter.

### 6.4 Frontend relationship

The frontend receives the effective permission set for UI rendering (hide/disable
controls) only. **Every** endpoint independently enforces authorization. There is
a test that calls each protected endpoint directly with a token lacking the
permission and asserts `403`.

---

## 7. Authentication architecture

There are **three** flows, all converging on one session model
(`PROJECT_BLUEPRINT.md` §7, ADR-0034):

| Mechanism | Used by | Transport | Token scope |
|---|---|---|---|
| Platform operator password | Platform administrators | `POST /api/v1/platform/auth/login` | `platform` |
| Password | Office/admin/personal devices | `POST /api/v1/auth/login` | `tenant` |
| PIN | Shared warehouse terminals | `POST /api/v1/terminal/sessions/{enrollmentCode}/pin-auth` | `tenant` |

Token issuance is centralised in `ITokenService` so all three flows produce
identical session semantics; only the preconditions and the resulting scope
differ. A `Platform` session has `TenantId`/`MembershipId` NULL and
`SessionScope = 2`, enforced by a `CHECK` constraint.

Terminals are addressed by a 128-bit **enrollment code**, not by `deviceId`
(ADR-0035). A `deviceId` in a URL is a bearer value for the
`/identities` endpoint, which discloses the names of the staff assigned to a
warehouse, so the guessable human `DeviceCode` (`KITCHEN-01`) is never used to
address a terminal. The code is rotatable and dies with the device.

Throttle identifiers are stored as `HMAC-SHA256(identifier, serverPepper)`
(ADR-0036), not as a plain hash, so a database dump cannot be dictionary-attacked
to recover the tenant's email addresses.

Refresh token rotation:

```text
login  → refresh token R1 (stored hashed, family F)
refresh(R1) → issue R2, mark R1 used
refresh(R1) again → REUSE DETECTED → revoke entire family F + revoke session + audit
refresh(R2) → issue R3 ...
```

Detecting reuse of an already-rotated token is the primary defence against
refresh-token theft (ADR-0007).

---

## 8. Error handling and observability

| Concern | Approach |
|---|---|
| Error format | RFC 9457 `application/problem+json` (ADR-0027) |
| Internal exceptions | Caught by middleware; converted to a safe `500` with `traceId`; stack traces only in server logs |
| Validation errors | `400` with a field-keyed `errors` dictionary, codes not sentences |
| Business rule violations | `409 Conflict` with a stable machine code (e.g. `insufficient_stock`) |
| Authorization failures | `401` (unauthenticated), `403` (known resource, missing permission), `404` (cross-tenant existence) |
| Correlation | `X-Correlation-Id` accepted and echoed; generated if absent; present in every log line and audit row |
| Logging | Structured logging; **secret redaction enricher**; no PII beyond what is operationally necessary |
| Tracing | OpenTelemetry traces + metrics; SQL Server instrumentation |
| Health | `/health/live`, `/health/ready` (DB connectivity, migrations applied, outbox backlog) |
| Audit | Business/security events written inside the business transaction (see `SECURITY_ARCHITECTURE.md`) |

---

## 9. Background processing

The transactional outbox (ADR-0011) solves the "do not lose a notification if the
process crashes after commit" problem without distributed transactions:

```text
Business transaction
   ├─ business writes
   ├─ audit writes
   └─ OutboxMessage INSERT      ← same transaction, atomic
        │
        ▼ (after commit)
   Outbox worker (IHostedService)
        ├─ claim batch with UPDLOCK/ROWLOCK + status transition
        ├─ dispatch: notification, push, report generation
        ├─ on success → mark processed
        ├─ on failure → retry with exponential backoff + max attempts
        └─ on poison → dead-letter status + alert
```

Scheduled jobs in V1: expired-session sweeper, expired-reset-token sweeper,
near-expiry report, outbox dispatcher, subscription-state evaluator.

---

## 10. Frontend architecture (built in Phase 8)

| Concern | Decision |
|---|---|
| Stack | React + TypeScript + Vite, PWA |
| State/data | Server state via a query library; no duplicate server truth in local state |
| Auth storage | Access token in memory only; refresh token in `HttpOnly` cookie (ADR-0030) |
| Routing | Route guards are UX only; server is the authority |
| Offline | V1: online-first; read caching optional; **no offline writes** (ADR-0021) |
| Barcode/QR | Camera scanning in-browser where supported; hardware scanner support as keyboard wedge |
| i18n/RTL | Direction-aware layout driven by tenant/UI locale |
| Accessibility | Keyboard navigation and focus management required on terminal flows |
| Design system | Single component library with touch targets sized for terminals |

The frontend is explicitly **not** part of the security boundary.

---

## 11. Deployment architecture (built in Phase 1, hardened in Phase 8)

```text
docker-compose (V1)
├── nginx        : 443 (TLS), serves PWA, proxies /api
├── api          : 8080 internal only, non-root, read-only root FS where possible
├── sqlserver    : 1433 internal only, persistent volume, no public port
└── (optional) backup sidecar

Volumes:
├── db-data
├── app-files   (attachments)
└── backups

Secrets:
├── Docker secrets / environment injection from a secret store
└── NEVER baked into images, NEVER in the repository
```

Production requirements (must be satisfied before go-live):

- TLS with modern ciphers only; HSTS enabled.
- Database not reachable from the public network.
- Least-privilege database principals: the application user has no DDL rights
  in production and **no DELETE right on the audit table**.
- Automated backups with tested restore procedure.
- Log aggregation with alerting on authentication anomalies.
- Migration execution as a gated deployment step, never automatically on
  application start in production.
- The identifier pepper (ADR-0036) and the JWT signing key are injected from the
  secret store. The application **fails closed at startup** if either is missing,
  rather than falling back to a default — a silent fallback would put every
  deployment on a shared, publicly known key.

**Deployment is a Phase 1 deliverable, not a Phase 8 one.** The compose file,
Nginx configuration, and the migration gate exist from the first phase, because
"no deployment target until the end" is how environments drift and how the
first production incident becomes unreproducible. Phase 8 hardens it
(performance, alerting, restore drill) rather than introducing it.

---

## 12. Key architecture decisions

See `DECISIONS.md` (43 records). The decisions most visible in this document are
ADR-0001 (modular monolith), ADR-0004 (tenant from session), ADR-0006 (devices
belong to warehouses), ADR-0007 (token model), ADR-0011 (outbox), ADR-0013
(permissions not in token), ADR-0016 (single DbContext, schema per module),
ADR-0022 (containerised deployment), ADR-0030 (token storage in the frontend),
ADR-0032 (`TenantId`-leading unique indexes), ADR-0034 (platform session scope),
ADR-0035 (terminal enrollment code), ADR-0036 (HMAC throttle identifiers),
ADR-0038 (typed foreign keys), ADR-0039 (`CHECK` scope), ADR-0040 (eight
phases), ADR-0041 (filtered variants for nullable unique keys).
