# STOCKLY — DEVELOPMENT STATUS

> **Read this file first.** It is the current, authoritative state of Stockly.
> If it disagrees with the code, investigate before changing anything.

| Field | Value |
|---|---|
| Last updated | 2026-09-27 |
| Updated by | Bootstrap session (Phase 0) |
| Product | Stockly (ستوكلي) |
| Version | 0.1.0 (blueprint only, no released code) |

---

## 1. Current Phase

**Phase 0 — BLUEPRINT**

## 2. Phase Status

| Item | Status |
|---|---|
| Blueprint drafted | ✅ Complete (draft) |
| Blueprint reviewed / approved by owner | ⛔ **NOT DONE — blocking** |
| Open TBDs answered | ⛔ **NOT DONE — 25 open** |
| Phase 1 (implementation) | ⛔ Not started — **must not start** |
| Code written | None |
| Migrations written | None |
| Tests written | None |
| Frontend | None |
| Infrastructure | None |

## 3. Last Completed Milestone

**Repository bootstrap and project-memory documentation** (2026-09-27):
inspected the repository, confirmed it was empty, and created the documentation
structure listed in section 5.

## 4. Current Work

None. The bootstrap task is complete. No implementation work is authorised until
the blueprint is approved.

## 5. Documentation Structure (created in this session)

| File | Purpose | Status |
|---|---|---|
| `docs/PROJECT_BLUEPRINT.md` | Master specification (24 required areas) | Draft |
| `docs/ARCHITECTURE.md` | Solution structure, layering, module boundaries, runtime/deployment topology | Draft |
| `docs/SECURITY_ARCHITECTURE.md` | Threat model, trust boundaries, controls, known limitations | Draft |
| `docs/DATABASE_DESIGN.md` | 66-entity catalogue, column-level design, FK/index/constraint rules, migration policy | Draft |
| `docs/ROLES_PERMISSIONS.md` | Permission catalogue (107 codes), 12 system roles, role matrix, escalation controls | Draft |
| `docs/BUSINESS_RULES.md` | Universal/identity/PIN/organization/inventory/purchasing/event/food/subscription/reporting rules, business error codes | Draft |
| `docs/WORKFLOWS.md` | 17 end-to-end workflows | Draft |
| `docs/API_CONVENTIONS.md` | REST contract, headers, pagination, idempotency, concurrency, error format | Draft |
| `docs/TESTING_STRATEGY.md` | Test projects, taxonomy, 24 gating security test classes, concurrency/integrity suites, CI gates | Draft |
| `docs/DEVELOPMENT_STATUS.md` | This file | Current |
| `docs/DECISIONS.md` | ADR-0001 to ADR-0031 | Draft |
| `docs/CHANGELOG.md` | Change history | Current |
| `README.md` | Repository overview, documentation reading order | Draft |
| `AGENTS.md` | Binding operating rules for AI agents | Final |
| `.gitignore` | Secret/build exclusion rules | Final |
| `.env.example` | Non-secret configuration template | Final |

## 6. Repository State (verified 2026-09-27)

| Check | Result |
|---|---|
| Directory `H:\Stockly` | Exists |
| Existing source code | **None** — the directory was empty |
| Git repository | **Not initialised** |
| Existing migrations | None |
| Existing tests | None |
| Pre-existing user work to preserve | None found |

## 7. Technology Detected in the Environment

| Tool | Version | Status |
|---|---|---|
| .NET SDK | 10.0.201 | Available |
| Node.js | v24.14.0 | Available |
| Git | 2.53.0.windows.1 | Available (repo not yet initialised) |
| Docker | — | **NOT AVAILABLE** |
| Local SQL Server | — | Not detected |

**Consequence (honest statement):** integration, security, concurrency, and
migration tests require a real SQL Server. They **cannot be executed on this
workstation** until Docker or a local SQL Server is available. They are
designed to run in CI with Testcontainers. **No such test has been run, because
none exists yet.**

## 8. Technology Direction (decided, not yet implemented)

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core Web API, C#, **.NET 10 (LTS)**, EF Core, SQL Server 2022 |
| API | REST, OpenAPI / Swagger |
| Frontend | React + TypeScript + Vite, PWA |
| Architecture | Modular Monolith, schema-per-module |
| Infrastructure | Docker, Nginx, HTTPS/TLS, CI/CD |
| Test stack | xUnit, FluentAssertions, WebApplicationFactory, Testcontainers (MSSQL) |

## 9. Completed Work

- Product vision, name (Stockly / ستوكلي), and scope defined.
- Multi-tenant requirement established: tenant isolation, warehouse
  authorization independent of roles, devices bound to warehouses, sessions
  bound to user + device.
- Technology target defined.
- Modular Monolith decision recorded.
- Complete entity catalogue defined (66 entities, column-level for all).
- Permission catalogue and 12-role system matrix defined.
- Business rules, business error codes, and validation catalogue defined.
- 17 operational workflows defined.
- API conventions, including error format, idempotency, and concurrency
  contracts.
- Security architecture, threat model, and controls defined.
- Testing strategy with 24 gating security test classes defined.
- Project-memory documentation mechanism established.
- `AGENTS.md` operating rules established.

## 10. Pending Work

### 10.1 Blocking (must happen before Phase 1)

1. **Review and approve the blueprint.**
2. **Answer the 25 open TBDs** in `PROJECT_BLUEPRINT.md` section 23, or
   explicitly defer each with a recorded decision in `DECISIONS.md`.
3. **Decide the hosting/deployment target** (cloud provider, on-premise, or
   customer-managed). This affects Docker, backups, and TLS termination.
4. **Decide whether to install Docker on the development workstation** or rely
   solely on CI for DB-backed tests (`TESTING_STRATEGY.md` TEST-TBD-06).
5. **Decide the licence** (README currently `TBD`).
6. **Initialise the Git repository** and make the first commit of the
   documentation baseline.

### 10.2 Phase 1 (after approval)

- Solution scaffold: `Stockly.Domain`, `Stockly.Application`,
  `Stockly.Infrastructure`, `Stockly.Api`, test projects.
- `StocklyDbContext` with schema-per-module configuration and the initial
  migration.
- Seed: permission catalogue, plan catalogue, system roles per tenant.
- Pipeline: correlation id, security headers, authentication, authorization,
  rate limiting, exception handling, ProblemDetails.

### 10.3 Later phases

See `PROJECT_BLUEPRINT.md` section 25 for the full phase plan.

## 11. Known Issues

| ID | Issue | Impact | Plan |
|---|---|---|---|
| ISS-01 | Blueprint is unapproved | Phase 1 blocked | Owner review |
| ISS-02 | 25 TBDs unanswered | Design may change | Owner answers |
| ISS-03 | Docker unavailable locally | DB-backed tests cannot run locally | Install Docker or use CI (TEST-TBD-06) |
| ISS-04 | Git not initialised | No history, no CI triggers | Owner decision |
| ISS-05 | No licence file | Legal ambiguity | Owner decision |

## 12. Known Security Issues

No code exists, so there are **no implemented security defects**.

**Inherent design limitations recorded at design time** (see
`SECURITY_ARCHITECTURE.md` section 15 for the full register):

| ID | Limitation | Severity | Plan |
|---|---|---|---|
| SEC-KNW-01 | Device identity is a client claim; physical device possession is not cryptographically proven | Medium | V2 device attestation |
| SEC-KNW-02 | Shared terminals are usable by anyone with physical access to the name list | Medium (inherent to PIN model) | Operational controls + V2 |
| SEC-KNW-03 | Access tokens cannot be revoked before 15-minute expiry (mitigated by per-request session state checks) | Low | Shorter tokens if acceptable |
| SEC-KNW-04 | No MFA in V1 | Medium | V2 TOTP/WebAuthn |
| SEC-KNW-05 | Rate limiting is per-instance in V1 | Low | Shared store before horizontal scaling |
| SEC-KNW-06 | No antivirus scan on attachments in V1 | Low | V1.1 |
| SEC-KNW-07 | No WAF/DDoS service in V1 | Low | Production infrastructure phase |

**Security design decisions already locked in** (no known defect): tenant from
session only, composite tenant foreign keys, guarded atomic stock updates with
`CHECK` constraints, append-only audit, server-side authorization, idempotency
keys, mass-assignment rejection, secret redaction.

## 13. Tests

| Suite | Exists | Executed | Result |
|---|---|---|---|
| Unit | No | No | N/A |
| Architecture | No | No | N/A |
| Integration | No | No | N/A |
| Security | No | No | N/A |
| Concurrency | No | No | N/A |
| Migration | No | No | N/A |

**No test has been written or run, because no code exists.** No test result is
claimed anywhere in this repository.

## 14. Build Status

**N/A — no solution file, no projects, nothing to build.** Verified: the
repository contains documentation only.

## 15. Database / Migrations Status

**N/A — no database, no `DbContext`, no migrations.** The design exists in
`DATABASE_DESIGN.md` and is pending approval.

## 16. Infrastructure Status

**None.** No Dockerfiles, no compose files, no Nginx configuration, no CI
pipeline. All planned in `ARCHITECTURE.md` section 11.

## 17. Security Assumptions (to be confirmed by the owner)

1. Shared terminals are physically inside the warehouse they belong to, and
   access to the physical device is controlled operationally.
2. Selecting a name from a list is not treated as authentication anywhere.
3. PIN secrecy is an operational control; the system compensates with throttling,
   lockout, short sessions, and full auditing.
4. Tenant administrators are trusted within their tenant; cross-tenant access
   requires a platform administrator.
5. Platform operators are trusted but always audited.
6. Each organisation operates warehouses in a single country/timezone per tenant
   (multi-timezone display is a V2 consideration).
7. The organisation accepts that subscription enforcement is application-level,
   not cryptographically tamper-proof against a modified application build.

## 18. Next Required Action

**Owner review of `docs/PROJECT_BLUEPRINT.md` and answers to the 25 TBDs in
section 23 of that document.**

## 19. Blocked Items

| Item | Blocked by |
|---|---|
| Phase 1 implementation | Blueprint approval |
| Password/PIN hashing choice | TBD-02 |
| Rate limit numbers | TBD-04 |
| Approval workflow implementation | TBD-09 |
| Costing per serving | TBD-10 |
| Search implementation | TBD-17 |
| Report localisation | TBD-15 |
| Local DB test execution | Docker not installed |
| Licence | Owner decision |
