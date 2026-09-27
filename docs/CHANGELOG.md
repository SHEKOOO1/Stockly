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

## [0.1.0] — 2026-09-27 — Phase 0 Bootstrap

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

[Unreleased]: https://example.invalid/stockly/compare/v0.1.0...HEAD
[0.1.0]: https://example.invalid/stockly/releases/tag/v0.1.0
