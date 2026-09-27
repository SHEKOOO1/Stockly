# STOCKLY — DECISIONS (Architecture Decision Records)

Every meaningful architectural decision is recorded here. An ADR is **never
silently reversed**: superseding an ADR requires a new ADR that references it,
explains why, lists affected components and migration impact, and identifies the
regression tests to add.

**Format:** ID, Date, Status, Topic, Decision, Reason, Alternatives considered,
Consequences, Affected components.

**Status values:** `Proposed`, `Accepted`, `Superseded by ADR-XXXX`, `Rejected`

> All ADRs below are **Proposed** and take effect on blueprint approval.

---

## Index

| ID | Topic | Status |
|---|---|---|
| ADR-0001 | Modular Monolith over Microservices | Proposed |
| ADR-0002 | Backend stack: ASP.NET Core, EF Core, SQL Server | Proposed |
| ADR-0003 | Global user identity + tenant membership | Proposed |
| ADR-0004 | Tenant context derived from the session, never from input | Proposed |
| ADR-0005 | Warehouse scope as data, evaluated server-side | Proposed |
| ADR-0006 | Devices belong to warehouses, not users | Proposed |
| ADR-0007 | Short-lived JWT + rotating refresh tokens with reuse detection | Proposed |
| ADR-0008 | PIN authentication model and hashing | Proposed |
| ADR-0009 | Transaction-based inventory with an immutable ledger | Proposed |
| ADR-0010 | Concurrency via atomic guarded UPDATE + rowversion + CHECK | Proposed |
| ADR-0011 | Transactional outbox for side effects | Proposed |
| ADR-0012 | Dot-notation permission catalogue; roles are bundles | Proposed |
| ADR-0013 | Server-side permission resolution; permissions not in the JWT | Proposed |
| ADR-0014 | `Idempotency-Key` on state-changing inventory operations | Proposed |
| ADR-0015 | Append-only, tenant-aware audit log with restricted DB principal | Proposed |
| ADR-0016 | Single DbContext with schema-per-module naming in V1 | Proposed |
| ADR-0017 | Moving weighted average as the primary inventory valuation | Proposed |
| ADR-0018 | FEFO as the default outbound allocation | Proposed |
| ADR-0019 | Soft delete for master data; never delete ledger/audit | Proposed |
| ADR-0020 | Localization: frontend i18n, `Name`/`NameAr` master data, enum codes | Proposed |
| ADR-0021 | No offline writes in V1 | Proposed |
| ADR-0022 | Containerised deployment: Nginx + API + SQL Server | Proposed |
| ADR-0023 | No AI assistant inside Stockly | Proposed |
| ADR-0024 | `decimal(18,4)` for money and quantities, per-tenant currency | Proposed |
| ADR-0025 | Attachments in a file store behind an abstraction; GUID storage keys | Proposed |
| ADR-0026 | Document numbering via a sequence table with a unique-index guard | Proposed |
| ADR-0027 | RFC 9457 ProblemDetails for all errors | Proposed |
| ADR-0028 | Layered rate limiting (edge, global, endpoint, identity, device) | Proposed |
| ADR-0029 | UTC storage, tenant timezone for presentation | Proposed |
| ADR-0030 | Access token in memory, refresh token in an HttpOnly cookie | Proposed |
| ADR-0031 | Web-push subscriptions are membership-scoped and isolated | Proposed |
| ADR-0032 | Every unique index on a tenant-scoped table leads with `TenantId` | Proposed |
| ADR-0033 | `ProductBarcode` is the single source of truth for barcodes | Proposed |
| ADR-0034 | Platform operators have their own session scope and login flow | Proposed |
| ADR-0035 | Terminals are addressed by a 128-bit enrollment code | Proposed |
| ADR-0036 | Throttle identifiers are HMAC-hashed, not plain-hashed | Proposed |
| ADR-0037 | Append-only tables carry no `RowVersion` and no `IsCurrent` | Proposed |
| ADR-0038 | Typed foreign keys instead of polymorphic operational references | Proposed |
| ADR-0039 | `CHECK` constraints carry absolute invariants only | Proposed |
| ADR-0040 | The delivery plan is eight phases | Proposed |
| ADR-0041 | Nullable unique-key components get explicit filtered variants | Proposed |
| ADR-0042 | SQL Server full text for V1 global search | Proposed |
| ADR-0043 | The barcode symbology set is the six modelled values | Proposed |
| ADR-0044 | Role grants: enumerate the 33 decisive permissions, derive the other 72 by rule D1–D5 | Proposed |

**Total: 44 ADRs.** ADR-0001–ADR-0031 are the original architecture set;
ADR-0032–ADR-0044 were added during blueprint finalization to close contradictions
that the original set left open (index tenant-scoping, barcode ownership, platform
authorization, terminal identity, throttle hashing, append-only semantics,
referential integrity, `CHECK` scope, phase count, nullable unique keys, search
engine, symbologies, role-grant completeness). ADR-0008 is the PIN/password
hashing decision; ADR-0018 is the FEFO decision extended by ADR-0041.

---

## ADR-0001 — Modular Monolith over Microservices

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Build Stockly as a single deployable ASP.NET Core application
organised into business modules with enforced boundaries. No microservices.

**Reason:** Inventory correctness requires multi-table transactions inside one
database. A monolith gives atomic stock posting for free. The team and customer
scale do not justify distributed-system operational cost.

**Alternatives considered:**
- *Microservices* — rejected: distributed consistency for inventory, service
  discovery/tracing overhead, no current scaling need.
- *Serverless functions* — rejected: cold starts and unpredictable latency for a
  terminal workload.
- *Modular monolith then extract* — **chosen**, so extraction remains possible.

**Consequences:** simple deployment and debugging; discipline is required to keep
module boundaries real (enforced by architecture tests). Extraction later means
extracting a module, not rewriting the system.

**Affected:** solution structure, deployment, CI, architecture tests.

---

## ADR-0002 — Backend stack: ASP.NET Core, EF Core, SQL Server

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** ASP.NET Core Web API on **.NET 10 (LTS)**, EF Core with the SQL
Server provider, SQL Server 2022, REST + OpenAPI.

**Reason:** Required stack for a production WMS with strong typing, first-class
transaction support, filtered indexes, `rowversion`, check constraints, and
partitioning — all of which Stockly's integrity model depends on. .NET 10 is the
current LTS and the installed SDK (`10.0.201`) matches.

**Alternatives considered:** Node/NestJS (rejected: the integrity model depends on
SQL Server features that map less directly and on a typed domain model);
PostgreSQL (rejected: the target infrastructure expectation is SQL Server).

**Consequences:** SQL Server is a hard dependency; the in-memory provider is not
acceptable for integration/concurrency tests.

**Affected:** everything backend, test infrastructure, deployment.

---

## ADR-0003 — Global user identity + tenant membership

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** `AppUser` is a **global** identity with no `TenantId`. Access to an
organization is granted by `TenantMembership` (user to tenant), which carries the
per-tenant status and links roles and warehouse assignments.

**Reason:** One human is one identity. Multi-tenant SaaS must not create
duplicate accounts per tenant, and must support one person acting in several
organizations. Separating identity from membership also gives a single place to
enforce platform-wide security (lockout, rate limiting) independent of tenants.

**Alternatives considered:**
- *`TenantId` directly on `User`* — rejected: duplicate identities, impossible
  cross-tenant identity, confusing lockout semantics.
- *User-per-tenant isolation* — rejected: a platform admin becomes N accounts.

**Consequences:** every business query joins through membership; login requires a
membership-selection step when a user has more than one tenant; `AppUser` is a
global table and therefore never tenant-filtered (a documented exception).

**Affected:** auth, sessions, permissions, data access, security tests.

---

## ADR-0004 — Tenant context derived from the session, never from input

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** The tenant of a request comes from the validated token/session
claims. A `tenantId` supplied in a body, query, route, or header is **rejected as
an unknown field** (body) or ignored (query/header).

**Reason:** Tenant isolation is the single most important guarantee. Any
client-supplied tenant selector is a tenant-escape vector. Header-based tenant
selection is a widespread industry anti-pattern.

**Alternatives considered:** header-selected tenant (rejected: trivially
forgeable), URL-path tenant prefix (rejected: leaks tenant identity, encourages
sharing links), body field (rejected: mass assignment).

**Consequences:** switching tenants is an explicit action
(`POST /auth/select-tenant`) that issues a new token bound to a membership.

**Affected:** all endpoints, authorization, security tests S1/S3/S5.

---

## ADR-0005 — Warehouse scope as data, evaluated server-side

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Warehouse authorization is a data relationship
(`MembershipWarehouse`), fully independent of roles. The effective permission for
an operation is the intersection of role-granted permissions and the
membership's warehouse assignments, with optional per-role warehouse scoping via
`MembershipRole.ScopeType`.

**Reason:** The product requires that "has `stock.out`" and "may do it in *this*
warehouse" are separate facts. Modelling scope inside roles would conflate them
and make audits of "who can do what where" impossible.

**Alternatives considered:** warehouse list on the role (rejected: cannot express
assignment independent of role), warehouse filter in the UI only (rejected:
violates the security-first requirement).

**Consequences:** two join tables instead of one; authorization queries are
slightly heavier (mitigated by a short-lived cache with immediate invalidation).

**Affected:** authorization, role model, database, security tests S2.

---

## ADR-0006 — Devices belong to warehouses, not users

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** A `Device` belongs to exactly one tenant and exactly one warehouse.
An `AuthSession` references the user, the membership, and optionally the device.
A device is never owned by a user.

**Reason:** Shared terminals are the primary Stockly use case. Devices outlive
users, are managed by warehouse administrators, and must be revocable
independently of the people who used them.

**Alternatives considered:** user-owned devices (rejected: breaks the shared
terminal model), no device model at all (rejected: loses attribution, revocation,
and per-device throttling).

**Consequences:** every transaction can be attributed to a physical terminal;
device revocation immediately kills its sessions. Physical possession is not
cryptographically proven — recorded as SEC-KNW-01.

**Affected:** sessions, audit, security, inventory attribution.

---

## ADR-0007 — Short-lived JWT + rotating refresh tokens with reuse detection

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Access token is an RS256 JWT with a 15-minute lifetime, carrying
identity claims only (`sub`, `mid`, `tid`, `sid`, `did`, `jti`). Refresh token is
an opaque 256-bit random value stored **hashed**, organised in families, rotated
on every use. Reuse of an already-rotated token revokes the entire family and
writes a security audit event. Session state is checked server-side on every
authorized request.

**Reason:** Short-lived tokens bound the damage from a stolen access token.
Opaque rotated refresh tokens with family reuse detection are the standard
defence against refresh-token theft. Server-side session state makes revocation
immediate instead of expiry-delayed.

**Alternatives considered:**
- *Long-lived JWT with no refresh* — rejected: cannot revoke.
- *Opaque access tokens with a DB lookup per request* — rejected: latency, and
  JWT plus session-state gives equivalent guarantees with a cheaper hot path.
- *Symmetric (HS256) signing* — rejected: asymmetric allows verification without
  distributing the signing key.

**Consequences:** a session-state lookup per request (cached briefly). Revocation
is immediate. Key rotation needs an overlap procedure.

**Affected:** auth, sessions, security tests S6/S7/S23.

---

## ADR-0008 — PIN authentication model and hashing

**Date:** 2026-09-27 · **Status:** Proposed (algorithm TBD-02)

**Decision:** Terminal PINs are 6 to 12 digits, hashed with a memory-hard KDF
(Argon2id preferred, BCrypt acceptable), compared in constant time, never returned
or logged. The identity list endpoint returns names only. A PIN is the **only**
authenticator for a shared terminal; the selected name grants nothing. Throttling
applies per device, per membership, and per IP. `PinCredential.Version` bumps
invalidate dependent sessions.

**Reason:** PINs are low-entropy compared to passwords, so they need much
stronger throttling and shorter sessions than a password flow. The "name is not
authentication" rule prevents the most common shared-terminal vulnerability.

**Alternatives considered:** per-device static device PINs (rejected: no
accountability for actions), NFC/RFID badges (rejected: cost, hardware
dependency; revisit in V2), full password entry at a terminal (rejected: poor
terminal UX and shoulder-surfing risk for longer secrets).

**Consequences:** 6-digit PINs are inherently brute-forceable in isolation, so the
controls around them (lockout, session lifetime, auditing) are mandatory rather
than optional.

**Affected:** terminals, sessions, security tests S8/S9/S10.

---

## ADR-0009 — Transaction-based inventory with an immutable ledger

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Stock quantity is never directly editable. `StockDocument` and
`StockDocumentLine` are posted into an immutable `StockTransaction` ledger, and
`StockBalance` / `InventoryLot` are transactionally maintained projections that
can be rebuilt from the ledger.

**Reason:** A freely editable quantity field cannot be audited, cannot explain
history, and cannot be reconciled. The ledger is the evidence; the balance is a
performance structure.

**Alternatives considered:**
- *Editable balance field* — rejected: destroys auditability (explicit product
  rule).
- *Event sourcing for all of Stockly* — rejected: overkill outside inventory.
- *No ledger, only balances* — rejected: no history, no dispute resolution.

**Consequences:** more writes per operation; reversal is the only correction
mechanism; a tested rebuild job must exist.

**Affected:** inventory module, reports, audit, business rules section 5.

---

## ADR-0010 — Concurrency via atomic guarded UPDATE + rowversion + CHECK

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Balance mutation is a single conditional `UPDATE`
(`SET OnHand = OnHand - @qty WHERE Id = @id AND OnHand >= @qty`) inside an
explicit transaction. `rowversion` supports optimistic concurrency on other
mutable tables. `CHECK (OnHandQuantity >= 0)` is a final database-level barrier.

**Reason:** Read-then-write in application code is not safe under concurrency.
Making the guard part of the SQL statement removes the race entirely and is
verifiable with a raw-SQL integrity test. A `CHECK` constraint protects against
any future code path that bypasses the guard.

**Alternatives considered:**
- *`SELECT` then `if` then `UPDATE`* — rejected: TOCTOU race.
- *`sp_getapplock` / distributed locks* — rejected: adds a stateful dependency
  for something a single statement already solves.
- *Serializable isolation for everything* — rejected: broad contention cost.

**Consequences:** callers must interpret `0 rows affected` as a business failure
(`insufficient_stock`). Deadlocks are still possible and must be handled by
consistent lock ordering plus bounded retry.

**Affected:** inventory, tests S17, concurrency suite, DB integrity tests.

---

## ADR-0011 — Transactional outbox for side effects

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Notification fan-out, push delivery, and report generation are
written as `ops.OutboxMessage` rows **inside the business transaction** and
processed asynchronously by a hosted worker. Security audit events are always
written synchronously in the same transaction.

**Reason:** Sending a notification after commit can lose it on a crash; using a
distributed transaction for this is heavy. The outbox gives at-least-once
delivery with no new infrastructure and no lost business side effects.

**Alternatives considered:**
- *Fire-and-forget after commit* — rejected: lossy.
- *RabbitMQ/Kafka* — rejected: unjustified operational overhead for V1.
- *Same-transaction synchronous push* — rejected: a slow third party would block
  inventory posting.

**Consequences:** at-least-once delivery, therefore consumers must be idempotent.
Dead-letter handling and alerting are required.

**Affected:** notifications, reports, hosted services, ops schema.

---

## ADR-0012 — Dot-notation permission catalogue; roles are bundles

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Permissions are globally seeded, read-only codes
(`module.resource.action`, 107 of them). Roles are tenant-scoped named bundles of
permission ids. System roles are seeded per tenant and their permission sets are
immutable in V1. Custom roles may only contain a subset of the creator's
effective permissions.

**Reason:** Decoupling permissions from roles lets Stockly ship new features
without schema changes, and makes "what may I do" answerable by inspection.
Immutability of system roles prevents a tenant admin from quietly widening a
privileged role.

**Alternatives considered:** hard-coded enums per role (rejected: not extensible),
per-tenant free-form permissions without roles (rejected: unusable for review and
audit), mutable system roles (rejected: privilege-escalation risk).

**Consequences:** permission codes are API contract. Changing one requires an ADR.
Role customisation is limited in V1 (see RP-TBD-01).

**Affected:** authorization, seeding migrations, admin UI, security tests S4.

---

## ADR-0013 — Server-side permission resolution; permissions not in the JWT

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** The access token carries identity and session claims only.
Effective permissions are resolved server-side per request from a resolver with a
roughly 60-second cache that is invalidated **immediately** on role, permission,
membership, or session changes.

**Reason:** Embedding permissions in a JWT means a revoked permission stays
effective until the token expires (up to 15 minutes) — a real, exploitable
window. Resolving server-side makes revocation instant. A short cache keeps the
per-request cost acceptable.

**Alternatives considered:**
- *Permissions in the JWT* — rejected: stale authorization window.
- *No cache, DB lookup per request* — rejected: acceptable for correctness but
  wasteful; the cache with immediate invalidation gives both.
- *Longer-lived tokens to reduce lookups* — rejected: worse revocation window.

**Consequences:** every authorized request performs at least one session-state
check. The cache must be invalidated by the write paths themselves, which is
covered by tests.

**Affected:** authorization pipeline, performance, security tests S23.

---

## ADR-0014 — `Idempotency-Key` on state-changing inventory operations

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Posting a stock document, a goods receipt, or a stock-count
submission requires an `Idempotency-Key` header (16 to 100 chars). The key is
stored hashed, scoped to tenant + actor + endpoint, with a unique index. A repeat
with the same payload returns the original result; a repeat with a different
payload returns `409 idempotency_conflict`.

**Reason:** Terminal networks retry. Without idempotency, a retried receipt
silently doubles stock. A unique index makes the race safe at the database level.

**Alternatives considered:** client-side de-duplication only (rejected: client
controlled), natural business keys only (rejected: not available for all
operations), exactly-once messaging (rejected: over-engineering).

**Consequences:** a key table and retention policy; response replay must be an
exact representation of the original.

**Affected:** inventory, purchasing, stock count, tests S18.

---

## ADR-0015 — Append-only, tenant-aware audit log with a restricted DB principal

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** `audit.AuditLog` is append-only, partitioned monthly, and the
application database principal has **no** `UPDATE` or `DELETE` grant on it.
Security events are written synchronously inside the business transaction.
Payloads are redacted of all secret-named fields. Raw before/after JSON requires
`reports.audit`.

**Reason:** Audit is worthless if the actor can alter it. Enforcing append-only in
the database — not just in code — means a compromised application bug still
cannot rewrite history.

**Alternatives considered:**
- *Application-enforced append-only* — rejected: insufficient guarantee.
- *External log aggregator as the source of truth* — rejected: unavailable in
  air-gapped deployments; kept as a complement in production, not the system of
  record.
- *Mutable audit with a "changed" flag* — rejected: permits silent edits.

**Consequences:** purge/archive must be performed by a privileged DBA job, not
the application. Retention is TBD-14.

**Affected:** audit, DB roles, security tests S20/S21.

---

## ADR-0016 — Single DbContext with schema-per-module naming in V1

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** One `StocklyDbContext` with a single migrations assembly; tables are
placed in per-module schemas (`identity.`, `inventory.`, and so on). Module
boundaries are enforced in code (namespaces, abstractions) and by architecture
tests.

**Reason:** Multiple DbContexts per module would fragment migrations and make
cross-module transactional work (purchasing to inventory) harder, while the
module boundaries that actually matter (ownership of tables, authorization,
audit) are already enforced by conventions and tests. Schemas give logical
separation without migration complexity.

**Alternatives considered:**
- *DbContext per module* — rejected for V1: migration ordering and cross-module
  transactions become painful; reversible later.
- *No schema separation, `dbo` only* — rejected: loses obvious ownership and
  makes selective permissions/backup harder.

**Consequences:** discipline plus architecture tests are required. The path to
splitting contexts exists if a module ever needs independent scaling.

**Affected:** infrastructure, migrations, architecture tests.

---

## ADR-0017 — Moving weighted average as the primary inventory valuation

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Stock value is the moving weighted average per (tenant, warehouse,
product), updated on receipt. Issuing does not change the average. Where no
purchase history exists, the fallback order is last purchase price, then
standard cost, then 0, and the fallback is explicitly recorded in the cost
snapshot.

**Reason:** Food and consumable inventory for a residence or conference operation
is bought at varying prices; moving average reflects reality closely enough for
costing without the complexity of FIFO-layer valuation and expiry-driven
obsolescence. Flagging estimated values prevents false precision in reports.

**Alternatives considered:** FIFO layer valuation (rejected: complexity, and it
interacts badly with FEFO consumption), LIFO (rejected: not meaningful for food),
standard cost only (rejected: requires manual maintenance that will drift).

**Consequences:** the average can be historically inaccurate after a large
stock-out; the ledger retains actual costs for audit.

**Affected:** food costing, inventory valuation, reports.

---

## ADR-0018 — FEFO as the default outbound allocation

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Outbound stock allocation consumes lots in ascending expiry order,
then earliest `ReceivedAtUtc`, then lowest id. Explicit batch selection is
permitted for callers with the lot permission. Non-expiry-tracked products ignore
expiry.

**Reason:** For food and perishables, preventing expiry waste is a primary
business objective; FIFO by receipt date would waste stock unnecessarily.

**Alternatives considered:** FIFO (rejected: wastes expiring stock), LIFO
(rejected: wrong for perishable management), manual selection only (rejected:
error-prone and slow for high-volume terminal operations).

**Consequences:** FEFO allocation logic must be deterministic and unit-tested
with ties; reports should highlight lots that will expire before expected use.

**Affected:** inventory posting, waste reports, dashboards.

---

## ADR-0019 — Soft delete for master data; never delete ledger or audit

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Master and configuration data use `IsDeleted` with filtered unique
indexes (so codes can be reused). Ledger (`StockTransaction`), audit
(`AuditLog`), price history, and cost snapshots are append-only and have no
delete path. Physical purge is a separate, audited retention job (V2).

**Reason:** Historical transactions must keep referring to valid products,
warehouses, and suppliers. Soft delete preserves history while removing the item
from active use.

**Alternatives considered:** hard delete with `ON DELETE SET NULL` (rejected:
destroys the historical reference), status field without a delete marker
(rejected: `IsDeleted` also serves filtered unique indexes), archive tables
(rejected: over-engineering for V1).

**Consequences:** every list query must consider `IsDeleted`; unique indexes must
be filtered; `includeDeleted=true` requires the **resource's own** `*.manage`
permission (see `API_CONVENTIONS.md` §5.3), never a single global permission.

**Affected:** all master data, queries, indexes.

---

## ADR-0020 — Localization strategy

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** UI strings live in the frontend i18n resources. Master data carries
`Name` (canonical) plus optional `NameAr`. Enums cross the wire as stable codes.
Numbers, currency, and dates are sent raw (decimal + ISO currency; UTC ISO-8601)
and formatted client-side. The backend stores no pre-translated sentences.

**Reason:** Translating every master-data name is a tenant data concern;
translating product sentences is not. Keeping presentation formatting in the
frontend avoids duplicating locale rules on the server and makes RTL an entirely
frontend concern.

**Alternatives considered:** server-rendered localized strings (rejected: mixes
concerns, bloats the API), resource files on the server (rejected: same), a
translation table for all entities (rejected: heavy, low value).

**Consequences:** the frontend owns RTL/LTR entirely. Report/PDF localization is
a separate open question (TBD-15).

**Affected:** API contracts, frontend, reports.

---

## ADR-0021 — No offline writes in V1

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** The V1 PWA is online-first. Offline read caching of master data is
permitted; **offline stock posting is not**. If the network drops during an
operation, the user is told the operation did not complete.

**Reason:** Offline stock posting on a shared terminal would create a queue whose
reconciliation (duplicates, conflicts, negative stock, wrong warehouse) requires
a full distributed-conflict model. That complexity is not justified before the
core inventory engine is proven. Claiming offline writes without a correct
reconciliation design would be worse than not having them.

**Alternatives considered:** full offline queue (rejected: conflict resolution is
a project in itself), offline read-only (chosen for V1), service-worker
background sync for non-critical reads (allowed).

**Consequences:** warehouse network quality becomes an operational requirement
and must be documented. Revisit in V2 with a designed reconciliation protocol.

**Affected:** frontend, operations, documentation. TBD-20 open.

---

## ADR-0022 — Containerised deployment: Nginx + API + SQL Server

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** V1 deployment is Docker Compose: Nginx terminating TLS and serving
the PWA, the API on an internal port only, SQL Server on an internal port only,
with named volumes for data, files, and backups. Secrets are injected, never
baked in.

**Reason:** Simple, reproducible, and sufficient for a single-tenant SaaS
deployment at V1 scale. A managed database may replace the container in
production; the application is unaffected.

**Alternatives considered:** Kubernetes (rejected: unjustified operational
complexity for V1), bare-metal/IIS (rejected: less reproducible, harder TLS
consistency), managed cloud only (rejected: the first customer may be
on-premise).

**Consequences:** TLS, backups, and restore procedures must be scripted and
tested. Migrations run as a gated step, not on app start.

**Affected:** infrastructure, CI/CD, operations.

---

## ADR-0023 — No AI assistant inside Stockly

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Stockly the product contains **no** AI assistant, chatbot, copilot,
or conversational AI feature. AI may be used in the development process only.

**Reason:** Explicit product requirement. It also keeps the security model
auditable — no probabilistic component sits in a security decision path, and no
user data leaves the deployment boundary implicitly.

**Alternatives considered:** adding an assistant later (rejected: product
requirement; if it ever changes, it requires a new ADR and a security review).

**Consequences:** no LLM integration, no AI-based suggestions in the UI, no
AI-generated content in the database model.

**Affected:** product scope, architecture, security review checklist.

---

## ADR-0024 — `decimal(18,4)` for money and quantities, per-tenant currency

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** All money and quantity values are `decimal(18,4)` with at most four
decimal places. Currency is a per-tenant setting expressed as an ISO-4217 code.
Floating point is never used for money or quantity.

**Reason:** Float arithmetic produces rounding errors that are unacceptable in
stock and costing. Four decimals supports kilograms with grams and small
currency units, and a single scale avoids conversion bugs.

**Alternatives considered:** `money` type (rejected: currency-dependent scale and
rounding surprises), per-unit decimal precision (rejected: complexity for little
practical gain), integer minor units (rejected: complicated with fractional
quantities).

**Consequences:** 4-decimal validation must be enforced at the API boundary. A
test asserts the database rejects 5-decimal values.

**Affected:** schema, validation, costing, reports.

---

## ADR-0025 — Attachments in a file store behind an abstraction, GUID storage keys

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Files are stored behind an `IFileStore` abstraction (local volume
in V1, S3-compatible later). The storage key is a server-generated GUID path; the
client-supplied file name is metadata only and is never used to build a path.
Downloads are served by an authenticated endpoint that re-authorizes entity
access. Allowlisted extensions, content types, magic-byte validation, size
limits, and `nosniff` are enforced. `OcrStatus`/`ExtractedText` columns are
reserved for V2 OCR.

**Reason:** The client file name is a classic path-traversal and
content-type-confusion vector. GUID keys remove the whole class of problem.
Reserving OCR fields now avoids a breaking schema change later.

**Alternatives considered:** storing binaries in the database (rejected: bloat,
poor backup behaviour), trusting the client path (rejected: vulnerability),
base64 in the API (rejected: 33% overhead, memory pressure).

**Consequences:** a storage backend abstraction is required from day one; no
public or signed URL shortcuts that skip authorization.

**Affected:** files module, security tests S15.

---

## ADR-0026 — Document numbering via a sequence table

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Document numbers come from `ops.DocumentSequence` per (tenant,
document type, period), allocated with an atomic
`UPDATE ... SET NextValue = NextValue + 1` guarded by a unique index.

**Reason:** `MAX(number) + 1` in application code is a well-known race that
produces duplicate document numbers under concurrency — unacceptable for
auditable documents.

**Alternatives considered:** SQL `SEQUENCE` (rejected: not tenant-scoped or
resettable per period without workarounds), `MAX+1` (rejected: race),
application-generated GUID-only numbers (rejected: humans need readable numbers
for physical documents).

**Consequences:** one round trip per allocation; gaps in numbering are acceptable
and expected.

**Affected:** inventory, purchasing, events.

---

## ADR-0027 — RFC 9457 ProblemDetails for all errors

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** All non-2xx responses use `application/problem+json` per RFC 9457
with `type`, `title`, `status`, `detail`, `instance`, `traceId`, a stable `code`,
and a field-keyed `errors` map. `detail` is always safe.

**Reason:** A single machine-parseable error contract lets the frontend show
field-level messages and stable codes, keeps error text reviewable, and forces
discipline about not leaking internals.

**Alternatives considered:** ad-hoc `{ success, message }` envelopes (rejected:
no field mapping, no standard), empty bodies with status codes only (rejected:
unhelpful to users), RFC 7807 (superseded by 9457, same shape).

**Consequences:** a global exception handler must convert everything; tests
assert no stack traces or SQL ever appear.

**Affected:** API, frontend error handling, security tests S22.

---

## ADR-0028 — Layered rate limiting

**Date:** 2026-09-27 · **Status:** Proposed (numbers TBD-04)

**Decision:** Rate limiting is layered: edge (Nginx, per IP), global (per IP),
per-endpoint-group, per-identity for auth, per-device, per-membership, and
per-tenant for expensive jobs. Keys derive from the verified identity where
available and from the client IP otherwise. `X-Forwarded-For` is trusted only
from configured proxy hops. Throttling decisions on auth endpoints are audited.

**Reason:** A single global limit either breaks legitimate terminal use or fails
to stop targeted PIN brute force. Layered limits let a legitimate high-volume
warehouse work while a specific device or account is throttled hard.

**Alternatives considered:** in-memory only (rejected: bypassable across
instances, which we may need), a single limit (rejected: as above), no rate
limiting (rejected: unacceptable).

**Consequences:** per-instance counters in V1 (limiter interface designed for a
shared store — SEC-KNW-05). A trusted-proxy configuration is mandatory.

**Affected:** infrastructure, security, tests S9/S10.

---

## ADR-0029 — UTC storage, tenant timezone for presentation

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** All timestamps are stored as UTC `datetime2(3)`. Each tenant has a
timezone. Report date filters are interpreted in the tenant timezone and
converted to UTC at the query boundary. The API always emits UTC ISO-8601.

**Reason:** Mixing local times in storage is one of the most common sources of
corrupt historical data. Interpretation at the boundary keeps reports correct for
the tenant regardless of where the reader is.

**Alternatives considered:** local-time storage (rejected: DST and migration
problems), UTC with a global timezone (rejected: wrong for a single-region
tenant), storing UTC plus a duplicate local copy (rejected: redundancy that
drifts).

**Consequences:** a tenant timezone setting is required; report filters must be
timezone-aware; the frontend formats using the tenant timezone.

**Affected:** schema, reports, API, frontend.

---

## ADR-0030 — Access token in memory, refresh token in an HttpOnly cookie

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** The PWA keeps the access token in memory only (lost on reload) and
receives the refresh token as an `HttpOnly; Secure; SameSite=Strict` cookie scoped
to the auth path. On app start it calls `/auth/refresh` to obtain a new access
token. State-changing requests carry the access token in a header; the CSRF risk
of the cookie is covered by `SameSite=Strict` plus an explicit anti-CSRF token on
cookie-authenticated mutations.

**Reason:** `localStorage` is readable by any XSS payload, which would let
stolen JavaScript exfiltrate a long-lived credential. An `HttpOnly` cookie cannot
be read by script. `SameSite=Strict` blocks cross-site submission. Memory-only
access tokens limit the XSS window to the current page's lifetime.

**Alternatives considered:** tokens in `localStorage` (rejected: XSS
exfiltrable), both tokens in cookies (rejected: CSRF surface on every request),
long-lived access token in a cookie (rejected: revocation problem).

**Consequences:** a page reload triggers a refresh round trip; the API must
support refresh-based re-authentication. Multi-tab requires a shared refresh
path, and refresh family reuse detection must tolerate legitimate parallel
refreshes (see S7).

**Affected:** frontend, auth endpoints, security tests S7/S14.

---

## ADR-0031 — Web-push subscriptions are membership-scoped and tenant-isolated

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** A `PushSubscription` belongs to exactly one membership, optionally
one device, and is used only for notifications targeted at that membership within
its tenant. Subscription keys (`P256dh`, `Auth`) are stored as operational
secrets: never returned by any read endpoint, never logged, never included in
audit payloads, and deleted on unsubscribe.

**Reason:** Push endpoints are effectively credentials for reaching a user
device. Leaking them would allow unsolicited messages and user tracking. Strict
scoping prevents a notification intended for one person or tenant from reaching
another.

**Alternatives considered:** one subscription per device shared by all users of
that device (rejected: a shared terminal must not push one user's private notices
to the next user), storing keys in the client only (rejected: the server must sign
the push request).

**Consequences:** push delivery is best-effort; every subscription is tied to a
membership, so switching tenants requires a separate subscription. A test asserts
no endpoint returns subscription keys.

**Affected:** notifications module, security tests S21.

---

## ADR-0032 — Every unique index on a tenant-scoped table leads with `TenantId`

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Any unique index or unique constraint on a tenant-scoped table
MUST have `TenantId` as its **leading** column, so uniqueness is enforced within
a tenant and only within a tenant. The only permitted exceptions are indexes that
guard a **server-generated global identifier** which has no tenant context at
lookup time: `files.Attachment.StorageKey`,
`identity.RefreshToken.TokenHash`, `identity.PasswordResetToken.TokenHash`,
`identity.Device.EnrollmentCode`, `notifications.PushSubscription.Endpoint`, and
the surrogate keys of `identity.LoginAttempt` / `audit.AuditLog`. Each exception
is named explicitly in `DATABASE_DESIGN.md` §5.6.

**Reason:** `inventory.StockBalance`, `inventory.InventoryLot`,
`master.ProductBarcode`, `identity.MembershipWarehouse` and others previously
had unique indexes such as `(WarehouseId, ProductId)` or
`(MembershipId, WarehouseId)`. Two problems follow. First, the index no longer
supports the dominant query prefix `(TenantId, …)`. Second — and worse — a
composite FK from a child references its parent on `(Id, TenantId)`, so the
parent needs `UNIQUE (Id, TenantId)` regardless; an index that omits `TenantId`
leaves the *business* uniqueness rule accidentally global, which would let one
tenant block another tenant's SKU code or warehouse code.

**Alternatives considered:** keep business keys global (rejected: a global
uniqueness requirement that the product never asked for, and a cross-tenant
denial-of-service). Drop tenant-scoped uniqueness entirely (rejected: duplicate
SKUs within a tenant make barcode scanning and stock lookups ambiguous).

**Consequences:** every entity's index list changes; the migration must create
the `TenantId`-leading indexes up front. Global-lookup code (terminal
enrollment, attachment blob fetch) uses its own documented unique index.

**Affected:** all 66 entities' index definitions, `DATABASE_DESIGN.md` §5/§7,
`PROJECT_BLUEPRINT.md` §5.1, migration tests.

---

## ADR-0033 — `master.ProductBarcode` is the single source of truth for barcodes

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** `master.Product` has **no** `Barcode` column. Every barcode lives in
`master.ProductBarcode`, which carries `(TenantId, Barcode)` as a unique key and
`IsPrimary` to mark the primary code.

**Reason:** the previous schema had both `Product.Barcode` and
`ProductBarcode`, each with its own unique index. The two could disagree, the
scan lookup had to check two tables (or pick an arbitrary winner), and "primary
barcode" was expressible twice with no single authority.

**Alternatives considered:** keep `Product.Barcode` as a denormalised cache of
the primary (rejected: two write paths, drift, and no compensating consistency
rule). Keep only `Product.Barcode` and drop `ProductBarcode` (rejected: products
legitimately carry several codes — EAN-13 plus a Code-128 for printing).

**Consequences:** product DTOs expose barcodes as a collection; the scan lookup
hits one covering index; no data migration is needed (V1 has no data).

**Affected:** `master` module, barcode scan endpoints, product import/export,
`DATABASE_DESIGN.md` §4.4.

---

## ADR-0034 — Platform operators have their own session scope and login flow

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** `identity.AuthSession` gains `SessionScope` (`1=Tenant`,
`2=Platform`) with `TenantId` and `MembershipId` **nullable** and a `CHECK`
enforcing exactly one shape. `POST /api/v1/platform/auth/login` authenticates a
user with `AppUser.IsPlatformAdmin = true` and issues a token with scope
`platform` and no tenant claim. A platform token is rejected by every
non-`/platform/**` route; a tenant token is rejected by every `/platform/**`
route. In V1, `IsPlatformAdmin` grants the entire `platform.*` catalogue.

**Reason:** the platform could not be operated as designed. The tenant login flow
rejects a user with zero active memberships, and the permission resolver
derives every permission from `MembershipRole`, so a platform operator with no
tenant membership had no way to sign in and no way to hold a permission. Two
alternative fixes were rejected: giving platform operators a synthetic tenant
(fakes tenant scope and pollutes reports) and letting them hold a real tenant
membership (their privileges would then depend on which tenant they picked).

**Alternatives considered:** a single scope with implicit platform checks inside
every handler (rejected: no defence in depth, and an IDOR waiting to happen).
Splitting platform operators into separate roles now (rejected: premature; the
flag is the minimum, recorded as RP-TBD-06).

**Consequences:** `scp` becomes a required claim; the authorization middleware
gains a scope branch; new security tests assert the two token types cannot be
used on each other's routes; a platform session list is available for audit.

**Affected:** identity module, authorization middleware, `SECURITY_ARCHITECTURE.md`
§5.4, `API_CONVENTIONS.md`, `ROLES_PERMISSIONS.md` §3.

---

## ADR-0035 — Terminals are addressed by a 128-bit enrollment code

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** `identity.Device` gains `EnrollmentCode` (128-bit random,
base32url, globally unique, rotatable, never displayed to end users). The shared
terminal endpoints are addressed by that code, not by `DeviceCode`. Rotating or
revoking a device invalidates the code. `GET
/api/v1/terminal/sessions/{enrollmentCode}/identities` returns only
`{ membershipId, displayName, avatarRef }` for memberships assigned to that
device's warehouse, and is rate limited per IP and per code.

**Reason:** the terminal flow asked a client to present a `deviceId`, and the
`DeviceCode` examples in the design (`KITCHEN-01`) are guessable. A remote
attacker could enumerate codes and harvest the names of a tenant's staff — a
staffing and schedule disclosure before any authentication is attempted.
Knowing a `deviceId` was also enough to reach the identity-listing endpoint.

**Alternatives considered:** requiring the terminal to be pre-registered by
membership id (rejected: a shared terminal must support many users, and the id
would still be a bearer value). Client certificates per terminal (rejected:
provisioning cost is disproportionate in V1). Network allow-listing only
(rejected: the same WLAN is not a trust boundary; a compromised device on that
WLAN is the threat).

**Consequences:** the enrollment code is displayed once at registration and
stored by the operator; the residual risk that physical possession is not
cryptographically proven is recorded as SEC-KNW-01/SEC-KNW-08 in
`DEVELOPMENT_STATUS.md` and re-assessed in V2.

**Affected:** identity module, terminal UI, `SECURITY_ARCHITECTURE.md` §6,
`WORKFLOWS.md`, security tests for the terminal flow.

---

## ADR-0036 — Throttle identifiers are HMAC-hashed, not plain-hashed

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** `identity.LoginAttempt.IdentifierHash` and
`EnrollmentCodeHash` are `HMAC-SHA256(value, serverPepper)` with the value
lowercase-trimmed and case-folded first. The pepper is a 32-byte secret held in
the secret store, never in the database and never in configuration committed to
source.

**Reason:** the previous design used a plain SHA-256 of the email/username and
justified it as "avoids storing candidate identities in plaintext". A plain hash
of a low-entropy value is reversible in practice: email addresses come from a
small public set, so anyone with a database dump can confirm which addresses have
accounts by hashing a candidate list. The same argument applies to enrollment
codes, which is why that column is now hashed too. High-entropy random values
(`RefreshToken.TokenHash`, `PasswordResetToken.TokenHash`) do **not** need a
pepper and are explicitly not changed.

**Alternatives considered:** storing the identifier encrypted (rejected: the
throttle path then needs decryption on every attempt and the key becomes a
runtime dependency of authentication). A keyed hash with a per-tenant key
(rejected: pre-tenant requests have no tenant).

**Consequences:** rotating the pepper invalidates the throttle history, which is
acceptable and scheduled in a maintenance window; the pepper must be present at
startup or the app fails closed.

**Affected:** identity module, `SECURITY_ARCHITECTURE.md` §4, `.env.example`
(`Auth__IdentifierPepperSource` — a secret-store reference, never the value).

---

## ADR-0037 — Append-only tables carry no `RowVersion` and no `IsCurrent`

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** `audit.AuditLog`, `inventory.StockTransaction`,
`purchasing.ProductPriceHistory`, `events.EventShortageSnapshot`,
`food.RecipeCostSnapshot` and `food.EventFoodCostSnapshot` have **no**
`RowVersion`, no `IsDeleted`, and no update path. "Current" is defined as the
newest row, never stored as a mutable flag.

**Reason:** these tables were simultaneously described as append-only and given
concurrency tokens or a `RowVersion` + `IsCurrent` pair
(`EventFoodCostSnapshot`). That is a contradiction with a real failure mode: two
concurrent recalculations each believe they are writing the current row, and a
`rowversion` on a table the database principal may only `INSERT` into is dead
metadata. `ProductPriceHistory` and `EventShortageSnapshot` had the same
problem.

**Alternatives considered:** keep `IsCurrent` and maintain it with a trigger
(rejected: triggers reintroduce hidden writes on an immutable table, and a
partial unique index on a mutable flag is not a concurrency control). Keep
`RowVersion` "for consistency" (rejected: it invites an update path that the
permission model forbids).

**Consequences:** read paths that need "the current value" add `ORDER BY
<timestamp> DESC` on an index that already exists. A `StockDocumentLine` cannot
carry `BalanceAfter` (a line may split over several lots), so the running balance
lives on the ledger row; `events.Event.CostSnapshotId` is removed for the same
reason (a forward pointer to a row written later).

**Affected:** inventory, purchasing, events, food, audit modules;
`DATABASE_DESIGN.md` §4.

---

## ADR-0038 — Typed foreign keys instead of polymorphic operational references

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Operational integrity tables use typed nullable foreign keys with a
`CHECK` requiring exactly one source, plus a small integer tag for the API:
`inventory.StockDocument` gets `SourceGoodsReceiptId`, `SourceStockCountId`,
`SourceEventConsumptionId` and `SourceType`;
`purchasing.PurchaseApproval` gets `PurchaseRequestId`, `PurchaseOrderId` and
`EntityType`. Polymorphic `(Type, Id)` pairs are permitted **only** in
append-only history tables, where a dangling reference is harmless because the
row is never rewritten.

**Reason:** the previous design described `SourceDocumentId` as "FK NULL (to
purchasing.GoodsReceipt etc., filtered)". SQL Server cannot create a foreign key
whose target table is chosen at runtime, so that column was in fact an
unreferenced `uniqueidentifier`: the database would have accepted a posted stock
document pointing at nothing, and no test could have caught it. The same applied
to `PurchaseApproval.EntityId`, where an approval row not linked to any request
or order would be invisible to the approval workflow.

**Alternatives considered:** a single nullable `SourceTableName` + `SourceId`
(rejected: same problem, less explicit). Separate junction tables per source type
(rejected: more tables for no integrity gain).

**Consequences:** the posting transaction must set the typed column; the CHECK
constraint makes an impossible source combination unrepresentable; the API
serialises the tag for clients while the database relies on the FK.

**Affected:** inventory and purchasing modules, `DATABASE_DESIGN.md` §4.5/§4.6.

---

## ADR-0039 — `CHECK` constraints carry absolute invariants only

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** A `CHECK` constraint is used only when **no** business scenario may
ever violate the rule. Where the product allows a documented exception, the bound
is enforced by a guarded `UPDATE` that references the parent row, a specific
business error, and an audit record. Two current cases:
`purchasing.PurchaseOrderLine.ReceivedQuantity` (over-receipt, TBD-08) and
`events.EventConsumption.ConsumedQuantity` (over-consumption sanity guard).

**Reason:** the previous schema declared
`CHECK (ReceivedQuantity <= OrderedQuantity)` and simultaneously documented that
over-receipt is allowed when `AllowOverReceipt = true`. A `CHECK` cannot be
relaxed from application code, so the database would have rejected a posting the
product is required to accept — the guarantee was unachievable, and the "relax
by application logic" note described something impossible. The same problem
applied to `ConsumedQuantity <= PlannedQuantity * 2`.

**Alternatives considered:** keep the CHECK and drop over-receipt from the product
(rejected: over-receipt is a real warehouse behaviour, already a business rule).
Drop the CHECK and rely on validation alone (rejected: a direct SQL write or a
buggy service would drift). Use a trigger with a session-context bypass
(rejected: bypass context is a security smell in a system this strict).

**Consequences:** the guarantee for these two bounds moves to the service layer
plus integration tests, and the tests MUST cover the allowed exception as well as
the rejection. Absolute invariants (`Quantity > 0`, `OnHandQuantity >= 0`,
`ReservedQuantity <= OnHandQuantity`, the session-scope shape) keep their CHECK
constraints.

**Affected:** purchasing and events modules, `DATABASE_DESIGN.md` §5.8,
`BUSINESS_RULES.md`, `TESTING_STRATEGY.md`.

---

## ADR-0040 — The delivery plan is eight phases

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Delivery is organised as **Phase 0** (blueprint) plus **eight**
implementation phases, defined in `PROJECT_BLUEPRINT.md` §25: 1 Foundation +
Architecture + Database + Security Core; 2 Identity + Tenants + Warehouses +
RBAC; 3 Products + Units + Inventory Engine; 4 Devices + Shared Terminal +
Purchasing; 5 Batches + Expiry + Waste + Transfers + Stocktake + Locations +
Barcode/QR; 6 Events + Recipes + Food Cost + Forecasting; 7 Reports +
Notifications + SaaS + Search + Customization; 8 Frontend PWA + Terminal UI +
Full System Testing + Production.

**Reason:** the previous plan had eleven implementation phases. Authentication
was separated from identity even though neither is testable without the other,
"security hardening" was a phase of its own even though a security regression is
a build failure at every phase (`AGENTS.md` §4), and production infrastructure
was deferred to the last phase, which guarantees it is rushed. Merging them gives
eight phases that each end in a demonstrable, verifiable increment and keeps
infrastructure in Phase 1 where it belongs.

**Alternatives considered:** keeping eleven phases (rejected: phases that cannot
be independently demonstrated). A feature-per-phase backlog (rejected: no
security spine until late).

**Consequences:** every document that referenced "Phase 9/10/11" must be
remapped; `PROJECT_BLUEPRINT.md` §25 carries the explicit legacy mapping.

**Affected:** `PROJECT_BLUEPRINT.md` §25, `ARCHITECTURE.md`,
`TESTING_STRATEGY.md` §2, `DEVELOPMENT_STATUS.md`, `README.md`.

---

## ADR-0041 — Nullable unique-key components get explicit filtered variants

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** Any unique index that includes a nullable column gets an explicit
`WHERE <col> IS NULL` variant for each null shape that must be unique. Affected
in V1: `inventory.Batch` (non-expiry batches),
`inventory.StockCountLine` (non-batched count lines),
`master.UnitConversion` (tenant-wide defaults),
`events.EventRequirement` and `events.EventConsumption` (event-wide, i.e.
`MealTypeId IS NULL`), `events.EventAttendance` (walk-ins),
`food.EventFoodCostSnapshot` (event-level cost, `MealTypeId IS NULL`).
`inventory.InventoryLot` gains a persisted `ExpirySortKey date` column so FEFO is
an index seek instead of a join to `Batch`.

**Reason:** in SQL Server a unique index treats NULLs as *distinct*, so
`(TenantId, EventId, ProductId, MealTypeId)` permits two rows with
`MealTypeId = NULL` — exactly the duplicate an event-wide requirement must
forbid. The design previously relied on "a filtered variant" without ever
listing one, so the duplicate was reachable. Persisting `ExpirySortKey` also
removes the `ISNULL(ExpiryDate,'9999-12-31')` expression from the FEFO path.

**Alternatives considered:** `COALESCE` in an indexed computed column (rejected:
it makes the key non-null and hides the semantics). A sentinel `Guid.Empty` for
"no meal type" (rejected: sentinel values leak into the domain).

**Consequences:** each affected table carries one extra index; the FEFO index
becomes `(TenantId, WarehouseId, ProductId, ExpirySortKey, FirstReceivedAtUtc,
Id)`.

**Affected:** inventory, master, events, food modules, `DATABASE_DESIGN.md` §5.7.

---

## ADR-0042 — SQL Server full text for V1 global search

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** V1 global search uses a SQL Server `FULLTEXT` catalog with the
`Arabic_CI_AS` collation over `master.Product`, `master.ProductBarcode`,
`purchasing.Supplier`, `events.Event` and `master.Category`, plus a trigram
`LIKE` fallback for partial codes. Barcodes are matched by unique index, not by
full text. A dedicated search engine is V2 only if a measured requirement
appears.

**Reason:** the choice was open (TBD-17) with no benchmark and no decision, so
the search feature had no defined implementation. Within the chosen stack
(full-text is in the same engine as the data, one backup, no new service in a
modular monolith) this is the lowest-complexity option that supports Arabic.
The known limitation — `Arabic_CI_AS` does no Arabic stemming or diacritic
normalisation — is recorded rather than hidden.

**Alternatives considered:** Elasticsearch (rejected for V1: a second service to
deploy, secure, back up and monitor, for a feature the scale target does not
require). Client-side search (rejected: breaks the admin use case and leaks the
row count).

**Consequences:** Phase 7 must benchmark Arabic search quality and recall before
the feature is declared done; a trigram index is added only if the benchmark
requires it.

**Affected:** search module, `ARCHITECTURE.md`, `TESTING_STRATEGY.md` §8.

---

## ADR-0043 — The barcode symbology set is the six modelled values

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** V1 supports EAN-13, EAN-8, UPC-A, Code-128, QR and DataMatrix —
exactly the values the `Symbology` enum already models. Validation is
per-symbology (length, check digit) where the symbology is known; a scanned code
of unknown symbology is stored as `Code128` rather than rejected, and a manual
entry is always allowed.

**Reason:** the set was open (TBD-18) while the column already enumerated six
values, so the schema and the requirement disagreed. The six values cover retail
EAN, North American UPC, the internal Code-128 labels a warehouse prints itself,
and the two 2-D symbologies used for receiving and asset labels. Rejecting an
unrecognised scan would block a real receiving workflow.

**Alternatives considered:** Code-39 (rejected: mostly legacy, and rarely used in
this domain). GS1-128 (rejected: it is a Code-128 application identifier, handled
by storing the AI prefix, not a new symbology).

**Consequences:** the scanner normalises the symbology; symbology-specific
validation is unit-tested per value; `Code128` is the documented fallback.

**Affected:** master module, barcode/QR scanning, `DATABASE_DESIGN.md` §4.4.

---

## ADR-0044 — Role grants are enumerated for decisions and derived by rule for reads

**Date:** 2026-09-27 · **Status:** Proposed

**Decision:** the role → permission matrix enumerates the 33 permissions that
determine what a role can *do* (writes, approvals, dangerous actions, and the
sentinel reads that the roles are named after). The remaining 72 codes are
granted by a mechanical derivation rule, stated in `ROLES_PERMISSIONS.md` §3.1.1
as rules D1–D5:

- D1 — the four read tiers grant every non-cost `*.read` / read-style code.
- D2 — `tenant_owner` receives every tenant-scoped code; `tenant_admin`
  receives every tenant-scoped code except an explicit ownership-critical
  exception list.
- D3 — functional ownership: a code is granted to the role whose stated purpose
  owns the resource, and to no other role.
- D4 — cost visibility is never inherited by a read tier.
- D5 — a write never follows a read.

`Stockly.ArchitectureTests` asserts the generated grant set is exactly
reproducible from the permission catalogue plus D1–D5, so the rule and the seed
cannot drift apart silently.

**Reason:** the 105-code catalogue was complete but the matrix listed only 33 of
those codes, so 72 codes — every read permission, every attachment permission,
every purchasing-master write, `admin.search`, `notifications.*` — had no
documented grant anywhere. Phase 1 seeds roles from this design, so the gap was
a real blocker, not a cosmetic omission. Enumerating 105 × 11 = 1155 cells by
hand is unreadable and rots on the first change; a stated rule is checkable,
which an unwritten intention is not.

**Alternatives considered:** leave it implicit and let the implementer decide
(rejected: that is a hidden decision, exactly what this document exists to
prevent). Enumerate the full 105 × 11 matrix (rejected: 1155 hand-maintained
cells is a defect waiting to happen, and most of it is a mechanical function of
five rules). Store the grant set in a spreadsheet as a separate source of truth
(rejected: a third place to keep in sync, and it would drift from the catalogue).

**Consequences:** the role matrix is complete by construction, so no code has an
undefined grant. Two roles are deliberately asymmetric and documented as such:
`viewer` never receives a cost code (D4) and
`inventory.reservations.manage` is seeded but granted to nobody in V1 (D3).
Adding a permission code in V1.1 now requires choosing a rule or a matrix row —
that is a feature, and the architecture test is what enforces it.

**Affected:** `ROLES_PERMISSIONS.md` §3, seeding migration (Phase 1),
`Stockly.ArchitectureTests`, `TESTING_STRATEGY.md` §3.2.

---

## ADR Change Procedure

1. Do **not** edit an accepted ADR's decision text.
2. Create a new ADR: `Supersedes ADR-XXXX`, date, new decision, and **why**.
3. List affected components, data migration impact, API compatibility impact, and
   the regression tests that must be added or changed.
4. Update the affected design documents (`ARCHITECTURE`, `DATABASE_DESIGN`,
   `SECURITY_ARCHITECTURE`, `ROLES_PERMISSIONS`, `BUSINESS_RULES`, `WORKFLOWS`,
   `API_CONVENTIONS`, `TESTING_STRATEGY`).
5. Update `DEVELOPMENT_STATUS.md` and `CHANGELOG.md`.
6. Mark the superseded ADR `Superseded by ADR-YYYY` and keep it in the file
   permanently as history.
