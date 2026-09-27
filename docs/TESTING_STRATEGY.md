# STOCKLY — TESTING STRATEGY

| Field | Value |
|---|---|
| Document | `TESTING_STRATEGY.md` |
| Version | 1.0.0 |
| Status | FINAL DRAFT — consistent with `PROJECT_BLUEPRINT.md` v1.0 |
| Last updated | 2026-09-27 |
| Related | `SECURITY_ARCHITECTURE.md`, `DEVELOPMENT_STATUS.md` |

---

## 1. Principles

1. **A phase is not complete because the project builds.** Compilation proves
   nothing about tenant isolation or inventory integrity.
2. **Security tests are mandatory and are a build gate.** A security regression
   fails CI.
3. **Tests must prove behaviour against a real SQL Server.** EF Core's in-memory
   provider does not enforce `CHECK` constraints, unique filtered indexes,
   `rowversion`, or transaction isolation — which is exactly where Stockly's
   guarantees live. In-memory/SQLite substitutes are therefore **not** used for
   integration, concurrency, or integrity tests.
4. **Determinism.** No test depends on wall-clock time, ordering, or a shared
   mutable fixture. Each test creates and destroys its own tenant.
5. **Honesty.** If a test class could not run in the current environment, it is
   reported as **not run** — never as passing.

---

## 2. Test projects

| Project | Target | Runs in | Requires SQL Server |
|---|---|---|---|
| `Stockly.UnitTests` | Domain rules, validators, costing maths, FEFO selection, policy evaluation | Every build | No |
| `Stockly.ArchitectureTests` | Layering, module boundaries, naming, forbidden dependencies | Every build | No |
| `Stockly.IntegrationTests` | Use cases against a real DB via `WebApplicationFactory` | CI + pre-merge | **Yes** |
| `Stockly.SecurityTests` | Isolation, IDOR, escalation, brute force, replay, injection | CI + pre-merge + nightly | **Yes** |
| `Stockly.ConcurrencyTests` | Parallel stock mutations, deadlock behaviour, idempotency races | CI + nightly | **Yes** |
| `Stockly.MigrationTests` | Migrations apply from empty and from a previous-release snapshot; asserts the 66 tables, the unique/index catalogue, `CHECK` scope, and composite tenant FKs | CI + release | **Yes** |

### 2.1 Tooling

| Purpose | Tool |
|---|---|
| Test framework | xUnit v3 |
| Assertions | FluentAssertions |
| Mocking | NSubstitute (interfaces only — never EF internals) |
| API host | `Microsoft.AspNetCore.Mvc.Testing` |
| Real SQL Server | **Testcontainers** (`mcr.microsoft.com/mssql/server`) |
| Coverage | coverlet + ReportGenerator, threshold enforced in CI |
| Mutation spot-checks | Stryker.NET (nightly, non-gating overall; **gating for the authorization project** — see TEST-TBD-03) |
| Load | k6 (nightly) |

> **Environment limitation (recorded honestly):** Docker is **not available** on
> the current development workstation. Every DB-backed test class
> (Integration, Security, Concurrency, Migration) therefore **cannot be executed
> on this machine** until Docker or a local SQL Server is installed. CI runs them
> in a container. Do not report these suites as passing locally.

---

## 3. Test taxonomy

### 3.1 Unit tests (no DB)

- Domain invariants: unit conversion cycles, FEFO ordering, wastage maths,
  moving-average formula, shortage and recommendation maths, costing per
  serving, `VarianceQuantity = Counted − Expected`.
- Validators: every `FluentValidation` rule, including boundary values
  (0, negative, 4 vs 5 decimals, `decimal(18,4)` overflow, whitespace, RTL
  characters, control characters, oversized strings).
- Authorization **policy** logic in isolation: `RequirePermission` handler given
  a fake resolver, asserting allow/deny for a matrix of (role, permission,
  warehouse) tuples.
- Redaction helper: given a payload containing `password`, `pin`, `token`,
  `connectionString` keys, assert none appear in the output.
- Business state machines: document status transitions (legal/illegal).

### 3.2 Architecture tests

- `Stockly.Domain` references no other Stockly project.
- `Stockly.Application` does not reference `Stockly.Infrastructure` or `Stockly.Api`.
- No module references another module's internal namespaces.
- Controllers contain no business logic (no direct `DbContext` usage).
- No entity type has a public setter outside its own module.
- No `Password`, `PinHash`, `TokenHash` property is projected into a response DTO
  (a reflection test over all response DTOs).
- Every `Post`/`Patch` controller action is annotated with a required-permission
  metadata attribute or is explicitly listed as anonymous.

### 3.3 Integration tests (real DB)

Per module, at minimum:

- **Happy path** for every use case.
- **Not-found** for a valid-but-wrong-tenant id ⇒ `404`.
- **Validation failure** ⇒ `400` with the documented field paths.
- **Permission denied** ⇒ `403` for a visible resource.
- **Business rule violation** ⇒ `409` with the documented code.
- **Idempotency**: same key twice ⇒ one effect, same response.
- **`If-Match`**: stale ETag ⇒ `412`.
- **Soft delete** behaviour and `includeDeleted` authorization.
- **Audit row written** with the expected action/actor/device/session.
- **Outbox message written** for notifications.

### 3.4 Security tests (mandatory, gating)

Each item below is an automated test class. A new feature that touches any of
these areas MUST ship the corresponding test in the same change.

| # | Test class | Must prove |
|---|---|---|
| S1 | `TenantIsolationTests` | A user of tenant A can neither read nor mutate any tenant B row across **every** module, by guessing GUIDs, by list endpoints, by report endpoints, by search, by export, by attachment download, and by nested resource paths (e.g. `orders/{tenantBOrderId}/lines`) |
| S2 | `WarehouseIsolationTests` | A membership assigned to W1 cannot read/write W2 data even with the correct permission and a valid tenant |
| S3 | `IdorTests` | Enumerating sequential/other IDs yields `404`, never `403` with data; no endpoint returns another tenant's existence |
| S4 | `PrivilegeEscalationTests` | Cannot self-assign a role; cannot grant a role above own authority; cannot edit a system role; cannot create a custom role exceeding own permissions; cannot remove the last owner |
| S5 | `MassAssignmentTests` | Sending `tenantId`, `status`, `approvedBy`, `unitPrice`, `onHandQuantity`, `postedAtUtc`, `isPlatformAdmin` in a body is rejected or ignored |
| S6 | `AuthenticationTests` | Invalid signature, wrong issuer, wrong audience, expired token, `alg: none`, token from a revoked session — all `401` |
| S7 | `TokenReplayTests` | Refresh rotation works; reusing a rotated token revokes the family and is audited; logout invalidates immediately |
| S8 | `PinAuthenticationTests` | Wrong PIN, unknown membership, locked PIN, disabled membership, unassigned warehouse — all produce identical responses; a name selection alone grants nothing |
| S9 | `BruteForceTests` | Progressive delay then lock per device / per membership / per IP; no bypass by changing casing, whitespace, IP headers, or repeated requests |
| S10 | `RateLimitBypassTests` | Rate limits hold across parameter variations, `X-Forwarded-For` spoofing from untrusted hops, and parallel connections |
| S11 | `EnumerationTests` | `/auth/login` and password-reset endpoints are indistinguishable in status, body, and (within tolerance) timing for unknown-user vs wrong-password |
| S12 | `InjectionTests` | SQL injection attempts in every string parameter are stored as literal text and cause no error leakage; `ORDER BY`/sort allowlist rejects unknown fields |
| S13 | `XssTests` | Stored payloads in product names/notes are returned as data and the CSP blocks script execution (API-level assertion on headers + encoding) |
| S14 | `CsrfTests` | Cookie-authenticated state-changing requests without the anti-CSRF token are rejected; CORS does not allow credentialed wildcard |
| S15 | `FileUploadTests` | Disallowed extensions, mismatched magic bytes, oversized files, and double extensions are rejected; storage keys are GUIDs; path traversal in the file name cannot escape the storage root |
| S16 | `InputValidationTests` | Oversized payloads `413`, unknown fields `400`, malformed JSON `400`, invalid enum `400`, deeply nested JSON rejected |
| S17 | `ConcurrencyTests` (security) | Concurrent stock-outs cannot produce a negative balance; no lost update on balance; transfers are atomic across both warehouses |
| S18 | `IdempotencyTests` | Duplicate receipts/stock-outs with the same key create one document; the same key with a different payload ⇒ `409` |
| S19 | `SubscriptionLimitTests` | Creating a user/warehouse beyond the plan limit fails; a read-only tenant cannot write; limits cannot be bypassed with a different payload shape |
| S20 | `AuditTamperTests` | No API exists or accepts a request to update or delete an audit row; non-privileged roles cannot read raw audit JSON |
| S21 | `SecretLeakTests` | Passwords, PINs, hashes, tokens, and keys never appear in responses, logs, or audit payloads; connection strings never surface in error output |
| S22 | `ErrorSafetyTests` | Every error response for every endpoint contains no stack trace, no SQL text, no internal host name |
| S23 | `DeviceSessionTests` | Revoking a device invalidates its sessions immediately; suspending a membership invalidates its sessions; role removal takes effect without token expiry |
| S24 | `EntitlementTests` | Disallowed features are refused server-side regardless of what the client displays |
| S25 | `TokenScopeTests` (ADR-0034) | A `scp=platform` token is rejected by **every** tenant route, including for a user with an active tenant membership; a `scp=tenant` token is rejected by every `/api/v1/platform/**` route; a token with a missing or invalid `scp` is rejected; the `scp` claim is cross-checked against the live `AuthSession` and a tampered claim fails |
| S26 | `PlatformLoginTests` (ADR-0034) | A non-`IsPlatformAdmin` user calling `/platform/auth/login` receives the same response as a wrong password, creates no session row, and writes a `platform.auth.login` audit row; a platform session has `TenantId = NULL` and cannot be used to read tenant data |
| S27 | `TerminalEnrollmentTests` (ADR-0035) | A wrong enrollment code and a correct code with a wrong PIN are indistinguishable in status, body shape, and timing; an unknown code, a revoked device, and a rotated-away code all return the same generic `404`; the code is generated by the server (128-bit, base32url, globally unique) and is rotatable; a list/get endpoint never returns a code, only a masked hint |
| S28 | `TerminalDisclosureTests` (SEC-KNW-08) | `/terminal/sessions/{enrollmentCode}/identities` returns no role, permission, PIN state, or account status; only memberships assigned to that device's warehouse appear; the endpoint is rate limited per IP and per code; a warehouse not assigned to the device yields an empty list, not another tenant's names |
| S29 | `IdentifierHashTests` (ADR-0036) | `LoginAttempt` stores no plaintext identifier; `IdentifierHash` is `HMAC-SHA256(identifier, pepper)`; the application refuses to start when the pepper is missing; an attacker with database read access cannot recover candidate email addresses from the throttle ledger; changing the pepper is a documented, logged rotation procedure |
| S30 | `WarehouseProbingTests` | An unassigned or foreign `warehouseId` returns `404` on **list** endpoints as well as item endpoints, so it cannot be used to probe for another warehouse; `400` is returned only for a malformed value inside an authorized warehouse |
| S31 | `UniqueIndexTests` (ADR-0032) | Every tenant-scoped unique index is created with `TenantId` as the leading column; the global-unique exceptions (`Attachment.StorageKey`, `RefreshToken.TokenHash`, `PasswordResetToken.TokenHash`, `Device.EnrollmentCode`, `PushSubscription.Endpoint`) are verified as intentionally global; a cross-tenant duplicate code is accepted, a same-tenant duplicate is rejected |
| S32 | `AppendOnlyTests` (ADR-0037) | `StockTransaction`, `AuditLog`, `EventConsumption`, and cost snapshots have no `RowVersion`, no `IsCurrent`, no update endpoint, and the application principal has no `UPDATE`/`DELETE` grant; the current event-food snapshot is selected by max `CalculatedAtUtc` |
| S33 | `CheckConstraintScopeTests` (ADR-0039) | Absolute invariants are enforced by `CHECK` (negative `OnHandQuantity` rejected from raw SQL); conditional bounds (over-receipt, over-consumption) are **not** `CHECK` constraints and are instead proven by a guarded-`UPDATE` race test: two concurrent over-receipts cannot both pass the bound |

### 3.5 Concurrency tests

| Scenario | Assertion |
|---|---|
| 100 parallel stock-outs of 1 from a balance of 50 | Exactly 50 succeed; 50 get `insufficient_stock`; final balance = 0; ledger row count = 50; **never negative** |
| 100 parallel receipts of 1 into a balance of 0 | Final balance = 100; ledger count = 100; average cost is correct |
| Parallel receipts with different costs | Moving average equals the expected value computed independently |
| Parallel master-data updates with the same stale ETag | Exactly one succeeds; the rest get `412` |
| Parallel document posting of the same document | Exactly one succeeds; the rest get `document_not_postable` |
| Parallel posting with the same idempotency key | One document; all callers receive an equivalent response |
| Parallel transfers A→B and B→A | Both succeed or one fails cleanly; balances always consistent; no deadlock escape hatch |
| Concurrent stock count posting and stock-out | Count reconciles against its cutoff; no corruption; final balance matches the ledger |
| Long-running transaction contention | Deadlock victim retried and eventually succeeds; the retry counter is bounded and surfaced in telemetry |

All concurrency tests run against a real SQL Server with a realistic isolation
level and **must not** be marked as passing unless they actually executed.

### 3.6 Database integrity tests

- Migrations apply cleanly to an empty database and to a snapshot of the
  previous release.
- `CHECK (OnHandQuantity >= 0)` rejects a direct negative `UPDATE` from raw SQL
  (proving the constraint, not just the application). Conditional bounds
  (over-receipt, over-consumption) are proven absent from the schema instead, and
  proven present in the code by a guarded-update race test (S33, ADR-0039).
- Composite tenant FKs reject a cross-tenant insert.
- Filtered unique indexes reject duplicate active codes and allow reuse after
  soft delete; every tenant-scoped unique index is verified to lead with
  `TenantId`, and each documented global-unique exception is verified to be
  intentionally global (S31, ADR-0032).
- Nullable unique keys use a **filtered** index for each populated variant, and
  a filtered index accepts multiple NULLs where the design requires it
  (ADR-0041) — proven with an insert test per variant, not by inspection.
- `rowversion` changes on every update and the stale ETag is rejected; tables
  documented as append-only have no `rowversion` column at all.
- The application principal cannot `DELETE` or `UPDATE` `audit.AuditLog` or
  `inventory.StockTransaction` (verified by executing the DML and expecting a
  permission error).
- The ledger rebuild job reproduces `StockBalance` and `InventoryLot` exactly
  from `StockTransaction` (property-based, seeded random histories), including
  `IncomingQuantity` sourced from open purchase orders and FEFO ordering by
  `ExpirySortKey`, `FirstReceivedAtUtc`, `Id`.
- Audit redaction is enforced at the database boundary for secret-named fields.

---

## 4. Test data strategy

- A **test data builder** library creates a tenant, users, roles, warehouses,
  devices, products, and sessions in a deterministic way.
- Every test creates its **own tenant**, so tests are independent and can run in
  parallel. There is no shared fixture state.
- Sessions and tokens are minted through the real token service, never forged in
  tests (so token validation is itself covered).
- Security tests use a **two-tenant, two-warehouse, two-role** scaffold:
  `TenantA/{W1,W2}` and `TenantB/{W3}` with users `AdminA`, `ManagerA`,
  `WorkerA`, `AdminB`.
- Seed data uses a fixed RNG seed for reproducibility; no real personal data
  ever enters the repository.
- Sensitive fixtures (passwords, PINs) use documented test-only values and are
  still hashed through the production hasher so hashing is covered.

---

## 5. CI gates

```text
On every push:
  1. dotnet format --verify-no-changes
  2. dotnet build -warnaserror
  3. UnitTests + ArchitectureTests                    (no DB, fast)
  4. Security "static" checks (grep-based secret scan, analyser rules)
  5. dotnet ef migrations has-pending-model-changes   (no DB)

On pull request (requires Docker):
  6. IntegrationTests
  7. SecurityTests
  8. ConcurrencyTests
  9. Coverage threshold (see §6)
 10. Frontend: typecheck, lint, unit tests, build

Nightly:
 11. MigrationTests across all supported upgrade paths
 12. Full security suite incl. rate-limit/soak tests
 13. k6 performance smoke against a seeded dataset
 14. Dependency vulnerability scan (NuGet + npm) with a fail threshold
 15. Container image scan (Trivy)

On release:
 16. Restore-from-backup drill
 17. Migration dry run against a production-sized snapshot
```

Any failure in steps 1–9 blocks the merge. Steps 10–17 block the release.

---

## 6. Coverage policy

Coverage is a **signal, not a goal**. A blanket percentage gate produces
meaningless tests.

| Area | Gate |
|---|---|
| `Stockly.Domain` | ≥ 90% line coverage |
| `Stockly.Application` use cases | ≥ 80% line, **100% of use cases have a happy + authorisation test** |
| Authorization handlers | 100% branch coverage |
| `Stockly.Infrastructure` | ≥ 60% line |
| `Stockly.Api` | 100% of endpoints have an integration test (positive or negative) |
| Security test classes S1–S33 | 100% present and passing |

---

## 7. Definition of done — testing

A module is test-complete when:

```text
[ ] Every use case has an integration test
[ ] Every endpoint has at least one authorised and one unauthorised test
[ ] The matching security test classes pass
[ ] Concurrency tests pass where the module mutates shared state
[ ] Database integrity tests pass
[ ] Audit assertions exist for every sensitive action
[ ] No test was skipped or marked inconclusive to make the build green
[ ] Known gaps are listed in DEVELOPMENT_STATUS.md
```

**A skipped test is a documented failure, not a pass.**

---

## 8. Manual verification scenarios (per phase)

Automated tests do not replace a human walkthrough. Minimum manual scenarios:

1. Terminal: pick a name → wrong PIN → correct error, lockout after N tries.
2. Terminal: switch user mid-session → previous session unusable.
3. Worker in kitchen: issue stock; confirm the cleaning warehouse is invisible.
4. Store keeper: receive a partial PO; confirm the order shows partially received and stock increased exactly once.
5. Purchase manager: approve an order; confirm the requester cannot approve their own.
6. Count: count a shelf, post, confirm adjustments and the ledger.
7. Transfer A→B; confirm both ledgers and balances.
8. Waste: post waste; confirm it appears in the waste report and not the consumption report.
9. Event: enter attendance → derive requirements → calculate shortage → generate a draft request.
10. Chef: build a recipe → cost it → cost an event per serving.
11. Admin: revoke a role → confirm the effect is immediate (no re-login).
12. Suspend a membership → confirm immediate lockout on the terminal.
13. Restore a backup and confirm data integrity (release phase).
14. Platform operator with no tenant membership → sign in at `/platform/auth/login`, create a tenant, then attempt a tenant route with the same token and confirm rejection.
15. Terminal with a rotated enrollment code → confirm the printed old code returns a generic 404 and the new code works.
16. Search a product in Arabic with/without diacritics and a Latin fragment → confirm the full-text index returns it (ADR-0042).

Phase mapping: these scenarios are executed at the end of the delivery phase that
introduces the feature (`PROJECT_BLUEPRINT.md` §26). Scenarios 1–3, 11–12, 14–15
are security scenarios and are **not** waived by automation gaps.

---

## 9. Open testing questions

None of these block implementation; each carries a disposition.

| ID | Question | Disposition |
|---|---|---|
| TEST-TBD-01 | Minimum hardware/CI runner size for the concurrency suite | 4 vCPU / 16 GB / SSD for the integration and concurrency suites. The suite must be deterministic on that class of runner; heavier soak runs are a separate nightly job |
| TEST-TBD-02 | Target dataset size for performance tests | §23 TBD-25 provisional target: ~50 warehouses, ~100 k products, ~10 M ledger rows per tenant, ~200 concurrent users. Seeded by a generator, not by hand |
| TEST-TBD-03 | Mutation testing gate for `Authorization` code specifically | **Yes, for the authorization layer only** (`dotnet stryker` restricted to the authorization project). 100% survived mutants on scope, tenant predicate, and warehouse predicate. Not applied to the rest of the codebase |
| TEST-TBD-04 | PWA offline behaviour test strategy | §23 TBD-20 (ADR-0021): **no offline writes in V1**, so there is nothing to reconcile. The only offline test obligation is that a cached master-data read is never treated as authoritative — a stale read followed by a write must be rejected or refreshed, never silently applied |
| TEST-TBD-05 | Arabic/English PDF golden-file testing for exports | §23 TBD-15 provisional: PDF/Excel Arabic-first arrives in V1.1, so golden-file tests are added in the phase that introduces it, not in Phase 8. CSV (in V1) is tested for encoding, delimiter, and BOM by byte-level assertion, which needs no golden file |
| TEST-TBD-06 | Whether Docker will be installed on the dev workstation, or all DB tests stay CI-only | **CI-only until Docker exists locally.** This workstation has no Docker (see `DEVELOPMENT_STATUS.md` ISS-03). DB-backed tests must not be reported as passing when they did not run; the CI result is the only valid evidence |
