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

The current workstation (as of Phase 0) has:

| Tool | Status |
|---|---|
| .NET SDK | `10.0.201` available |
| Node.js | `v24.14.0` available |
| Git | `2.53.0.windows.1` available |
| Docker | **NOT available on this workstation** |

Consequence: any test that requires a real SQL Server instance (integration,
concurrency, migration and database-integrity tests) cannot be executed here
until Docker or a local SQL Server is installed. This limitation is tracked in
`docs/DEVELOPMENT_STATUS.md` and `docs/TESTING_STRATEGY.md`. Do not claim such
tests passed on this machine.
