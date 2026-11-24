# Backend API Contract Gaps — Stockly

Status date: 2026-11-23

## 1. Purpose

This document records the difference between the ratified API contract and the
backend that actually exists. It is an implementation-status artifact, not a new
decision record and not a replacement for `docs/API_CONVENTIONS.md`.

The authoritative sources are:

- `docs/API_CONVENTIONS.md` §§1–12 — base path, headers, endpoint lists, scopes,
  request/response rules and OpenAPI obligations.
- `docs/ROLES_PERMISSIONS.md` §2 — the 105 permission codes in 13 namespaces.
- `docs/WORKFLOWS.md` — end-to-end call sequences.
- `src/Stockly.Api/Program.cs` — the only routes currently mapped by the host.
- `GET /api-docs/openapi.json` — the currently generated document.

## 2. Verified implementation state

The running API maps only:

| Route | Status |
|---|---|
| `GET /health/live` | Implemented; liveness only |
| `GET /health/ready` | Implemented; database connectivity and applied migrations only |
| `GET /api-docs/openapi.json` | Implemented; sanitised document with fixed title/version and no server list |

There are no `/api/v1/**` business controllers, no authentication handlers, no
authorization policies attached to routes, no permission evaluator, no rate
limiter, no localization middleware and no Swagger UI. This is the approved
Phase 1/Phase 2 boundary enforced by
`tests/Stockly.ArchitectureTests/PhaseBoundaryTests.cs`, not a defect to be
silently fixed.

`docs/API_CONVENTIONS.md` §12 requires every operation to document its required
permission, warehouse scope, idempotency requirement, `If-Match` requirement,
rate limit and known error codes. With no business operations mapped, that
contract is entirely unmet today. The generated OpenAPI document contains no
operations to which that metadata could attach.

## 3. Cross-cutting gaps

| Gap | Evidence | Consequence |
|---|---|---|
| No `/api/v1` business surface | `Program.cs` maps `MapControllers()` but no controller exists | Every endpoint list in `API_CONVENTIONS.md` §4 is a design promise only |
| No operation-level OpenAPI metadata | `API_CONVENTIONS.md` §12; generated document has only document info | Permission, warehouse scope, idempotency, `If-Match`, rate limit and known errors are not documented per operation |
| No XML operation documentation | `API_CONVENTIONS.md` §12 requires generated docs from code comments | Future controllers must carry this before the OpenAPI gate can be considered satisfied |
| No Swagger UI in Development/Staging | `API_CONVENTIONS.md` §1; no SwaggerUI package by design | Document endpoint exists; UI does not. Adding the previously rejected vulnerable UI package is not an acceptable fix |
| No request/response DTO catalogue | Design documents give tables and field semantics, not JSON schemas | DTO names, shapes, nullable fields, enum wire values and examples must be designed with each use case; database columns must not be exposed blindly |
| Unknown-field rejection not applied | `API_CONVENTIONS.md` §5.4 | No endpoint exists to enforce or test `400 unknown_field` |
| `TenantId` rejection not applied | `API_CONVENTIONS.md` §2 | No endpoint exists to prove a client-supplied tenant is rejected |
| `warehouseId` scope rule not applied | `API_CONVENTIONS.md` §3: unassigned warehouse → `404` | `ScopeRequirementHandler` exists but is not attached to a route |
| `Idempotency-Key` not enforced | `API_CONVENTIONS.md` §5.1 | Posting operations do not exist yet |
| `If-Match` / `ETag` not enforced | `API_CONVENTIONS.md` §5.1 and API-TBD-06 | No mutable endpoint exists to return an ETag or require `428 precondition_required` |
| Rate-limit headers and numbers absent | `API_CONVENTIONS.md` §§5.1, 10 and API-TBD-04; TBD-04 unresolved | No endpoint can document or emit `X-RateLimit-*`; provisional numbers must not be frozen as a decision |
| Localization contract absent | `API_CONVENTIONS.md` §5.1; ADR-0020 | No `Accept-Language` handling or Arabic/English response selection exists |
| Readiness contract incomplete | `ARCHITECTURE.md` §8 requires outbox backlog in `/health/ready` | Current readiness checks only DB connectivity and migration state |
| Health routes are outside `/api/v1` | `Program.cs` | Correct for probes, but they must not be presented as business API coverage |

## 4. Endpoint-area gaps

The permission mappings below are the intended mappings to use when the owning
phase implements each area. They are not active policy declarations.

### 4.1 Auth and platform scope

| Area | Intended routes | Intended permission/scope source | Gap |
|---|---|---|---|
| Password auth | `API_CONVENTIONS.md` §4.1 | Anonymous or post-credential; `scp` issued in Phase 2 | No login, refresh, logout, tenant selection, password change or reset endpoints |
| Session identity | `GET /api/v1/auth/me` | Authenticated tenant or platform scope | No identity, roles, permissions, warehouses or entitlements projection |
| Platform admin | `API_CONVENTIONS.md` §4.4 | `platform.*` codes; `scp=platform` only | No tenant, plan, subscription, entitlement, usage, audit or session endpoints |
| Platform entitlement path conflict | `API_CONVENTIONS.md` §4.4 uses `PUT /api/v1/platform/tenants/{id}/entitlements/{featureKey}`; `WORKFLOWS.md` platform flow uses `POST .../entitlements` | `platform.entitlements.manage` | Resolve in the owning phase; do not invent a third path |
| Tenant deactivation conflict | `WORKFLOWS.md` uses `POST /api/v1/platform/tenants/{id}/deactivate`; `API_CONVENTIONS.md` §4.4 has no matching route | `platform.tenants.manage` | Route and verb require an owner/API decision before implementation |

### 4.2 Terminal and device flows

| Area | Intended routes | Intended permission/scope source | Gap |
|---|---|---|---|
| Terminal enrollment-code flows | `API_CONVENTIONS.md` §4.2 | Anonymous, device-scoped; enrollment code re-resolved every request | No public-info, identities, PIN challenge, PIN auth, switch-user or heartbeat endpoints |
| Terminal disclosure limits | `API_CONVENTIONS.md` §4.2 and SEC-KNW-08 | Identities returns only `{ membershipId, displayName, avatarRef }` | No implementation or contract tests for the disclosure boundary |
| Tenant device administration | `API_CONVENTIONS.md` §4.3 | `device.read`, `device.manage`, `device.assign_warehouse` | No device endpoints |
| Device route conflicts | `API_CONVENTIONS.md` §4.3 uses `POST /tenant/devices/{id}/suspend|revoke`; `WORKFLOWS.md` also uses `/rotate-code` and `/warehouse` | Device permissions | Normalise these paths before Phase 4 implementation |

### 4.3 Tenant administration

| Area | Intended routes | Intended permission source | Gap |
|---|---|---|---|
| Memberships/users | `API_CONVENTIONS.md` §4.3 | `tenant.users.read` / `tenant.users.manage` | No list, create, update, suspend, reactivate or membership role/warehouse assignment endpoints |
| Roles | `API_CONVENTIONS.md` §4.3 | `tenant.roles.read` / `tenant.roles.manage` | No role list, create or permission replacement endpoints |
| Warehouses | `API_CONVENTIONS.md` §4.3 | `tenant.warehouses.read` / `tenant.warehouses.manage` | No warehouse administrative endpoints |
| Assignments | `API_CONVENTIONS.md` §4.3 | `tenant.assignments.manage` | No assignment endpoints and no effective warehouse-set tests |
| Audit | `API_CONVENTIONS.md` §4.3 | `tenant.audit.read` / `tenant.audit.export` | No audit query/export endpoint; no collector drain call site |
| Settings/subscription | `API_CONVENTIONS.md` §4.3 | `tenant.read`, `tenant.settings.manage`, `tenant.subscription.read` | No tenant settings or own-subscription endpoints |
| Session revocation | `SECURITY_ARCHITECTURE.md` / permission catalogue | `security.sessions.read`, `security.sessions.revoke`, `security.pin.reset`, `security.password.reset`, `security.lockout.manage` | No admin security endpoints |

### 4.4 Master data

| Area | Intended routes | Intended permission source | Gap |
|---|---|---|---|
| Categories | `API_CONVENTIONS.md` §4.5 | `master.categories.read` / `.manage` | No CRUD endpoints or DTOs |
| Units and conversions | `API_CONVENTIONS.md` §4.5 | `master.units.read` / `.manage` | No endpoints, validation rules or DTOs |
| Products | `API_CONVENTIONS.md` §4.5 | `master.products.read` / `.manage` | No CRUD, cost visibility rule or DTOs |
| Barcodes | `API_CONVENTIONS.md` §4.5 | `master.barcodes.manage` | No barcode endpoints; ADR-0033 single-source rule not exposed |
| Reason codes | `API_CONVENTIONS.md` §4.5 | `master.reason_codes.read` / `.manage` | No endpoints; no inventory-linkage tests |
| Search | `API_CONVENTIONS.md` §4.5 | `admin.search` | No products/suppliers/events search endpoint; ADR-0042 full-text design not implemented |

### 4.5 Inventory

| Area | Intended routes | Intended permission source | Gap |
|---|---|---|---|
| Balances and lots | `API_CONVENTIONS.md` §4.6 | `inventory.balances.read`, `inventory.lots.read` | No query endpoints; warehouse assignment → `404` rule untested |
| Stock movements | `API_CONVENTIONS.md` §4.6 | `inventory.stock.in`, `.out`, `.return`, `.transfer`, `.adjust`, `.waste` | No posting endpoints; no `Idempotency-Key`, reason-code or guarded-update behavior |
| Ledger | `API_CONVENTIONS.md` §4.6 | `inventory.ledger.read` | No immutable ledger query endpoint |
| Documents | `API_CONVENTIONS.md` §4.6 | Movement-specific permissions | No documents/post/reverse endpoints |
| Stock counts | `API_CONVENTIONS.md` §4.6 | `inventory.stock.count`, `inventory.stock.count.post` | No count endpoints |
| Metadata | `API_CONVENTIONS.md` §4.6 | Read permissions for referenced code sets | No enum/reason-code/unit metadata endpoint |
| Movement metadata route conflict | `API_CONVENTIONS.md` uses `/inventory/movements/meta`; `WORKFLOWS.md` uses `/inventory/stock-documents/{id}/reverse` while `API_CONVENTIONS.md` uses `/inventory/documents/{id}/reverse` | Movement permissions | Path set requires normalisation before Phase 3/5 implementation |
| Reservations | None in V1 | `inventory.reservations.manage` is seeded to nobody | Correctly absent; TBD-26/ADR-0050 forbids inventing V1 reservation endpoints |

### 4.6 Purchasing

| Area | Intended routes | Intended permission source | Gap |
|---|---|---|---|
| Suppliers and supplier products | `API_CONVENTIONS.md` §4.7 | `purchasing.suppliers.*`, `purchasing.supplier_products.*` | No CRUD endpoints or DTOs |
| Price history | `API_CONVENTIONS.md` §4.7 | `purchasing.prices.read` / `.manage` | No endpoints; ADR-0046 forbids FX fields |
| Requests | `API_CONVENTIONS.md` §4.7 | `purchasing.requests.read/create/manage/approve` | No request lifecycle endpoints |
| Orders | `API_CONVENTIONS.md` §4.7 | `purchasing.orders.read/create/manage/approve` | No order lifecycle endpoints |
| Receipts | `API_CONVENTIONS.md` §4.7 | `purchasing.orders.receive` / `.close` | No partial receiving or close endpoints |
| Route conflicts | `API_CONVENTIONS.md` §4.7 uses `/purchasing/requests`, `/orders`, `/receipts`; `WORKFLOWS.md` uses `/purchase-requests`, `/purchase-orders`, `/goods-receipts` | Purchasing permissions | Resolve before Phase 4 implementation; keep one documented path set |

### 4.7 Events, food, reports and notifications

| Area | Intended routes | Intended permission source | Gap |
|---|---|---|---|
| Events | `API_CONVENTIONS.md` §4.8 | `events.read`, `events.manage` | No event endpoints |
| Attendance/requirements | `API_CONVENTIONS.md` §4.8 | `events.attendance.manage`, `events.requirements.manage` | No endpoints; DTOs and state transitions unspecified |
| Consumption/shortage | `API_CONVENTIONS.md` §4.8 | `events.consumption.*`, `events.shortage.*` | No endpoints or calculation result contract |
| Meal types/recipes/costing | `API_CONVENTIONS.md` §4.8 | `food.*` | No endpoints; costing DTOs and snapshot semantics unspecified |
| Reports | `API_CONVENTIONS.md` §4.8 | `reports.*` and the report-name enum | No report or export endpoints; CSV/PDF/Excel tier split is phase/tier-gated |
| Notifications | `API_CONVENTIONS.md` §4.8 | Notification/push subscription permissions | No notification endpoints or push-subscription contract |

## 5. Do not close these gaps early

1. Do not add authentication, JWT bearer validation, permission seeding, RBAC
   evaluation, lockout or rate limiting to close the gaps in §4. The current
   phase-boundary tests forbid them and the blueprint assigns them to Phase 2.
2. Do not turn the provisional rate-limit values in `API_CONVENTIONS.md` §10 or
   `.env.example` into a frozen contract while TBD-04 is unresolved.
3. Do not derive JSON DTOs directly from the 66 table definitions. The database
   model is not the external contract.
4. Do not choose between conflicting workflow and API-convention paths by
   silent implementation choice. Record the selected path when the owning phase
   begins.
5. Do not expose `ReservedQuantity`, reservations or any reservation endpoint:
   TBD-26/ADR-0050 leave the semantics undecided.

## 6. Status summary

| Expectation | Status |
|---|---|
| `/api/v1` business endpoints from `API_CONVENTIONS.md` §4 | Missing by approved phase boundary |
| OpenAPI operation metadata from `API_CONVENTIONS.md` §12 | Missing; no operations exist |
| JSON DTO schemas from approved docs | Missing; must be designed per phase |
| Per-route permission and warehouse-scope policy | Primitive exists; not attached to any route |
| Cross-cutting headers and RFC 9457 body shape | Implemented at host level |
| Health/liveness/readiness | Liveness and basic readiness implemented; outbox backlog missing |
| Swagger UI | Not implemented; deliberate package/security choice |
| Frontend/PWA | Missing; Phase 8 |
