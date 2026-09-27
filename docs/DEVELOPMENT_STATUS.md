# STOCKLY — DEVELOPMENT STATUS

> **Read this file first.** It is the current, authoritative state of Stockly.
> If it disagrees with the code, investigate before changing anything.

| Field | Value |
|---|---|
| Last updated | 2026-09-27 |
| Updated by | Blueprint Finalization session (Phase 0) |
| Product | Stockly (ستوكلي) |
| Blueprint version | **1.0.0 — `STOCKLY BLUEPRINT v1.0 — BLOCKED BY OWNER DECISIONS`** |
| Released code version | None — no application code exists |

---

## 1. Current Phase

**Phase 0 — BLUEPRINT FINALIZATION. Design complete, awaiting owner ratification.**

The design work is finished. What remains is not engineering: it is the owner's
ratification of the blueprint and answers to 20 open questions.

## 2. Phase Status

| Item | Status |
|---|---|
| Blueprint drafted | ✅ Complete — **v1.0.0** |
| Blueprint internally consistent | ✅ Verified (see §5) |
| Blueprint reviewed / ratified by owner | ⛔ **NOT DONE — blocking** |
| ADRs ratified | ⛔ 0 of 44 — all still `Proposed` |
| Open owner decisions answered | ⛔ **NOT DONE — 20 open** |
| Phase 1 (implementation) | ⛔ Not started — **must not start** |
| Code written | **None** |
| Migrations written | **None** |
| Tests written | **None** |
| Frontend | **None** |
| Infrastructure | **None** |

## 3. Last Completed Milestone

**Blueprint finalization to v1.0** (2026-09-27): reconciled the master
specification against every detailed design document, resolved the
`InventoryLot`/`StockBalance` unique-index defect, and produced a consistent
`PROJECT_BLUEPRINT.md` v1.0.0.

## 4. Current Work

None. The blueprint finalization task is complete. **No implementation work is
authorised** until the owner ratifies the blueprint (§27) and answers or defers
the 20 open items in §23.1.

## 5. Verified Design Counts (2026-09-27)

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
| Architecture decision records | **44** (`ADR-0001`–`ADR-0044`), all `Proposed` | `DECISIONS.md` |
| Workflows | **19** sections | `WORKFLOWS.md` |
| Gating security test classes | **33** (`S1`–`S33`) | `TESTING_STRATEGY.md` |
| Known security limitations | **9** (`SEC-KNW-01`–`SEC-KNW-09`) | `SECURITY_ARCHITECTURE.md` §15 |
| Implementation phases | **8** (ADR-0040) | `PROJECT_BLUEPRINT.md` §25 |
| Open owner decisions | **20** | `PROJECT_BLUEPRINT.md` §23.1 |
| Disposed design decisions | 4 (`TBD-02`, `TBD-17`, `TBD-18`, `TBD-20`) | `PROJECT_BLUEPRINT.md` §23.2 |
| Disposed safe assumptions | 1 (`TBD-16`) | `PROJECT_BLUEPRINT.md` §23.3 |

Entity distribution: `platform` 6, `identity` 16, `ops` 2, `master` 7,
`inventory` 8, `purchasing` 10, `events` 5, `food` 5, `notifications` 3,
`files` 1, `extensibility` 2, `audit` 1 = **66**.

## 6. Documentation Structure

| File | Purpose | Status |
|---|---|---|
| `docs/PROJECT_BLUEPRINT.md` | Master specification (v1.0.0, 28 sections) | **Final draft — blocked on owner** |
| `docs/ARCHITECTURE.md` | Solution structure, layering, module boundaries, runtime/deployment topology | Final draft |
| `docs/SECURITY_ARCHITECTURE.md` | Threat model, trust boundaries, controls, 9 known limitations | Final draft |
| `docs/DATABASE_DESIGN.md` | 66-entity catalogue, column-level design, FK/index/constraint rules, migration policy | Final draft |
| `docs/ROLES_PERMISSIONS.md` | 105-permission catalogue, 13 namespaces, 11+1 roles, matrix, D1–D5 derivation, escalation controls | Final draft |
| `docs/BUSINESS_RULES.md` | 147 rules across all domains, 27 business error codes | Final draft |
| `docs/WORKFLOWS.md` | 19 end-to-end workflow sections | Final draft |
| `docs/API_CONVENTIONS.md` | REST contract, headers, pagination, idempotency, concurrency, error format, route scopes | Final draft |
| `docs/TESTING_STRATEGY.md` | Test projects, taxonomy, 33 gating security test classes, concurrency/integrity suites, CI gates | Final draft |
| `docs/DEVELOPMENT_STATUS.md` | This file | Current |
| `docs/DECISIONS.md` | ADR-0001 – ADR-0044, all `Proposed` | Final draft |
| `docs/CHANGELOG.md` | Change history | Current |
| `README.md` | Repository overview, documentation reading order | Current |
| `AGENTS.md` | Binding operating rules for AI agents | Final |
| `.gitignore` | Secret/build exclusion rules | Final |
| `.env.example` | Non-secret configuration template | Final |

## 7. Repository State (verified 2026-09-27)

| Check | Result |
|---|---|
| Directory `H:\Stockly` | Exists |
| Application source code | **None** |
| Migrations | **None** |
| Tests | **None** |
| Git repository | **Initialised** — branch `main` |
| Baseline commit | `9121044` "Initial commit" |
| Finalization checkpoint | `949a873` "docs: checkpoint surviving blueprint finalization work" |
| Nested repository `H:\Stockly\Stockly` | **Empty repo, no commits, untracked.** See ISS-04. Do not touch. |
| Unrelated user work destroyed | **None** |

## 8. Technology Detected in the Environment

| Tool | Version | Status |
|---|---|---|
| .NET SDK | 10.0.201 | Available |
| Node.js | v24.14.0 | Available |
| Git | 2.53.0.windows.1 | Available |
| Docker | — | **NOT AVAILABLE** |
| Local SQL Server | — | Not detected |

**Consequence (honest statement):** integration, security, concurrency, and
migration tests require a real SQL Server. They **cannot be executed on this
workstation** until Docker or a local SQL Server is available. They are designed
to run in CI with Testcontainers. **No such test has been run, because none
exists yet.**

## 9. Technology Direction (decided, not yet implemented)

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core Web API, C#, **.NET 10 (LTS)**, EF Core, SQL Server 2022 |
| API | REST, OpenAPI / Swagger, RFC 9457 ProblemDetails (ADR-0027) |
| Frontend | React + TypeScript + Vite, PWA |
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

## 11. Pending Work

### 11.1 Blocking (must happen before Phase 1)

1. **Review and ratify `PROJECT_BLUEPRINT.md` v1.0** and the 44 `Proposed` ADRs
   (`PROJECT_BLUEPRINT.md` §27).
2. **Answer the 20 open owner decisions** in `PROJECT_BLUEPRINT.md` §23.1, or
   explicitly defer each with a recorded decision in `DECISIONS.md`.
3. **Acknowledge the 9 known security limitations** (`SEC-KNW-01`–`SEC-KNW-09`)
   as accepted for V1.
4. **Decide the hosting/deployment target** (cloud provider, on-premise, or
   customer-managed). This affects Docker, backups, and TLS termination.
5. **Decide whether to install Docker on the development workstation** or rely
   solely on CI for DB-backed tests (`TESTING_STRATEGY.md` TEST-TBD-06).
6. **Decide the licence** (README currently `TBD`).
7. **Decide the fate of the empty nested repository** `H:\Stockly\Stockly`
   (ISS-04).

### 11.2 Phase 1 (after approval only)

- Solution scaffold: `Stockly.Domain`, `Stockly.Application`,
  `Stockly.Infrastructure`, `Stockly.Api`, test projects.
- `StocklyDbContext` with 12 schemas and the initial migration.
- Seed: 105-permission catalogue, plan catalogue, 11 tenant roles per tenant
  derived by ADR-0044 rules D1–D5.
- Pipeline: correlation id, security headers, authentication, authorization,
  rate limiting, exception handling, ProblemDetails.
- Docker, Nginx, and CI/CD — infrastructure is a **Phase 1** deliverable.

### 11.3 Later phases

See `PROJECT_BLUEPRINT.md` §25.

## 12. Known Issues

| ID | Issue | Impact | Plan |
|---|---|---|---|
| ISS-01 | Blueprint is unratified | Phase 1 blocked | Owner review |
| ISS-02 | 20 owner decisions unanswered | Some behaviour may change | Owner answers |
| ISS-03 | Docker unavailable locally | DB-backed tests cannot run locally | Install Docker or use CI (TEST-TBD-06) |
| ISS-04 | Empty nested repository at `H:\Stockly\Stockly` (no commits; Git commands fail inside it) | Untracked noise; **must not** be deleted, merged, ignored or committed | Owner decision |
| ISS-05 | No licence file | Legal ambiguity | Owner decision |
| ISS-06 | No hosting/deployment target decided | Affects Docker, backups, TLS termination | Owner decision |

## 13. Known Security Issues

No code exists, so there are **no implemented security defects**.

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

## 14. Tests

| Suite | Exists | Executed | Result |
|---|---|---|---|
| Unit | No | No | N/A |
| Architecture | No | No | N/A |
| Integration | No | No | N/A |
| Security | No | No | N/A |
| Concurrency | No | No | N/A |
| Migration | No | No | N/A |

**No test has been written or run, because no code exists.** No test result is
claimed anywhere in this repository. The 33 gating security classes `S1`–`S33`
in `TESTING_STRATEGY.md` are a **specification of work to be done**, not a
record of tests that passed.

## 15. Build Status

**N/A — no solution file, no projects, nothing to build.** Verified: the
repository contains documentation only.

## 16. Database / Migrations Status

**N/A — no database, no `DbContext`, no migrations.** The design exists in
`DATABASE_DESIGN.md` and is pending approval. No open database question blocks
Phase 1: all 10 `DB-TBD` items are disposed, and each is additive, so a late
owner answer never requires rewriting the V1 schema.

## 17. Infrastructure Status

**None.** No Dockerfiles, no compose files, no Nginx configuration, no CI
pipeline. Designed in `ARCHITECTURE.md` §11; scheduled for **Phase 1** with
hardening in Phase 8 (ADR-0040).

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
   (multi-timezone display is a V2 consideration).
7. The organisation accepts that subscription enforcement is application-level,
   not cryptographically tamper-proof against a modified application build.

## 19. Next Required Action

**Owner ratification of `docs/PROJECT_BLUEPRINT.md` v1.0 and answers to the 20
open decisions in §23.1 of that document.** Until then the blueprint status stays
`STOCKLY BLUEPRINT v1.0 — BLOCKED BY OWNER DECISIONS` and Phase 1 must not start.

## 20. Blocked Items

| Item | Blocked by |
|---|---|
| Phase 1 implementation | Blueprint ratification (§27) |
| Password policy details | TBD-01 |
| Identity providers for V1 | TBD-03 |
| PIN brute-force thresholds | TBD-04 |
| Session timeouts | TBD-05 |
| `RETURN` semantics | TBD-06 |
| Near-expiry window / expired-stock policy | TBD-07 |
| Over-receipt tolerance | TBD-08 |
| Approval threshold model | TBD-09 |
| Costing per serving | TBD-10 |
| Concurrent-session policy per device | TBD-11 |
| Subscription grace period | TBD-12 |
| Billing provider | TBD-13 |
| Audit retention | TBD-14 |
| Report localisation scope | TBD-15 |
| Warehouse sub-locations in V1 vs V1.1 | TBD-19 |
| Multi-currency | TBD-21 |
| In-transit virtual warehouse | TBD-22 |
| External event registration integration | TBD-23 |
| Multi-entity tenants | TBD-24 |
| Reporting scale target | TBD-25 |
| Local DB-backed test execution | Docker not installed |
| Licence | Owner decision (ISS-05) |
| Hosting target | Owner decision (ISS-06) |
| Nested repo `H:\Stockly\Stockly` | Owner decision (ISS-04) |

**No longer blocked** — these were resolved during finalization and must not be
listed as open: password/PIN hashing (TBD-02 → Argon2id, ADR-0008), search
implementation (TBD-17 → SQL Server full text, ADR-0042), barcode symbology set
(TBD-18 → the six modelled values, ADR-0043), offline write behaviour
(TBD-20 → no offline writes, ADR-0021), fractional quantities (TBD-16 → safe
`decimal(18,4)` assumption, DB-TBD-01).
