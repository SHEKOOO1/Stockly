# AGENTS.md — Operating Rules for AI Coding Agents Working on Stockly

This file is binding on any AI coding agent (or human developer) touching this
repository. It encodes the Stockly operating model.

---

## 1. THE REPOSITORY IS THE SOURCE OF TRUTH

**Do not rely on previous chat history.**

At the start of every session you MUST:

1. Read `docs/DEVELOPMENT_STATUS.md`.
2. Read `docs/PROJECT_BLUEPRINT.md`.
3. Read `docs/DECISIONS.md`.
4. Inspect the actual source code, migrations and tests.
5. Run the relevant validation / test commands.
6. Determine the real current state, then act.

If documentation and implementation disagree, **investigate the discrepancy
first**. Never "fix" a discrepancy by silently editing the documentation to
match incorrect code. Never overwrite correct existing work.

## 2. Do not start work that has not been approved

Phase 0 (blueprint) must be reviewed and approved before Phase 1 (implementation)
begins. Unresolved requirements are marked `TBD` in the docs — **do not invent
requirements silently**. Ask, then record the answer as a decision.

## 3. After any meaningful change, update

- `docs/DEVELOPMENT_STATUS.md` (always)
- `docs/CHANGELOG.md` (for meaningful changes)
- `docs/DECISIONS.md` (whenever an architectural decision is made or changed)
- the relevant design document (`ARCHITECTURE`, `DATABASE_DESIGN`,
  `SECURITY_ARCHITECTURE`, `ROLES_PERMISSIONS`, `BUSINESS_RULES`, `WORKFLOWS`,
  `API_CONVENTIONS`, `TESTING_STRATEGY`) when behaviour or design changes
- regression tests wherever a change touches security guarantees

## 4. Security rules

- The backend is authoritative. The frontend is never trusted for identity,
  tenant selection, warehouse authorization, permissions, roles, prices,
  quantities, approval state or any security decision.
- Tenant scope comes from the authenticated session, never from a
  client-supplied header, route value or body field.
- Every warehouse-scoped operation must verify:
  `User → Tenant → Permission → Warehouse Assignment → Resource scope`.
- Never implement security through frontend visibility alone.
- Never log, return, seed or commit: passwords, PINs, PIN hashes, JWTs, refresh
  tokens, encryption keys, connection strings, certificates.
- Do not weaken rate limiting, lockout, token rotation or audit logging to
  "make a test pass".
- Security tests are mandatory. A security regression is a build failure.

## 5. Honest reporting

- **Never fake a test result.** Paste real output or state that it was not run.
- Never claim Docker validation if Docker is unavailable.
- Never claim production readiness without evidence.
- Never hide a known security issue — record it in
  `docs/DEVELOPMENT_STATUS.md` under *Known Security Issues*.

## 6. Git safety

- Do not destroy unrelated user work.
- Do not `reset --hard`, delete branches, or rewrite history unless explicitly
  instructed.
- Stage only intended files. Never commit secrets (`.gitignore` is not a
  substitute for judgement).
- Do not commit unless explicitly asked.

## 7. No AI assistant inside Stockly

Stockly the product must contain **no** AI assistant, chatbot, copilot, or
conversational AI feature. AI is used only in the development process.

## 8. Before declaring a phase complete

```text
Design → Implementation → Unit → Integration → Security → Edge Cases
→ Concurrency → DB Integrity → Performance (if relevant) → Manual scenarios
→ Full regression → Final security review → PASS / FAIL
```

A phase is **not** complete because the project builds.

## 9. Local development environment notes

The current workstation (verified 2026-10-03) has:

| Tool | Status |
|---|---|
| .NET SDK | `10.0.201` available |
| Node.js | `v24.14.0` available |
| Git | `2.53.0.windows.1` available |
| SQL Server | **`localhost\SQLEXPRESS` available** (Windows/integrated authentication) |
| Docker | **NOT available on this workstation** |

Consequence: DB-backed tests **can** be executed here — integration, security and
database-integrity tiers run against the local instance and pass. Two
requirements apply:

- Set `STOCKLY_TEST_SQLSERVER=Server=localhost\SQLEXPRESS;Integrated Security=true;TrustServerCertificate=true`
  before running them. The fixture must fail rather than fall back to a default.
- `sqlcmd` needs `-C` (self-signed certificate) and `-S localhost\SQLEXPRESS -E`.

**Docker-based validation still cannot be run or claimed here.** Local
Docker/Nginx reference scaffolding and a CI workflow now exist, but no container
image or Compose stack has been built or run on this workstation. The formerly
empty `Stockly.ConcurrencyTests` and `Stockly.MigrationTests` tiers now contain
real tests; a green solution run covers them (ISS-10 resolved).

---

## 10. Current state (verified 2026-11-23)

| Field | Value |
|---|---|
| Phase | **Phase 1 — IN PROGRESS, not complete.** Persistence foundation, tenant-isolation enforcement, DB-backed tests, a startable API host and local/CI reference infrastructure are implemented; ISS-09, ISS-10 and ISS-11 are resolved |
| Blueprint status | `STOCKLY BLUEPRINT v1.1 — RATIFIED` (2026-09-30) |
| Code / migrations / tests / frontend / CI | Domain + Application + Infrastructure + API skeletons and 2 migrations (`InitialSchema`, `TenantIsolation`, both applied) exist and build clean · **433 tests** (322 unit, 51 architecture, 33 integration, 20 security, 3 concurrency, 4 migration) all passing · API serves `/health/live`, `/health/ready`, `/api-docs/openapi.json` · local Dockerfile/Compose/Nginx scaffold and CI workflow exist but are **not Docker-validated** · **no frontend, no business endpoints** |
| Entities / permissions / roles | 66 across 12 schemas · 105 codes in 13 namespaces · 11 tenant + 1 platform authority |
| Rules / error codes / ADRs / phases | 147 · 27 (26 emitted + reserved) · **50 ratified `Accepted`** + ADR-0051 *reserved but never written* (ISS-08) + ADR-0052 `Proposed` · 8 |
| Tracked decision items | **26** — 5 decided (TBD-19, 21, 22, 24, 25) · 16 unresolved · 4 design dispositions · 1 safe assumption |
| Ratification 2026-09-30 | Blueprint v1.1 ratified in full · ADR-0001 → ADR-0044 promoted `Proposed` → `Accepted` (ADR-0045 → ADR-0050 were already `Accepted`) · SEC-KNW-01 → SEC-KNW-09 acknowledged as known V1 limitations |
| Owner decisions taken 2026-09-30 | TBD-19 locations → **ADR-0045** · TBD-21 currency → **ADR-0046** · TBD-22 transfers → **ADR-0047** · TBD-24 legal entities → **ADR-0048** · TBD-25 scale target → **ADR-0049** |
| New open item | **TBD-26** stock reservations — split out of TBD-10, deferred, V2 candidate (**ADR-0050**) |
| Database | One canonical `Stockly` on `localhost\SQLEXPRESS`, at head migration (**ADR-0052**). All DB-backed tiers target it |
| Git | Commits `9121044` (baseline), `949a873` (finalization checkpoint), `67b26e6`, `fe2eb7a`; everything since — the v1.1 correction, the ratification, **and all Phase 1 code** — is **uncommitted working-tree changes** (nothing committed without being asked); plus an **empty nested repo** at `H:\Stockly\Stockly`, **ignored by owner decision (ISS-04 — resolved)** — still on disk, never tracked |

Consequences for an agent working here right now:

- **The design work is finished.** The documented final counts are listed in
  `docs/DEVELOPMENT_STATUS.md` §5. If you change any of them, change the count in
  the same commit, in every document that repeats it, and add a `CHANGELOG.md`
  entry. A stale count is a defect, not a cosmetic issue. Note that the ratified
  ADR count is 50, not 44: §27 of the blueprint requires a decision record for
  every decision, and six were recorded on 2026-09-30.
- **Phase 1 is partly implemented and NOT complete.** Do not claim it is done.
- **`ADR-0051` is reserved and must not be reused.** Seven code sites already
  cite it for the tenant-isolation decision (RLS, TenantScope `CHECK`s,
  `SESSION_CONTEXT`), which is implemented and applied but **was never written
  as an ADR or documented** in `DATABASE_DESIGN.md` / `SECURITY_ARCHITECTURE.md`.
  Authoring it is an **owner action** (`DEVELOPMENT_STATUS.md` ISS-08).
- **ADR-0052** (one canonical database for dev and all test tiers) was added
  2026-10-03 as **`Proposed`**. Ratification is an owner action.
- **Do not create new ADRs without a new owner decision** — ADR-0001 – ADR-0050
  are the ratified set.
- **Ratification resolved no TBD.** All 16 stay `TBD`. Do not treat ratification
  as an answer to any of them. `TBD-26` in particular has no answer: hard vs
  soft reservations, release timing and lifecycle are all undecided, and
  `ReservedQuantity` being `0` in V1 must not be read as a soft-reservation
  design.
- **Known open defects, all recorded in `docs/DEVELOPMENT_STATUS.md` §12:**
  ISS-08 (ADR-0051 never written) and ISS-12 (ADR-0052 remains unratified).
  ISS-09 (API startup), ISS-10 (concurrency/migration tiers) and ISS-11
  (`dotnet ef` startup project) are resolved.
- **Do not delete or commit `H:\Stockly\Stockly`.** ISS-04 is **RESOLVED**: the
  owner decided to ignore the empty nested repository through `.gitignore`
  (commit `fe2eb7a`). It remains on disk, untracked and empty — nothing was
  deleted and nothing was ever committed. Do not add it to the project, commit
  it, or remove it without a new owner decision. Git commands inside it still
  fail because the repository has no commits.
- **These documents are UTF-8 and contain Arabic text and typographic
  characters.** Use the `edit` tool for every write. Do not use PowerShell
  `Get-Content -Raw | Set-Content -Encoding UTF8` on them: on Windows PowerShell
  5.1 that round-trip destroys the encoding and silently corrupts the Arabic
  product name and the box-drawing characters. After any write, verify the file
  is non-empty and contains no `U+FFFD` replacement character.
