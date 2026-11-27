# Next Development Steps — Stockly

Status date: 2026-11-27

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

### Category-management backend seam (2026-11-27)

The Categories, Units and Suppliers pages have shared in-memory demo stores, and
the latest frontend suite has 100 passing tests in total after the warehouse
operations slice. They remain local-only and must not be treated as an API
contract.

1. In the owning Phase 3 backend work, implement category CRUD, hierarchy and
   archive/restore endpoints/DTOs with tenant scope from the authenticated
   session and `master.categories.read` / `master.categories.manage` enforcement.
   If any category resource is exposed as warehouse-scoped, also verify the
   warehouse assignment and resource scope.
2. Make product counts authoritative on the server, with documented semantics for
   archived products and categories. The demo UI currently derives counts from
   local product state and excludes archived products by default.
3. Preserve product category references on category archive. Archived categories
   must be excluded from selectable lists while remaining displayable as
   archived labels in product views; no cascading reassignment is a frontend
   decision.
4. Update `docs/BACKEND_API_CONTRACT_GAPS.md` and the relevant OpenAPI metadata
   when the operations exist. Only then replace the inactive demo-store seam with
   a real typed client.
5. Do not promote client-side `Category.isActive`, `isDeleted` or `updatedAt`
   fields into a schema decision, and do not open a new ADR or answer a TBD from
   this frontend work.

### Unit-management backend seam (2026-11-27)

The Units page now has a shared in-memory demo store and consistent labels across
product and inventory views. It remains local-only and must not be treated as an
API contract.

1. In the owning Phase 3 backend work, implement unit CRUD and archive/restore
   endpoints/DTOs with tenant scope from the authenticated session and
   `master.units.read` / `master.units.manage` enforcement. If any unit resource
   is exposed as warehouse-scoped, also verify warehouse assignment and resource
   scope.
2. Make usage counts and archive guards authoritative on the server, with
   documented semantics for archived products and units. The demo UI derives
   counts from local product state, excludes archived products by default and
   blocks archive when an active product references the unit as its base or
   purchase unit.
3. Preserve product unit references on unit archive. Archived units must be
   excluded from new selectable lists while remaining displayable as archived
   labels in product views; no cascading reassignment is a frontend decision.
4. Define conversion rules in their owning phase. The demo `unitType`,
   `decimals`, `isActive`, `isDeleted` and `updatedAt` fields are UI state only;
   TBD-26 stock reservations and conversion semantics remain untouched.
5. Update `docs/BACKEND_API_CONTRACT_GAPS.md` and the relevant OpenAPI metadata
   when the operations exist. Only then replace the inactive demo-store seam with
   a real typed client. Do not promote the client's tenant-wide selectable-unit
   rule into a backend scope decision.

### Supplier-management backend seam (2026-11-27)

The Suppliers page now has a shared in-memory demo store and consistent labels
across product and incoming views. It remains local-only and must not be treated
as an API contract.

1. In the owning Phase 5 backend work, implement supplier CRUD and archive/restore
   endpoints/DTOs with tenant scope from the authenticated session and
   `purchasing.suppliers.read` / `purchasing.suppliers.manage` enforcement. If any
   supplier resource is exposed as warehouse-scoped, also verify warehouse
   assignment and resource scope.
2. Make duplicate checks and usage counts authoritative on the server, with
   documented semantics for archived products and suppliers. The demo UI scopes
   duplicate names/codes by organization and derives counts from distinct active
   products whose warehouse settings reference the supplier.
3. Preserve product supplier references on supplier archive. Archived suppliers
   must be excluded from new selectable lists while remaining displayable as
   archived labels in product views; no cascading reassignment is a frontend
   decision.
4. Do not infer supplier currency, payment terms or rating rules from the demo
   fields. Those remain phase/owner-governed design questions.
5. Update `docs/BACKEND_API_CONTRACT_GAPS.md` and the relevant OpenAPI metadata
   when the operations exist. Only then replace the inactive demo-store seam with
   a real typed client. Do not promote the client's organization-scoped
   selectable-supplier rule into a backend scope decision, and do not open a new
   ADR or answer a TBD from this frontend work.

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

### Shared-terminal backend seam (2026-11-27)

The client now has a demo-only shared-terminal gate: active demo user selection,
a format-only 6–12 digit masked preview, route/action gating, switch-user and
sign-out. It is in-memory UI state and must not be treated as authentication.

1. In Phase 2, implement the anonymous enrollment-code identity endpoint from
   `API_CONVENTIONS.md` §12 and `WORKFLOWS.md` §2: return only membership id,
   display name and avatar reference, rate limited and with no role/PIN status.
2. Implement server-side PIN verification with memory-hard hashing, per-device,
   per-membership and per-IP throttling, lockout and audit events. The selected
   name and the client digit count grant nothing.
3. Issue the access token in memory and the refresh token in an HttpOnly cookie
   per ADR-0030. Never put either in `localStorage`, URLs, logs or demo data.
4. Replace `DemoSessionProvider` with the real session adapter only when the
   backend contract and S1–S12 security tests exist. Keep sign-out and
   switch-user clearing all user-visible local state.
5. In Phase 8, retain the responsive touch layout and PWA asset work, then add
   the production service-worker/offline policy as a separate ratified design;
   the current manifest explicitly claims no offline writes.

### Inventory-operations backend seam (2026-11-27)

The Incoming, Outgoing, Adjustments and Stocktaking pages now share a local
in-memory movement/store projection. Receipts, issues, adjustments and counts
update visible balances and history, but they are not authoritative and must not
be treated as an API contract.

1. In owning Phase 3/5 work, implement the documented stock-in, stock-out,
   adjustment and stocktake endpoints/DTOs with tenant scope from the
   authenticated session and warehouse assignment checks. The local
   `StockBalance` projection and request ids are not backend schema decisions.
2. Make balances, guarded decrement/increment, FEFO allocation, immutable ledger
   rows, count cutoff snapshots, reason codes, idempotency and audit/outbox
   effects authoritative on the server. The demo only enforces local UI rules.
3. Resolve the route conflicts and absent inventory endpoints recorded in
   `docs/BACKEND_API_CONTRACT_GAPS.md` before adding a typed client. Do not
   invent reservations, locations, in-transit stock, approval semantics or
   conversion rules from this frontend work.
4. Replace `DemoInventoryStoreProvider` only when the backend contract and
   security/integration tests exist. Retain the local preview labelling until
   then.

### Warehouse-operations backend seam (2026-11-27)

The warehouse operations screen is a local daily-use projection over the shared
demo inventory store. Quick adds/issues, Add Item and low-stock notifications are
not authoritative.

1. In owning Phase 3/5/7 work, map quick quantity changes to the documented
   stock-in/stock-out endpoints/DTOs with tenant scope from the authenticated
   session and warehouse assignment checks.
2. Move minimum-threshold alerts and audience targeting to server-side
   notifications. The demo `roleCode`, route filtering and in-app toast audience
   are UI visibility only and must not become RBAC evaluation.
3. Add initial-balance and duplicate-name/SKU validation to the authoritative
   product/stock contracts before adding a typed client. Do not freeze the demo
   request ids or local balance shapes as backend contracts.
4. Keep the local preview labelling until backend contract, permission, scope,
   idempotency, audit/outbox and security/integration tests exist.

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
