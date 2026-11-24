# Next Development Steps — Stockly

Status date: 2026-11-23

These steps follow the ratified phase order. They do not answer unresolved TBDs
or promote deferred functionality into Phase 1.

## Immediate Phase 1 completion work

1. **ADR-0051 (owner action).** Author and ratify the tenant-isolation decision
   already cited by seven code sites, then add the corresponding design sections
   to `DATABASE_DESIGN.md` and `SECURITY_ARCHITECTURE.md`. Do not reuse the
   identifier for another decision.
2. **Complete health readiness.** Add the documented outbox-backlog check using
   least-privilege database access that respects the tenant-isolation design.
   Add an integration test that proves degraded readiness when the check fails.
3. **Keep ISS-10 closed.** `Stockly.ConcurrencyTests` and
   `Stockly.MigrationTests` now contain real DB-backed assertions; a future green
   solution run must continue to cover them.
4. **Audit write path.** Drain `IAuditCollector` inside the same transaction as
   the first real use case; do not introduce a background save that can lose
   audit rows.
5. **Route-level scope policy.** Attach `ScopeRequirementHandler.ForRequest` to
   the first business routes when Phase 2/3 is authorized. Resolve the route
   conflicts recorded in `docs/BACKEND_API_CONTRACT_GAPS.md` first.
6. **Infrastructure.** The local Docker/Nginx/Compose reference scaffold and CI
   workflow exist. Validate them where Docker is available, complete the pending
   CI gates, and wait for the ISS-05/ISS-06 owner decisions before choosing a
   licence, hosting target or production TLS source. Docker validation cannot be
   claimed on this workstation.

## Phase 2 boundary

Begin only after the owner confirms the Phase 2 gate and the relevant TBDs:

- authentication and session issuance;
- refresh-token rotation and lockout;
- permission/role seeding and deterministic role derivation;
- RBAC evaluation as the composition of permission and warehouse assignment;
- rate limiting keyed by the existing `ForwardedIpResolver`;
- S1–S12 security tests.

Do not add `AddAuthentication`, `AddJwtBearer`, `UseAuthentication`, seeders or
evaluation services while the current phase-boundary tests forbid them.

## Later phases

- Phase 3: tenant/warehouse/master data and core inventory services.
- Phase 4: device enrolment and PIN login for shared terminals.
- Phase 5: batches, expiry, FEFO, transfers, stocktake and purchasing workflows.
- Phase 6: events and food/recipe costing.
- Phase 7: reports, notifications, SaaS administration, search and customization.
- Phase 8: PWA, shared terminal UI, full S1–S33 suite, hardening and deployment.

## Rules that remain binding

- The backend is authoritative; no client-supplied identity, tenant, warehouse
  authorization, permission, role, price, quantity, cost or approval state.
- Tenant scope comes from the authenticated session only.
- Every warehouse-scoped operation must verify user → tenant → permission →
  warehouse assignment → resource scope.
- No AI assistant in the Stockly product.
- Do not invent answers to unresolved TBDs.
- Do not claim production readiness, Docker validation or test coverage that has
  not actually been executed.
