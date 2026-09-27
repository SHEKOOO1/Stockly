# STOCKLY — CHANGELOG

All notable changes to Stockly are recorded here. Trivial formatting changes are
not recorded. Security-relevant and database-relevant changes are **always**
recorded, even in early phases.

**Format:** Keep a Changelog — `Added` / `Changed` / `Deprecated` / `Removed` /
`Fixed` / `Security` / `Database` / `Tests` / `Breaking`.

---

## [Unreleased]

Nothing yet.

---

## [0.2.0] — 2026-09-27 — Phase 0 · Blueprint v1.0 finalisation

Documentation-only. **Still no application code, no migrations, no tests.**
The blueprint reached a final, internally consistent draft and is now
**BLOCKED BY OWNER DECISIONS** rather than "unfinished".

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
- All 43 ADRs remain **Proposed** and take effect only on ratification.

### Changed — counts (supersede the 0.1.0 figures)

| Quantity | 0.1.0 | 0.2.0 |
|---|---|---|
| Entities | 66 across 11 schemas | **66 across 12 schemas** (`ops` split out) |
| Permissions | 107 codes / 10 namespaces | **105 codes / 13 namespaces** (one duplicate removed) |
| System roles | 12 | **11 tenant + 1 platform operator authority** |
| Business error codes | 25 | **27** (26 emitted + reserved `account_locked`) |
| ADRs | 31 | **43** |
| Delivery phases | legacy 18 | **8** (+ legacy `LEGACY-PHASE-MAP`) |
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
- An **empty nested Git repository** exists at `H:\Stockly\Stockly` and is
  untracked (`?? Stockly/`). It is recorded as ISS-04 and left untouched: it is
  either an accidental nested repository or an intended application root, and
  that is an owner decision, not a cleanup task.

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
