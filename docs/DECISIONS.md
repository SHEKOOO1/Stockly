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
be filtered; `includeDeleted` requires a permission.

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
