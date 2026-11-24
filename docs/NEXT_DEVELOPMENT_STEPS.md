# Next Development Steps — Stockly

Status date: 2026-11-24

These steps follow the ratified phase order. They do not answer unresolved TBDs
or promote deferred functionality into Phase 1.

## Frontend skeleton handoff decision (2026-11-24)

**Outcome: B. READY WITH DOCUMENTED NON-BLOCKING LIMITATIONS.** Evidence and the
requirement-by-requirement audit are in `docs/DEVELOPMENT_STATUS.md` §3a. The
authorised mocked/static skeleton has since been implemented under `client/`;
see `docs/INITIAL_FRONTEND_IMPLEMENTATION.md` and `DEVELOPMENT_STATUS.md` §3b.

Allowed now:

1. Start a mocked/static frontend skeleton: application shell, routes, layout,
   design system, localization scaffolding, loading/empty/error states and
   clearly labelled mock service adapters.
2. Use `docs/API_CONVENTIONS.md`, `docs/WORKFLOWS.md`,
   `docs/ROLES_PERMISSIONS.md` and `docs/BACKEND_API_CONTRACT_GAPS.md` only as
   design inputs. Mocked JSON shapes are provisional, not frozen DTO decisions.
3. Prepare seams for later real API adapters, but keep them inactive until the
   owning backend phase implements and documents the operations.

Not allowed now:

1. No live login/logout/refresh flow, RBAC evaluation, tenant/warehouse/user
   administration, product/inventory CRUD or outbox side effect may be claimed.
2. No client-supplied identity, tenant, warehouse scope, permission, role,
   price, quantity, cost or approval state may drive a security decision.
3. No route may assume a working `/api/v1/**` endpoint; the API currently maps
   only `/health/live`, `/health/ready` and `/api-docs/{documentName}.json`.
4. No production-readiness, Docker-validated or GitHub Actions-executed claim.
5. No Phase 1 completion claim. The audit drain call site, route-level scope
   policy, concrete outbox producers/handlers and column-level migration
   fidelity remain open.

Before a data-connected frontend can replace mocks:

1. Complete Phase 2 auth, sessions, role/permission seeding, RBAC evaluation and
   rate limiting when the owner confirms that gate.
2. Implement the owning Phase 2/3 business endpoints, DTOs and operation-level
   OpenAPI metadata; resolve the route conflicts recorded in
   `docs/BACKEND_API_CONTRACT_GAPS.md` in their owning phases.
3. Add the first real outbox producer and handler with the first business side
   effect, keeping the write inside the same database transaction.
4. Validate Docker/Compose/Nginx and hosted CI where Docker and GitHub Actions
   are available.
5. Correct the stale `/health/ready` rows in `BACKEND_API_CONTRACT_GAPS.md`
   §§2–3 before treating that document as the handoff contract source; the
   current host is outbox-aware.

## Immediate Phase 1 completion work

1. **Audit write path.** Drain `IAuditCollector` inside the same transaction as
   the first real use case; do not introduce a background save that can lose
   audit rows.
2. **Route-level scope policy.** Attach `ScopeRequirementHandler.ForRequest` to
   the first business routes when Phase 2/3 is authorized. Resolve the route
   conflicts recorded in `docs/BACKEND_API_CONTRACT_GAPS.md` first.
3. **Concrete outbox handlers.** Register `IOutboxMessageHandler` implementations
   when the first business side effect exists; unhandled messages are released
   deliberately, not delivered.
4. **Keep ISS-08 – ISS-12 closed.** ADR-0051/ADR-0052 are ratified, health readiness
   is outbox-aware, and the concurrency/migration tiers contain real DB-backed
   assertions. A future green solution run must continue to cover all of them.
5. **Infrastructure.** The local Docker/Nginx/Compose reference scaffold and CI
   workflow exist; the Dockerfile restore defect is fixed. Validate them where
   Docker is available, complete the pending CI gates, and wait for the
   ISS-05/ISS-06 owner decisions before choosing a licence, hosting target or
   production TLS source. Docker validation cannot be claimed on this workstation.

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
