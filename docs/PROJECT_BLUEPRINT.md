# STOCKLY (ستوكلي) — PROJECT BLUEPRINT

| Field | Value |
|---|---|
| Document | `PROJECT_BLUEPRINT.md` |
| Status | **STOCKLY BLUEPRINT v1.0 — BLOCKED BY OWNER DECISIONS** |
| Version | 1.0.0 |
| Last updated | 2026-09-27 |
| Phase | 0 — Blueprint (finalised design, awaiting owner ratification) |
| Authority | This document is the master specification. `DATABASE_DESIGN.md`, `ARCHITECTURE.md`, `SECURITY_ARCHITECTURE.md`, `ROLES_PERMISSIONS.md`, `BUSINESS_RULES.md`, `WORKFLOWS.md`, `API_CONVENTIONS.md` and `TESTING_STRATEGY.md` are its detailed expansions. If they conflict, the conflict must be raised and resolved here, not silently in a sub-document. |

**Why this document is marked BLOCKED.** The design work is complete: 66
entities, 105 permissions, 44 architecture decision records, 147 business rules
and 8 delivery phases are specified and internally consistent. Twenty (§23)
questions remain for the product owner. Phase 1 MUST NOT begin until the owner
ratifies this blueprint and answers or explicitly defers each of those twenty
items with a recorded decision in `DECISIONS.md` (§27).

---

## 0. How to read this document

This blueprint is written so that a **new engineer or AI agent, with no access
to any previous conversation**, can start implementing Stockly. Every
unresolved requirement is explicitly marked **`TBD`**. A `TBD` is a question for
the product owner — it must **never** be silently invented or assumed by an
implementer.

Conventions used throughout:

- `MUST` / `MUST NOT` — mandatory, non-negotiable product or security rules.
- `SHOULD` / `SHOULD NOT` — strong recommendations, deviation requires a decision record.
- `TBD` — unresolved, requires owner answer, tracked in `DECISIONS.md` when resolved.
- `ADR-xxxx` — a decision record in `DECISIONS.md`. All 44 are `Proposed`; they
  take effect only on owner ratification (§27).
- `V1` / `V1.1` / `V2` / `V3` — release scope tiers (see §24).

---

## 1. What is Stockly?

**Stockly (ستوكلي)** is a secure, scalable, **multi-tenant Warehouse Management
SaaS** product delivered as a web application with a Progressive Web App (PWA)
front end that is fully usable on shared touch-screen warehouse terminals.

Stockly manages the physical and financial lifecycle of goods inside one or more
warehouses belonging to an organization (a "tenant"): receiving, storing,
issuing, returning, transferring, counting, wasting, purchasing, planning for
events, and costing.

### 1.1 First target deployment

The first real deployment is intended for a conference/residence organization
(such as "بيت العجايبي"). **Stockly MUST NOT be hardcoded around that
organization.** All organization-specific configuration is tenant data or
platform configuration. No tenant name, warehouse name, role, product, or
business rule may be embedded in source code.

### 1.2 Product naming

- Official product name: **Stockly**
- Arabic brand name: **ستوكلي**
- Use **Stockly (ستوكلي)** where both are appropriate.
- The internal repository/solution name is `Stockly`.
- No alternative product names may be introduced.

### 1.3 Explicit non-goals

- No microservices (see `DECISIONS.md` ADR-0001).
- **No AI assistant, chatbot, copilot, or conversational AI inside the product** (ADR-0023).
- No self-service user registration from a terminal.
- No multi-organization (multi-tenant-group) model in V1.
- No offline writes in V1 (ADR-0021, TBD-20).

---

## 2. Conceptual hierarchy

```text
Platform Admin                       (SaaS operator — cross-tenant, audited)
    │
    ├── Plan / Subscription / Entitlements
    │
    ▼
Tenant  (House / Residence / Organization — the data owner)
    │
    ▼
House Admin  (TenantAdmin role inside the tenant)
    │
    ▼
Warehouses  (physical + logical stock locations, tenant-scoped)
    │
    ▼
Users + Roles + Permissions + Warehouse Assignments
    │
    ▼
Devices / Shared Terminals  (belong to a warehouse, NOT to a user)
    │
    ▼
Warehouse Operations  (stock in / out / transfer / count / waste / purchase / consume)
```

### 2.1 The core concepts MUST remain separate

| Concept | Question it answers | Stockly entity |
|---|---|---|
| **User** | *Who am I?* | `AppUser` (global identity) |
| **Tenant Membership** | *Which organization am I acting for?* | `TenantMembership` |
| **Role / Permission** | *What may I do?* | `Role`, `Permission`, `RolePermission`, `MembershipRole` |
| **Warehouse Assignment** | *Where may I do it?* | `MembershipWarehouse` |
| **Tenant** | *Which organization owns the data?* | `Tenant` |
| **Device** | *Which physical terminal am I using?* | `Device` |
| **Session** | *Who is authenticated, and on what scope, right now?* | `AuthSession` |

These MUST NOT be collapsed into one model. In particular:

- A user with role `Worker` in `Kitchen Warehouse` and role `StoreKeeper` in
  `Cleaning Warehouse` is one identity with **two different effective capability
  sets per warehouse**.
- A device is **not** owned by a user. It is bound to a warehouse. Many users
  use it over time; none of them own it.
- A **session scope** is not a role. A platform operator's session has no tenant
  and no membership (ADR-0034, §7.2); a tenant session always has both.

---

## 3. Module map

| # | Module | Purpose | Phase |
|---|---|---|---|
| 1 | **Platform / SaaS** | Tenants, plans, subscriptions, entitlements, usage limits | V1 |
| 2 | **Identity & Security** | Users, memberships, roles, permissions, sessions, tokens, PINs, devices, lockout, rate limiting, audit | V1 |
| 3 | **Organization** | Warehouses, warehouse assignments, devices | V1 |
| 4 | **Inventory** | Categories, units, conversions, products, balances, lots/batches, expiry, FEFO, stock in/out/return/transfer/adjust/waste, stocktake | V1 |
| 5 | **Purchasing** | Suppliers, supplier products, price history, purchase requests, approvals, purchase orders, partial receiving, goods receipts | V1 |
| 6 | **Events / Conferences** | Events, requirements, consumption, attendance, planning, shortage calculation, purchase recommendations | V1 |
| 7 | **Food** | Meal types, recipes, recipe items, food costing, cost per meal/serving, event food cost | V1 |
| 8 | **Notifications** | In-app notifications, PWA push, notification preferences | V1.1 (core V1) |
| 9 | **Reporting** | Inventory, movement, consumption, waste, purchase, supplier, price history, event, user activity, audit, dashboard; PDF/Excel/CSV export | V1 / V1.1 |
| 10 | **Search & Customization** | Global search, custom fields, attachments, OCR-ready file architecture | V1 (global search) / V1.1 (custom fields, attachments) / V2 (OCR) |
| 11 | **Localization** | Arabic + English, RTL + LTR | V1 |

Sub-locations (aisles/bins/racks) are **V1.1**, not V1 (TBD-19, owner decision
recorded in `DATABASE_DESIGN.md` DB-TBD-02).

---

## 4. Entity catalogue (complete, authoritative list)

**66 entities across 12 schemas.** Tenant scope legend: **G** = global (no
tenant), **T** = tenant-scoped, **W** = additionally warehouse-scoped.

| Schema | Count | Owns |
|---|---|---|
| `platform` | 6 | Global identity and the SaaS commercial model |
| `identity` | 16 | Tenants, users, roles, permissions, warehouses, devices, sessions, tokens, PINs, throttle ledger |
| `ops` | 2 | Numbering and reliable side-effect delivery |
| `master` | 7 | Product catalogue and reference data |
| `inventory` | 8 | Balances, lots, documents, the immutable ledger, counts |
| `purchasing` | 10 | Suppliers, requests, approvals, orders, receipts, price history |
| `events` | 5 | Events, requirements, consumption, attendance, shortage |
| `food` | 5 | Meal types, recipes, costing snapshots |
| `notifications` | 3 | In-app, preferences, web push |
| `files` | 1 | Attachment metadata |
| `extensibility` | 2 | Custom field definitions and values |
| `audit` | 1 | Append-only security and business trail |

### 4.1 Platform (`platform` schema)

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `AppUser` | `Id` | G | Global human identity (login credentials, lockout state, `IsPlatformAdmin`) |
| `Plan` | `Id` | G | SaaS subscription plan |
| `PlanFeature` | `Id` | G | Feature entitlement + limit attached to a plan |
| `Subscription` | `Id` | T | Tenant ↔ Plan assignment with period and status |
| `TenantEntitlement` | `Id` | T | Per-tenant override of a plan feature/limit |
| `SubscriptionUsage` | `Id` | T | Metered usage counters for the current period |

### 4.2 Identity & Organization (`identity` schema)

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `Tenant` | `Id` | G (root) | House / residence / organization |
| `TenantSetting` | `Id` | T | Currency, locale, timezone, numbering, low-stock policy |
| `TenantMembership` | `Id` | T | User ↔ tenant relationship + per-tenant status |
| `Role` | `Id` | T | Named bundle of permissions (system or custom), with `AuthorityLevel` |
| `Permission` | `Id` | G | Seeded permission catalogue |
| `RolePermission` | `Id` | T | Role ↔ permission grant |
| `MembershipRole` | `Id` | T | Membership ↔ role assignment (per warehouse scope, see §5) |
| `MembershipRoleWarehouse` | `Id` | T | Junction restricting a `MembershipRole` to specific warehouses |
| `Warehouse` | `Id` | T | Physical/logical stock location |
| `MembershipWarehouse` | `Id` | T | Membership ↔ warehouse authorization (the "WHERE") |
| `Device` | `Id` | W | Shared terminal, bound to exactly one warehouse, addressed by enrollment code |
| `AuthSession` | `Id` | T (nullable `TenantId`) | An authenticated principal on a **scope**; platform sessions have `TenantId IS NULL` (ADR-0034) |
| `RefreshToken` | `Id` | T | Rotating opaque refresh token (hashed, family-tracked) |
| `PinCredential` | `Id` | T | Terminal PIN for a membership (hashed, never returned) |
| `LoginAttempt` | `Id` | G | Throttle ledger for password/PIN authentication |
| `PasswordResetToken` | `Id` | T | Single-use password reset token (hashed) |

### 4.3 Operations (`ops` schema)

`ops` is infrastructure, not a business module. It exists so that numbering and
side-effect delivery are not modelled as tenant business data (ADR-0016).

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `DocumentSequence` | `Id` | T | Per-tenant, per-document-type, per-period atomic number generator (ADR-0026) |
| `OutboxMessage` | `Id` | G (`TenantId` nullable) | Transactional outbox for reliable side effects (ADR-0011) |

### 4.4 Master data (`master` schema)

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `Category` | `Id` | T | Hierarchical product category |
| `Unit` | `Id` | T | Unit of measure, with `DecimalPlaces` |
| `UnitConversion` | `Id` | T | Product/unit conversion factor |
| `Product` | `Id` | T | Sellable/storable item master. **Has no `Barcode` column** (ADR-0033) |
| `ProductBarcode` | `Id` | T | **The single source of truth for barcodes.** One or more per product, with a symbology |
| `ProductWarehouseSetting` | `Id` | W | Min/max stock, reorder point, default supplier, shelf-life override |
| `ReasonCode` | `Id` | T | Controlled reason codes for movements and adjustments |

`Supplier.Rating`, `Event.CostSnapshotId` and `StockDocumentLine.BalanceAfter`
are also removed in V1 — see `DECISIONS.md` for the disposition of each.

### 4.5 Inventory (`inventory` schema) — the core

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `Batch` | `Id` | T | Lot identity: product + batch number + expiry |
| `InventoryLot` | `Id` | W | Stock of one batch in one warehouse (**concurrency-controlled row**) |
| `StockBalance` | `Id` | W | Aggregated on-hand/reserved/incoming/cost per warehouse + product (**concurrency-controlled row**) |
| `StockDocument` | `Id` | W | Movement header (`STOCK_IN`, `STOCK_OUT`, `RETURN`, `TRANSFER`, `ADJUSTMENT`, `WASTE`, `COUNT`) |
| `StockDocumentLine` | `Id` | W | Movement line: product, quantity, unit, lot, unit cost. **No `BalanceAfter` column** |
| `StockTransaction` | `Id` | W | **Immutable posted ledger entry** produced by posting a document line |
| `StockCount` | `Id` | W | Stocktake session header |
| `StockCountLine` | `Id` | W | Expected vs counted quantity, variance, resolution |

`StockReservation` for event planning is **deferred to V2** (TBD-10) — see §24.
`StockBalance.ReservedQuantity` and `InventoryLot.ReservedQuantity` exist and
are always `0` in V1, so the shortage formula is already correct when
reservations arrive.

### 4.6 Purchasing (`purchasing` schema)

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `Supplier` | `Id` | T | Vendor master. **No `Rating` column in V1** |
| `SupplierProduct` | `Id` | T | Supplier ↔ product with SKU, unit, last price, min order qty |
| `ProductPriceHistory` | `Id` | T | Effective-dated price records (source: PO / receipt) |
| `PurchaseRequest` | `Id` | T | Requisition header |
| `PurchaseRequestLine` | `Id` | T | Requisition line |
| `PurchaseApproval` | `Id` | T | Approval decision record for a request or order |
| `PurchaseOrder` | `Id` | W | PO header to a supplier, receiving warehouse |
| `PurchaseOrderLine` | `Id` | W | PO line: product, ordered qty, received qty, unit price |
| `GoodsReceipt` | `Id` | W | Goods receipt header against a PO |
| `GoodsReceiptLine` | `Id` | W | Received qty, lot, expiry, actual unit cost |

`SupplierPerformance` scoring is **deferred to V2**.

### 4.7 Events (`events` schema)

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `Event` | `Id` | T | Conference / gathering with dates, attendance, warehouse |
| `EventRequirement` | `Id` | T | Required product quantity for the event (total or per person) |
| `EventConsumption` | `Id` | W | Planned vs actual consumption per product |
| `EventAttendance` | `Id` | T | Per-person attendance + meal participation |
| `EventShortageSnapshot` | `Id` | W | Computed shortage and recommended purchase quantity at a point in time |

### 4.8 Food (`food` schema)

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `MealType` | `Id` | T | e.g. Breakfast / Lunch / Dinner / Snack |
| `Recipe` | `Id` | T | Recipe with yield, meal type, preparation metadata |
| `RecipeItem` | `Id` | T | Ingredient: product, quantity per yield, unit, wastage % |
| `RecipeCostSnapshot` | `Id` | T | Point-in-time recipe cost, cost per unit, cost per serving |
| `EventFoodCostSnapshot` | `Id` | T | Point-in-time food cost for an event and meal type |

### 4.9 Notifications, Files, Extensibility, Audit

| Entity | PK | Schema | Scope | Purpose |
|---|---|---|---|---|
| `Notification` | `Id` | `notifications` | T | In-app notification for a membership |
| `NotificationPreference` | `Id` | `notifications` | T | Per-membership channel/category preferences |
| `PushSubscription` | `Id` | `notifications` | T | Web-push endpoint + keys for PWA, membership-scoped (ADR-0031) |
| `Attachment` | `Id` | `files` | T | File metadata; binary in object storage |
| `CustomFieldDefinition` | `Id` | `extensibility` | T | User-defined field on an entity |
| `CustomFieldValue` | `Id` | `extensibility` | T | Value of a custom field for a record (JSON justified — see §21) |
| `AuditLog` | `Id` | `audit` | G (`TenantId` nullable) | Append-only security/business audit trail |

**Total V1 entity count: 66.** Full column-level design, keys, indexes and
constraints: `DATABASE_DESIGN.md`.

---

## 5. Tenant scope of every entity — the rules

### 5.1 Mandatory rules

1. Every business entity carries a non-null `TenantId` **except** the explicitly
   global entities listed in §5.2.
2. `TenantId` MUST be populated from the authenticated session, **never** from a
   request body, query string, route value, or header.
3. Every query against a tenant-scoped entity MUST include a tenant predicate.
   This is enforced centrally in the data access layer, not per-handler.
4. Every unique index on a tenant-scoped table MUST include `TenantId` as the
   **leading** column, so uniqueness is enforced **within** a tenant only
   (ADR-0032). Documented global exceptions are limited to `AppUser.Email`,
   `AppUser.UserName`, `Device.EnrollmentCode` and `RefreshToken.TokenHash`.
5. Every foreign key from a tenant-scoped table to a tenant-scoped table MUST be
   constrained so that a row can only reference a row of the **same tenant**
   (composite FKs on `(Id, TenantId)`).
6. A composite index MUST exist on every `*Transaction`, `*Line`, and audit
   table covering `(TenantId, …)` and `(WarehouseId, …)` access patterns.
7. A unique index containing a **nullable** column MUST have an explicit
   filtered variant for each null shape that must be unique, because SQL Server
   treats NULLs as distinct (ADR-0041). This applies to `Batch`, `StockCountLine`,
   `UnitConversion`, `EventRequirement`, `EventConsumption`, `EventAttendance`,
   `EventFoodCostSnapshot` and `InventoryLot`.

### 5.2 Global (non-tenant) entities and why

| Entity | Why it is global |
|---|---|
| `AppUser` | A person is one identity across the platform. Tenant access is granted through `TenantMembership`, which keeps the multi-tenant model clean and avoids duplicate accounts per tenant. (ADR-0003) |
| `Permission` | Platform-wide permission catalogue, seeded by migration, read-only at runtime. |
| `Plan`, `PlanFeature` | SaaS commercial catalogue managed by the platform operator. |
| `LoginAttempt` | Throttle ledger keyed by identity/IP, must work before a tenant is known. |
| `AuditLog` | Security audit must survive tenant deletion and must be queryable platform-wide by platform admins. |
| `OutboxMessage` | Infrastructure concern spanning tenants; `TenantId` is a nullable discriminator, not an ownership field. |

`Tenant` itself is the tenant-scope **root**: it has no `TenantId` column, and
`Subscription` is tenant-scoped (it can only be about one tenant).

### 5.3 Warehouse scope of every entity

Warehouse-scoped (`W`) entities: `Device`, `InventoryLot`, `StockBalance`,
`StockDocument`, `StockDocumentLine`, `StockTransaction`, `StockCount`,
`StockCountLine`, `ProductWarehouseSetting`, `PurchaseOrder`, `PurchaseOrderLine`,
`GoodsReceipt`, `GoodsReceiptLine`, `EventConsumption`, `EventShortageSnapshot`.

Warehouse scope is **separate from role**. Having `inventory.stock.out` does not
grant the right to use it in a warehouse the membership is not assigned to.

### 5.4 User scope where relevant

Audit and transactional entities additionally record `CreatedByMembershipId`,
`CreatedByUserId`, `DeviceId`, `SessionId`, and `CorrelationId` so that any
historical fact can be attributed to a person, a terminal, and a request.

### 5.5 Device/session scope

- `Device` belongs to exactly one `Warehouse` (`WarehouseId` NOT NULL).
- `AuthSession` belongs to exactly one `TenantMembership` **or** is a platform
  session with `TenantId IS NULL` and `MembershipId IS NULL`. A `CHECK`
  constraint enforces that these two shapes are the only possible ones.
- A `StockDocument` records the `DeviceId` and `SessionId` that created it, so
  movement attribution survives session revocation.

---

## 6. Roles (summary)

Full matrix: `ROLES_PERMISSIONS.md`. Tenancy note: system roles are **seeded per
tenant** so each tenant owns its own role rows and can safely customize later.

| Role | Purpose |
|---|---|
| `PlatformAdmin` | SaaS operator; **not a tenant role**. Not a `Role` row at all — it is the `AppUser.IsPlatformAdmin` boolean plus 11 `platform.*` permission codes (ADR-0034, SEC-KNW-09) |
| `TenantOwner` | Highest tenant authority (business + security + settings) |
| `TenantAdmin` | Manages users, roles, warehouses, devices, settings; cannot transfer ownership |
| `WarehouseManager` | Full operational authority over assigned warehouses |
| `StoreKeeper` | Receiving, issuing, transfers, stocktake within assigned warehouses |
| `Worker` | Issue goods / record consumption within assigned warehouses only |
| `Purchaser` | Suppliers, purchase requests, purchase orders (within approval limits) |
| `PurchasingManager` | Approves purchase requests/orders, manages supplier master |
| `KitchenManager` | Recipes, meal types, event food planning, food costing |
| `Accountant` | Prices, cost reports, purchase and supplier reporting; read-only stock |
| `Auditor` | Read access to all data and audit logs; no writes |
| `Viewer` | Read-only operational data; no audit access |

**11 tenant roles + 1 platform authority.** The permission catalogue is
**105 codes in 13 namespaces** (`platform`, `tenant`, `device`, `security`,
`master`, `inventory`, `purchasing`, `events`, `food`, `reports`,
`notifications`, `custom_fields`, `admin`).

Grant construction is **deterministic, not hand-maintained** (ADR-0044): 33
decisive permissions are enumerated explicitly in the matrix, and the remaining
72 grants are derived by rules **D1–D5**. An implementer MUST NOT hand-edit role
grants; the seeding migration derives them.

Authorization model: **roles are named permission bundles; permissions are
global codes.** Effective permission set of a membership in a given warehouse =
union of `MembershipRole` grants where the assignment's warehouse scope
intersects the requested warehouse, filtered by `WarehouseId ∈
MembershipWarehouse`. See `ARCHITECTURE.md` §6 and `ROLES_PERMISSIONS.md`.

---

## 7. Authentication model

Stockly supports **three distinct authentication flows**. They converge on the
same session and authorization model, but platform and tenant sessions are
**separate scopes that never cross** (ADR-0034).

| Flow | Actor | Endpoint | Token scope |
|---|---|---|---|
| A — Password | Office / admin / personal device | `POST /api/v1/auth/login` | `scp=tenant` |
| B — Platform password | SaaS operator | `POST /api/v1/platform/auth/login` | `scp=platform` |
| C — PIN | Shared warehouse terminal | `POST /api/v1/terminal/sessions/{enrollmentCode}/pin-auth` | `scp=tenant` |

### 7.1 Flow A — Password authentication (office / admin / personal devices)

```text
POST /api/v1/auth/login  { emailOrUsername, password, deviceId? }
    │
    ├─ Rate limit: per-IP + per-identity (sliding window)
    ├─ Resolve AppUser by email/username (constant-time, no enumeration)
    ├─ Verify password (Argon2id; PBKDF2-HMAC-SHA256 fallback — §8)
    ├─ Enforce lockout policy (progressive delay → temporary lock)
    ├─ Require tenant membership selection (user may belong to >1 tenant)
    ├─ Create AuthSession (SessionScope = Tenant)
    └─ Issue: access token (JWT, 15 min) + refresh token (opaque, rotated)
```

### 7.2 Flow B — Platform operator authentication

Without this flow the platform cannot be operated, because a platform operator
may belong to **no tenant at all** (ADR-0034).

```text
POST /api/v1/platform/auth/login  { emailOrUsername, password }
    │
    ├─ Identical throttle, password verification, and lockout as §7.1
    ├─ Require AppUser.IsPlatformAdmin = true
    │     false → the same generic invalid_credentials response; no session row
    ├─ Create AuthSession (SessionScope = Platform, TenantId = NULL, MembershipId = NULL)
    ├─ Issue access token (15 min, scp=platform, no tid/mid claim) + refresh cookie
    └─ Audit: platform.auth.login (Success | Failure)
```

**Invariants:** a platform token MUST NOT reach any tenant route, and a tenant
token MUST NOT reach any platform route. There is **no cross-scope switch**: an
operator who also works inside a tenant signs in again on the tenant path.
Failures: `401 invalid_credentials` (identical to a wrong password),
`429 rate_limited`.

### 7.3 Flow C — PIN authentication (shared warehouse terminal)

The terminal is addressed by its 128-bit **enrollment code**, never by
`deviceId` and never by the human-readable `DeviceCode` (ADR-0035). A terminal
URL looks like `https://terminal.stockly/…/k7m2qp9xr4td`.

```text
A. Open the terminal
   GET  /api/v1/terminal/sessions/{enrollmentCode}/public-info
        → { tenantDisplayName, branding, displayNameRequired: true }   // no secrets
   POST /api/v1/terminal/sessions/{enrollmentCode}/pin-challenge
        → { challengeId, expiresAt }   // nonce; defeats replay of a captured request
   GET  /api/v1/terminal/sessions/{enrollmentCode}/identities
        → [ { membershipId, displayName, avatarRef } ]   // names only, NOT authentication

B. User selects their name
   POST /api/v1/terminal/sessions/{enrollmentCode}/pin-auth
        { membershipId, pin, challengeId }
        │
        ├─ Throttle check (per enrollment code + per device + per membership + per IP)
        ├─ Verify the challenge is valid and unused
        ├─ Verify the code resolves to a device that is Active + shared terminal
        ├─ Verify the membership is Active, assigned to the device's warehouse,
        │   and holds a PIN credential
        ├─ Verify PIN (6+ digits, Argon2id hash, constant-time compare)
        ├─ Enforce PIN brute-force protection (progressive delay + lockout)
        ├─ Create AuthSession (SessionScope = Tenant, DeviceId bound)
        └─ Issue: access token + refresh token, session bound to device
```

**Hard rules (security-critical):**

- The selected name is **NOT** authentication. The backend authenticates the
  **PIN** against the selected membership. A client that simply asserts
  `userId = X` gets nothing.
- `GET .../identities` MUST return only the minimum data needed to render a list
  of names. It MUST NOT reveal whether a PIN is set, account status, roles, or
  any permission.
- An unknown enrollment code is **indistinguishable from an empty name list**.
- A terminal MUST NOT support self-registration or account creation.
- PIN responses MUST be uniform in shape and timing regardless of whether the
  membership exists, the PIN is wrong, or the account is locked.
- The disclosure of a staffing list to anyone holding the code is a known,
  accepted, and registered limitation: **SEC-KNW-08**.

### 7.4 Token model

| Token | Type | Lifetime | Storage | Notes |
|---|---|---|---|---|
| Access token | JWT | 15 minutes | Memory only in PWA | Claims: `sub` (user), `scp` (scope), `mid` (membership, tenant scope only), `tid` (tenant scope only), `sid`, `did`, `jti` |
| Refresh token | Opaque random 256-bit | 7 days, rotating | `HttpOnly; Secure; SameSite=Strict` cookie | Stored **hashed**; family-tracked; reuse ⇒ revoke whole family |

The `scp` claim is **mandatory** and is the single switch that makes §7.2's
cross-scope prohibition mechanically enforceable at the edge. Permissions are
**NOT** embedded in the access token. Authorization is resolved server-side per
request from a short-lived cache. See ADR-0013, ADR-0030, ADR-0034.

---

## 8. PIN model

| Aspect | Rule |
|---|---|
| Length | Minimum **6 digits**, maximum 12 digits. Digits only. |
| Storage | Hashed with **Argon2id**; PBKDF2-HMAC-SHA256 only as a migration fallback for legacy hashes. Never reversible. (TBD-02 disposed) |
| Comparison | Constant-time comparison of the hash. No early-exit string compare on plaintext. |
| Reuse | PIN MUST NOT be equal to the user's password, the tenant code, the current year, or a trivially sequential value (`123456`, `111111`, …). Validation at set/change time. |
| Rotation | `PinVersion` on the credential invalidates all sessions authenticated by that PIN. |
| Lockout | Progressive delay after each failure, then temporary lock, then administrative unlock. Per-membership **and** per-device counters. |
| Reset | Only a holder of `security.pin.reset` may reset. Reset by an admin sets `MustChangePin = true`; the terminal forces a PIN change before granting stock permissions. |
| Enumeration | The system MUST NOT reveal whether a PIN exists, or which PINs are weak, in any API response. |
| Logging | PINs MUST NEVER appear in logs, audit records, exception messages, telemetry, or API responses. |

PIN brute-force parameters (exact thresholds) are **TBD-04**.

### 8.1 Password hashing

`Argon2id` is the decision for all new hashes (TBD-02, disposed). `BCrypt` is
**not** used. Existing PBKDF2-HMAC-SHA256 hashes, if any exist, are upgraded to
Argon2id on the next successful login. Work factors are configuration, not code.

---

## 9. Session model

`AuthSession` fields: `Id`, `TenantId` (nullable), `UserId`, `MembershipId`
(nullable), `SessionScope` (`Tenant`, `Platform`), `DeviceId` (nullable),
`Status` (`Active`, `Expired`, `Revoked`, `Superseded`), `AuthenticationMethod`
(`Password`, `Pin`), `PinCredentialId`, `PinVersion`, `RefreshFamilyId`,
`IssuedAtUtc`, `ExpiresAtUtc`, `AbsoluteExpiresAtUtc`, `LastActivityAtUtc`,
`LastSeenAtUtc`, `RevokedAtUtc`, `RevokedReason`, `RevokedByMembershipId`,
`IpAddress`, `UserAgent`, `CorrelationId`, `RowVersion`.

Session rules (MUST):

1. **Scope shape is a `CHECK`, not a convention.** A `Platform` session MUST have
   `TenantId IS NULL AND MembershipId IS NULL`; a `Tenant` session MUST have
   both non-null. This is an absolute invariant, so a `CHECK` constraint is
   correct (ADR-0039).
2. **Idle timeout** — a session with no activity for the configured window is
   invalidated server-side. *(TBD-05)*
3. **Absolute timeout** — maximum session lifetime regardless of activity.
4. **Auto-lock** — the terminal UI locks and clears in-memory state, but
   **server-side invalidation is the real control**.
5. **Explicit logout** — revokes the session and the whole refresh family.
6. **Switch user** — revokes the current session before a new one is created.
7. **Revocation** — an administrator with `security.sessions.revoke` can revoke
   any session; the effect is immediate because every authorized request checks
   session state.
8. **Last-seen** — `LastSeenAtUtc` updated at most once per 60 seconds to avoid a
   write per request.
9. A revoked/expired session's access token MUST stop working immediately
   (checked server-side, not only via token expiry).

---

## 10. Device model

`Device` fields: `Id`, `TenantId`, `WarehouseId`, `EnrollmentCode` (128-bit
random, base32url, rotatable, globally unique), `DeviceCode` (human label, e.g.
`KITCHEN-01`, unique per tenant), `Name`, `Type` (`Kiosk`, `Tablet`, `Desktop`,
`Handheld`), `Status` (`Active`, `Suspended`, `Revoked`), `IsSharedTerminal`,
`LastSeenAtUtc`, `RegisteredAtUtc`, `RegisteredByMembershipId`, `RevokedAtUtc`,
`Notes`, `RowVersion`.

Device rules (MUST):

- A device belongs to **exactly one** warehouse. A terminal physically located
  in the kitchen cannot be used to act on the cleaning warehouse.
- Device registration is an administrative action requiring `device.manage` —
  never a user self-service action.
- Revoking a device revokes **all** active sessions bound to it.
- A device cannot be used to authenticate unless `IsSharedTerminal` is true and
  its status is `Active`.
- Devices are audited: register, suspend, revoke, reassign warehouse, rotate code.

### 10.1 Enrollment code vs device code vs device identity

| Concept | Purpose | Visibility |
|---|---|---|
| `EnrollmentCode` | **Routing** the terminal URL to one device (ADR-0035) | Held by anyone who can see the device, hence rate-limited per code |
| `DeviceCode` | Human label the operator recognises | Tenant-scoped, unique per tenant |
| `DeviceId` | The database key used server-side for attribution | Never a client-supplied authority |

**Device identity is a claim, never proof.** The client asserts a code; the
server resolves it. Proof of physical possession is out of scope for V1: the
security boundary is PIN + rate limiting + short sessions. The consequence —
that an attacker holding the enrollment code and a valid PIN can authenticate
from anywhere on the network — is a registered limitation (**SEC-KNW-01**) and
MUST remain in `DEVELOPMENT_STATUS.md` → Known Security Issues until V2 device
attestation or a per-device client certificate replaces it.

---

## 11. Inventory model

### 11.1 Principle

Stockly inventory is **transaction-based**. A freely editable quantity field is
**never** the source of truth. The authoritative history is the immutable
`StockTransaction` ledger. `StockBalance` and `InventoryLot` are
transactionally-maintained projections that may be rebuilt from the ledger by a
documented, tested rebuild job.

**The rebuild is partial and MUST be documented as partial.** The rebuild job
recomputes `OnHandQuantity`, `AverageUnitCost` and `LastMovementAtUtc` from the
ledger. It restores `IncomingQuantity` from open purchase-order lines and
`LastCountedAtUtc` from posted counts, and leaves `ReservedQuantity` at `0`,
because neither value is derivable from `StockTransaction` alone. The
rebuild-equivalence test therefore asserts equality **per column**, not per row.
Anyone writing a "full rebuild" story that claims row equality is wrong.

### 11.2 Movement types

| Type | Direction | Effect on balance | Notes |
|---|---|---|---|
| `STOCK_IN` | + | Increase | Goods receipt, manual receipt |
| `STOCK_OUT` | − | Decrease | Consumption, issue |
| `RETURN` | + | Increase | Return from consumption; or − if returning to supplier (**TBD-06**) |
| `TRANSFER` | − / + | Move between warehouses | Paired transactions, same reference, one atomic transaction; no in-transit state in V1 (**TBD-22** disposition) |
| `ADJUSTMENT` | + / − | Correct | Requires `ReasonCode` and elevated permission |
| `WASTE` | − | Decrease | Requires `ReasonCode`; separately reportable |
| `COUNT` | Derived | Reconciliation | Produced by posting a `StockCount`; generates `ADJUSTMENT` transactions |

### 11.3 Concurrency model (MUST)

Stock balance mutation MUST use a single atomic conditional `UPDATE` inside an
explicit database transaction:

```sql
UPDATE inventory.StockBalance
   SET OnHandQuantity = OnHandQuantity - @qty,
       RowVersion     = ROWVERSION
 WHERE Id = @balanceId
   AND TenantId = @tenantId
   AND WarehouseId = @warehouseId
   AND OnHandQuantity >= @qty;   -- guarded predicate
```

If `@@ROWCOUNT = 0`, the operation fails with `409 Conflict` and a safe business
error (`insufficient_stock`). This makes the classic race ("stock out 8" vs
"stock out 5" from 10) impossible to resolve to a negative quantity.
Additionally:

- `CHECK (OnHandQuantity >= 0)` and `CHECK (ReservedQuantity >= 0)` constraints
  exist on the table as a last line of defence. These carry **absolute
  invariants only** (ADR-0039); conditional business limits such as an
  over-receipt tolerance are enforced by the guarded `UPDATE` and by the
  application, never by a `CHECK`.
- `RowVersion` (`rowversion`) columns exist on all concurrency-controlled
  tables, and APIs expose them as ETags for optimistic client-side conflict
  detection. **Append-only tables carry no `RowVersion` and no `IsCurrent`**
  (ADR-0037).
- Document posting is all-or-nothing per document, with `SET XACT_ABORT ON`,
  and rows are locked in a consistent order to prevent deadlocks.
- Idempotency keys prevent duplicate postings from retried requests (ADR-0014).
- The unique index that makes the guarded update deterministic is documented in
  §11.5.

### 11.4 Batches, expiry, FEFO

- `Batch` = product + batch number + expiry date. Unique per tenant, with an
  explicit filtered variant for non-expiry batches (ADR-0041).
- `InventoryLot` = batch × warehouse (or product × warehouse for untracked
  products). Owns the per-warehouse quantity.
- `InventoryLot.ExpirySortKey` is **persisted** from `Batch.ExpiryDate`, or
  `9999-12-31` for non-expiry-tracked lots, so FEFO is a single index seek with
  no join to `Batch` (ADR-0041).
- Products flagged `TrackBatch` MUST have a batch on every inbound movement.
- Products flagged `TrackExpiry` MUST have an expiry date on every inbound
  movement; inbound lots already expired MUST be rejected.
- FEFO (First-Expired-First-Out) is the default outbound allocation order:
  ascending `ExpirySortKey`, then `FirstReceivedAtUtc`, then `Id`. Products
  without tracking ignore expiry.
- Near-expiry reporting is a **nightly job** over the FEFO index, not a filtered
  index: a predicate such as `ExpirySortKey < DATEADD(day, 30, SYSUTCDATETIME())`
  is non-deterministic and SQL Server would reject it. On-demand near-expiry
  reports use a parameterised predicate. *(Window and expired-stock policy:
  TBD-07.)*

### 11.5 The two inventory unique indexes that correctness depends on

| Index | Columns | Predicate | Why it exists |
|---|---|---|---|
| `UX_StockBalance` | `(TenantId, WarehouseId, ProductId)` | — | Exactly one balance row per tenant+warehouse+product, so the guarded `UPDATE` in §11.3 hits one row |
| `UX_InventoryLot_NonBatched` | `(TenantId, WarehouseId, ProductId)` | `BatchId IS NULL` | Exactly one lot row for untracked products, which is what the guarded `UPDATE` targets |
| `UX_InventoryLot_Batch` | `(TenantId, WarehouseId, BatchId)` | `BatchId IS NOT NULL` | Exactly one lot row per batch per warehouse |

The two `InventoryLot` predicates are **mutually exclusive**, so together they
admit precisely one lot per (tenant, warehouse, product) for untracked products
and precisely one per (tenant, warehouse, batch) for tracked products. Dropping
`UX_InventoryLot_NonBatched` would leave the guarded update with no single row to
hit, and SQL Server's NULL-distinct semantics would then permit duplicate
untracked lots. `TenantId` leads every one of them (ADR-0032).

---

## 12. Purchasing model

```text
Supplier  ──< SupplierProduct >──  Product
   │                                  ▲
   │                             ProductPriceHistory
   ▼
PurchaseRequest ──< PurchaseRequestLine
   │  (approvals via PurchaseApproval)
   ▼
PurchaseOrder ──< PurchaseOrderLine
   │  (supplier, receiving warehouse, currency, expected date)
   ▼
GoodsReceipt ──< GoodsReceiptLine
   │  (partial receiving allowed; lot + expiry + actual unit cost)
   ▼
Posts to Inventory: STOCK_IN document + StockTransaction + Batch + InventoryLot
                     + StockBalance + ProductPriceHistory
```

Purchasing rules (MUST):

1. A purchase order belongs to exactly one supplier and one receiving warehouse.
2. A PO line cannot be received into a warehouse different from the PO's.
3. Partial receiving is allowed; `ReceivedQuantity` may never exceed
   `OrderedQuantity` plus the tenant's over-receipt tolerance (**TBD-08**).
4. Over-receipt beyond tolerance is rejected unless the order has
   `AllowOverReceipt = true` and the actor holds `purchase.order.manage`.
5. Approval is a server-side state machine. A document cannot be posted or
   received while in `Draft`/`PendingApproval`/`Rejected`/`Cancelled`.
6. Approval thresholds per tenant are configurable (amount, or role-based).
   Exact threshold model is **TBD-09**; thresholds are tenant **data**, never
   code.
7. Unit cost on a goods receipt becomes the latest `ProductPriceHistory` entry
   and updates the moving weighted average (ADR-0017).
8. A goods receipt can be posted as a `STOCK_IN` stock document, so receiving is
   auditable through the same inventory pipeline.
9. `IncomingQuantity` on `StockBalance` is maintained from open PO lines and is
   therefore **not** ledger-rebuildable (§11.1).

---

## 13. Events / conferences model

```text
Event (dates, expected attendees, warehouse, status)
  ├── EventAttendance        (per person, per meal type, attending y/n)
  ├── EventRequirement       (product, required qty, basis: total | per-person × days | per-person × meals)
  ├── EventConsumption       (planned vs actual, links to stock documents)
  ├── Recipe                 (menu → consumption plan)
  └── EventShortageSnapshot  (required − available ⇒ shortage ⇒ recommended purchase qty)
```

Event rules (MUST):

1. Requirements are computed from attendance count, event duration in days,
   meal types served, and per-person consumption factors — all tenant-configurable.
2. Shortage = `required − (on-hand + reserved/committed + inbound on open POs)`.
   `ReservedQuantity` is `0` in V1, so the term is structurally present and
   correct before reservations arrive in V2.
3. A shortage MAY generate a draft `PurchaseRequest`; generating it requires
   `purchasing.requests.create` — recommendations themselves only require
   `events.shortage.read`.
4. Event consumption records link to real `StockDocument` rows so that
   "consumed at event X" is provable, not asserted.
5. Event food cost = Σ (recipe cost per serving × servings) across meal types,
   plus non-recipe items, snapshotted at calculation time.
6. Deleting an event that has posted stock documents is forbidden; events are
   archived instead (status `Cancelled`/`Completed`).
7. Event-wide rows (`MealTypeId IS NULL`) are protected by explicit filtered
   unique indexes, not by the nullable unique key alone (ADR-0041).

Meal type and recipe-driven planning details are **TBD-10**.

---

## 14. Food & costing model

```text
MealType
Recipe (YieldQuantity, YieldUnit, MealType, preparation metadata)
  └── RecipeItem (Product, QuantityPerYield, Unit, WastagePercent)
        ▲
        │ costed against
        ▼
   Product valuation ──► RecipeCostSnapshot (CostPerUnit, CostPerServing)
                          └──► EventFoodCostSnapshot
```

Costing rules (MUST):

1. Recipe cost = Σ over items of `effectiveQuantity × unitCost`, where
   `effectiveQuantity = QuantityPerYield × (1 + WastagePercent/100)`.
2. `CostPerUnit = RecipeCost / YieldQuantity`.
3. `CostPerServing = RecipeCost / servingsPerYield`, where `servingsPerYield` is
   a recipe field (**TBD-10** — servings per unit of yield is a product
   decision, e.g. "1 recipe yields 40 portions").
4. Inventory valuation primary method: **moving weighted average** per
   `(Tenant, Warehouse, Product)` (ADR-0017). Fallback hierarchy for products
   with no purchase history: last purchase price → standard cost → zero, with
   the fallback **explicitly reported in the snapshot** so a reader can tell
   estimated costs from real costs.
5. Cost snapshots are **immutable point-in-time records**. Recalculating creates
   a new snapshot; it never rewrites history.
6. Costing must be computed with a consistent view of price and quantity data
   inside a single transaction to avoid mixed-snapshot results.
7. Tenant currency is **single** per tenant in V1; there is no FX table
   (TBD-21 disposition).

---

## 15. Subscriptions, plans, entitlements

| Entity | Purpose |
|---|---|
| `Plan` | Commercial plan: name, code, price, billing period, status |
| `PlanFeature` | Feature key + limit type (`None`, `Boolean`, `Count`, `Storage`, `Users`, `Warehouses`) + limit value |
| `Subscription` | Tenant ↔ plan, start/end, status (`Trialing`, `Active`, `PastDue`, `Cancelled`, `Expired`), grace period |
| `TenantEntitlement` | Per-tenant override/addition on top of the plan |
| `SubscriptionUsage` | Metered counters (users, warehouses, storage GB, API calls) per period |

Entitlement enforcement (MUST):

1. Entitlements are enforced **server-side** on the operations that create the
   resource. Creating a 5th user when the plan allows 4 MUST fail with a clear
   business error, never with a silent success.
2. The frontend MUST NOT hide features as an enforcement mechanism. Hiding is a
   UX courtesy; the API is the boundary.
3. A tenant with no active subscription, or in `PastDue` past the grace period,
   enters a **read-only** mode: data reads allowed, all writes rejected. Exact
   grace period and expiry behaviour are **TBD-12**.
4. Usage counters are updated in the same transaction as the resource creation
   to avoid over-provisioning under concurrency.
5. Platform admins may grant overrides; every override is audited.

Billing provider integration is **TBD-13**; V1 has no payment processing.

---

## 16. Audit model

`AuditLog` fields: `Id`, `OccurredAtUtc`, `TenantId` (nullable for
platform-level events), `Category` (`Security`, `Identity`, `Inventory`,
`Purchasing`, `Events`, `Administration`, `Platform`), `Severity`, `Action`
(e.g. `stock.out.post`, `auth.pin.failed`, `platform.auth.login`,
`role.permissions.changed`), `Result` (`Success`, `Failure`, `Denied`),
`EntityName`, `EntityId`, `ActorUserId`, `ActorMembershipId`, `DeviceId`,
`SessionId`, `IpAddress`, `UserAgent`, `CorrelationId`, `ReasonCode`,
`BeforeJson`, `AfterJson`, `Quantity`, `UnitId`, `ProductId`.

Audit rules (MUST):

1. Audit records are **append-only**. No update, no delete through the
   application, and no `DELETE` grant for the application database principal on
   `audit.AuditLog` (ADR-0015). Append-only means **no `RowVersion` and no
   `IsCurrent` column** on this table (ADR-0037).
2. The table is **partitioned by month** on `OccurredAtUtc`; older partitions
   are archived to cold storage. Retention is **TBD-14** (provisional 24 months
   online).
3. Ordinary users MUST NOT be able to read, modify, or delete audit records.
   Read access requires `report.audit` or `tenant.audit.read`, and the API never
   returns raw `BeforeJson`/`AfterJson` to non-admin roles.
4. Security-category events are recorded even on **failure** — failed logins,
   PIN brute force, denied authorization, tenant-escape attempts, and rejected
   token replays.
5. Audit MUST NOT contain secrets: no passwords, PINs, PIN hashes, JWTs, refresh
   tokens, encryption keys, connection strings. A redaction helper is applied to
   all before/after payloads and is covered by a test.
6. Audit writes MUST NOT be able to fail the business transaction silently: they
   are written **inside the same database transaction** as the business change.
7. High-volume non-security events may be batched through `ops.OutboxMessage`
   (ADR-0011); **security** events are always written synchronously.

---

## 17. Security guarantees (summary; details in `SECURITY_ARCHITECTURE.md`)

The API MUST defend against, with automated tests: tenant escape, warehouse
escape, IDOR/BOLA, privilege escalation, role/permission tampering, mass
assignment, SQL injection, XSS, CSRF, unsafe file upload, path traversal,
oversized payloads, token replay, session hijacking, PIN brute force, password
brute force, rate-limit bypass, account enumeration, race conditions, duplicate
transactions, replayed requests, information leakage, and unsafe error messages.

Non-negotiable guarantees:

| # | Guarantee |
|---|---|
| G1 | The backend is authoritative for identity, tenant, warehouse, permission, role, price, quantity, and approval state. |
| G2 | Tenant scope is derived from the authenticated session only. |
| G3 | Every warehouse-scoped request verifies user → tenant → permission → warehouse assignment → resource ownership, in that order. |
| G4 | Security is never implemented by frontend visibility. |
| G5 | Inventory cannot be driven negative by concurrent requests. |
| G6 | No secret is ever logged, returned, seeded, or committed. |
| G7 | Errors never leak stack traces, SQL, internal identifiers of other tenants, or the existence of resources the caller cannot see (`404`, not `403`, for cross-tenant existence disclosure). |
| G8 | A revoked session or device stops working immediately, not at token expiry. |
| G9 | All state-changing inventory operations are idempotent and auditable. |
| G10 | A tenant token and a platform token are never interchangeable in either direction. |

### 17.1 Known security limitations (registered, not hidden)

Nine limitations are recorded in `SECURITY_ARCHITECTURE.md` §15 and MUST be
carried into `DEVELOPMENT_STATUS.md` → Known Security Issues until each is
closed by a decision, a test, and a status update: **SEC-KNW-01** (terminal
possession not cryptographically proven), **SEC-KNW-02** (shared terminals usable
by anyone with physical access), **SEC-KNW-03** (access token not revocable
before its 15-minute expiry), **SEC-KNW-04** (no MFA in V1), **SEC-KNW-05**
(per-instance rate limiting), **SEC-KNW-06** (no antivirus scanning in V1),
**SEC-KNW-07** (no WAF/DDoS service in V1), **SEC-KNW-08** (terminal name list
disclosure), **SEC-KNW-09** (`IsPlatformAdmin` is all-or-nothing in V1).

---

## 18. API boundaries

Base path: `/api/v1`. Full contract: `API_CONVENTIONS.md`.

| Boundary | Owner | Rule |
|---|---|---|
| `/api/v1/platform/**` | Platform admin only | `scp=platform` only; separate permission namespace, separate audit category, separate rate limits |
| `/api/v1/tenant/**` | Tenant admin | `scp=tenant` only. Tenant settings, memberships, roles, warehouses, devices, audit |
| `/api/v1/master/**` | Catalog managers | Categories, units, products, barcodes, reasons |
| `/api/v1/inventory/**` | Warehouse operations | Balances, lots, documents, counts, ledger |
| `/api/v1/purchasing/**` | Purchasing | Requests, orders, receipts, prices |
| `/api/v1/events/**` | Event managers | Events, requirements, consumption, shortage |
| `/api/v1/food/**` | Kitchen | Recipes, costing |
| `/api/v1/reports/**` | Reporting | Aggregations and exports |
| `/api/v1/notifications/**` | All users | In-app and push |
| `/api/v1/auth/**` | Anonymous + authenticated | Tenant password authentication |
| `/api/v1/terminal/**` | Anonymous + authenticated | Enrollment-code terminal flows |

Boundary rules (MUST):

1. Each module owns its routes, its DTOs, and its authorization policy. A module
   MUST NOT query another module's tables directly; it calls that module's
   application service.
2. Cross-module writes go through the owning module's use case, so authorization
   and audit are not bypassed.
3. Platform routes are physically separated and MUST NOT be reachable with a
   tenant-scoped token, and vice versa. The `scp` claim is checked at the edge
   before authorization runs.
4. Report endpoints are read-only and MUST NOT leak rows outside the caller's
   warehouse assignment scope.
5. Errors are RFC 9457 ProblemDetails (ADR-0027). A guarded `UPDATE` that
   matches no row is `409`; a precondition-required flow is `428`.

---

## 19. Validation rules (summary; full list in `BUSINESS_RULES.md`)

| Area | Rule |
|---|---|
| Identifiers | Client-supplied IDs MUST be validated for ownership **in the same query that loads the entity**. Never load-then-check. |
| Quantities | Positive, within `decimal(18,4)`, no more than 4 decimal places, no scientific notation. Quantity 0 allowed only for explicit zero-lines in stocktake. |
| Dates | `OccurredAt` MAY NOT be in the future beyond a small clock-skew tolerance. Expiry date MAY NOT be before the batch production date. |
| Names | 1–200 characters, trimmed, no control characters. |
| Barcodes | Unique **per tenant** via `ProductBarcode` (ADR-0033), with a symbology from the modelled set of six (ADR-0043). `Product` has no barcode column. |
| Email | RFC-shaped, normalized to lowercase, unique globally. |
| PIN | 6–12 digits, strength rules from §8. |
| Password | Policy is **TBD-01** (minimum length, complexity, breach-list check, rotation). Hashing is decided: Argon2id (§8.1). |
| Payload size | Request body limit 1 MB default; export/upload limits explicit per endpoint. |
| Unknown fields | Unknown JSON properties are **rejected** (prevents mass assignment and client/contract drift). |
| Enums | Unknown enum values are rejected, never silently defaulted. |
| Tenant-supplied data | Tenant id, price, cost, approval state, role, permission fields are **stripped/ignored** if present in request bodies. |

---

## 20. Concurrency strategy (summary; details in §11.3 and `ARCHITECTURE.md`)

| Mechanism | Where used |
|---|---|
| Explicit DB transaction + guarded `UPDATE` | All inventory mutations (the only reliable pattern) |
| `rowversion` + ETag/`If-Match` | Master data and document header edits |
| Unique indexes as concurrency guards | `DocumentSequence`, `StockBalance`, `InventoryLot`, idempotency keys, barcode uniqueness |
| `SELECT … WITH (UPDLOCK, HOLDLOCK)` | Read-then-write sequences that cannot be expressed as a single guarded `UPDATE` |
| `IsolationLevel.ReadCommitted` by default | Raise to `Serializable`/`RepeatableRead` per use case, never `ReadUncommitted` |
| `Idempotency-Key` | Document posting, goods receipts, stock count submission |
| Consistent lock ordering + bounded retry | Deadlock avoidance and deadlock-victim retry (max 3 attempts with jitter) |
| Optimistic retry in the outbox worker | Notification fan-out |

**MUST NOT** use: distributed locks, in-process locks as correctness guarantees,
application-level read-then-write without a guard, or `ReadUncommitted`.

---

## 21. File / attachment strategy

| Aspect | V1 | Later |
|---|---|---|
| Storage | Local filesystem behind an `IFileStore` abstraction (volume-mounted) | S3-compatible object storage |
| Metadata | `Attachment` table: tenant, entity, entity id, file name, content type, size, SHA-256, storage key, uploaded by | + scan status, version, retention |
| Naming | Random GUID storage key; original name is metadata only | |
| Validation | Extension **and** content-type allowlist, magic-byte sniffing, size limit, image re-encode for images | + malware scanning (ClamAV) — **SEC-KNW-06** |
| Serving | Authenticated, authorized download endpoint that re-checks entity permission; `Content-Disposition: attachment`; `X-Content-Type-Options: nosniff` | CDN signed URLs |
| Path traversal | Storage key is a server-generated GUID — the client file name is NEVER used to build a path | |
| OCR | Architecture reserves `Attachment.OcrStatus` and `ExtractedText` fields so OCR can be added without breaking the model | OCR pipeline in V2 |

Custom fields are the **only** justified JSON storage in V1: user-defined schemas
cannot be modelled relationally ahead of time. Every other business concept is
relationally modelled.

---

## 22. Localization strategy

| Layer | Approach |
|---|---|
| UI strings | Frontend i18n resources (`ar`, `en`); backend never returns UI text |
| Entity names | Master data carries `Name` (canonical) and optional `NameAr`; API returns the requested culture and falls back to `Name` |
| Enums | Returned as stable codes (`stock_out`), never as translated text; the frontend maps code → label |
| Numbers / currency | Backend sends raw decimal + ISO currency code; formatting happens in the frontend using the tenant locale |
| Dates | Backend sends UTC ISO-8601; frontend renders in the tenant timezone and locale |
| RTL/LTR | Purely a frontend concern; backend is direction-agnostic |
| Audit / ledger | Reason codes are tenant-configurable, therefore translatable per tenant |
| Reports / PDF / Excel | Scope is **TBD-15** (Arabic, English, or both from day one) |

The backend MUST NOT store pre-translated sentences. Master data names MAY be
translated because they are tenant data, not product strings.

---

## 23. Open requirements register

The 25 items originally raised are **disposed as follows**. Nothing here is a
silent open question, and nothing here may be invented by an implementer.

| Disposition | Count | Items |
|---|---|---|
| **Owner decision — UNRESOLVED** | **20** | TBD-01, TBD-03, TBD-04, TBD-05, TBD-06, TBD-07, TBD-08, TBD-09, TBD-10, TBD-11, TBD-12, TBD-13, TBD-14, TBD-15, TBD-19, TBD-21, TBD-22, TBD-23, TBD-24, TBD-25 |
| **Design decision — disposed** | 4 | TBD-02, TBD-17, TBD-18, TBD-20 |
| **Safe assumption — disposed** | 1 | TBD-16 |
| **Total** | **25** | |

### 23.1 Unresolved owner decisions (block Phase 1)

| ID | Question | Blocks |
|---|---|---|
| TBD-01 | Password policy: minimum length, complexity, breach-list check, rotation? | Identity |
| TBD-03 | Allowed identity providers for V1: local passwords only, or OIDC/SAML from day one? | Identity |
| TBD-04 | Exact PIN brute-force thresholds (delay curve, lock duration, attempt budget per device/membership/IP). | Security |
| TBD-05 | Idle/absolute session timeouts for terminals vs office sessions. | Security |
| TBD-06 | `RETURN` semantics: return from customer, return to supplier, or both? Does a supplier return reduce stock? | Inventory |
| TBD-07 | Near-expiry warning window and policy for expired stock (block issue? auto-waste?). | Inventory |
| TBD-08 | Over-receipt tolerance per PO line, and whether suppliers commonly over-deliver. | Purchasing |
| TBD-09 | Approval threshold model: fixed amounts, role hierarchy, or both? Who is the final approver? | Purchasing |
| TBD-10 | Recipe semantics: `servingsPerYield`, and whether consumption planning is per meal type, per day, or per person. | Food / Events |
| TBD-11 | Concurrent-session policy per device: single active session (kiosk) or multiple? | Identity |
| TBD-12 | Subscription grace period and what happens at expiry (read-only vs lockout). | Platform |
| TBD-13 | Billing: is there an external payment provider, or operator-managed subscription state? | Platform |
| TBD-14 | Audit retention period and archive destination. | Compliance |
| TBD-15 | Localization scope for reports/PDF/Excel: Arabic, English, or both from day one? | Reporting |
| TBD-19 | Warehouses need sub-locations (aisles/bins/racks) in V1 or V1.1? | Organization |
| TBD-21 | Currency: single currency per tenant, or multi-currency with FX rates? | Platform |
| TBD-22 | Warehouse transfers in transit: is there an "in transit" virtual warehouse, or is transfer instantaneous? | Inventory |
| TBD-23 | Should event attendance be integrated with an external registration system? | Events |
| TBD-24 | Are there multiple business entities/legal structures inside one tenant? | Platform |
| TBD-25 | Reporting scale target (rows/users/warehouses) to size indexes and pagination. | Performance |

### 23.2 Disposed design decisions

| ID | Question | Decision | Record |
|---|---|---|---|
| TBD-02 | Password hashing algorithm and work factors? | **Argon2id** for all new hashes; PBKDF2-HMAC-SHA256 only as an upgrade-on-login fallback. No BCrypt. | ADR-0008, §8.1 |
| TBD-17 | Full-text search: SQL Server index or a dedicated engine? | **SQL Server full text** (`Arabic_CI_AS`) over `master.Product`, `purchasing.Supplier`, `events.Event`, `master.Category`, with a trigram fallback. | ADR-0042 |
| TBD-18 | Barcode symbologies for camera scanning? | **The six modelled values** are the V1 set; `ProductBarcode` is the only barcode source. | ADR-0043, ADR-0033 |
| TBD-20 | Must the PWA work offline? | **No offline writes in V1.** Reads may be cached by the browser; the API is always authoritative. | ADR-0021 |

### 23.3 Disposed safe assumption

| ID | Question | Assumption | Record |
|---|---|---|---|
| TBD-16 | Are fractional quantities required from V1, and which base units? | **Safe superset:** `decimal(18,4)` everywhere and `Unit.DecimalPlaces` 0–4. The V1 schema already supports fractional quantities, so an owner answer requiring them needs no redesign — and an answer forbidding them is not a simplification. | DB-TBD-01 |

---

## 24. V1 / V1.1 / V2 / V3 boundaries

| Tier | Scope |
|---|---|
| **V1** | Platform/tenants/plans/subscriptions/entitlements; identity (users, memberships, roles, permissions, sessions, refresh tokens, password auth, PIN auth, lockout, rate limiting, devices); warehouses and assignments; master data (categories, units, conversions, products, barcodes, reason codes); inventory (balances, batches, expiry, FEFO, stock in/out/return/transfer/adjust/waste, stocktake, ledger); purchasing (suppliers, price history, requests, approvals, orders, partial receiving); events (events, requirements, consumption, attendance, shortage, recommendations); food (meal types, recipes, costing, event food cost); reporting (dashboard, inventory, movement, consumption, waste, purchase, supplier, price history, event, user activity) + CSV export; global search; audit log; Arabic/English UI |
| **V1.1** | PDF + Excel export; custom fields; attachments; in-app + PWA push notifications; per-warehouse min/max stock; expiry dashboards; warehouse sub-locations; device management UI hardening; ClamAV hook (SEC-KNW-06) |
| **V2** | OCR ingestion; stock reservations for event planning; supplier performance scoring; dedicated search engine if needed; multi-currency and FX; two-factor authentication; SSO/OIDC; in-transit virtual warehouse; device attestation or per-device client certificate (SEC-KNW-01); terminal display-name aliases (SEC-KNW-08); split platform operator roles (SEC-KNW-09); scheduled report delivery |
| **V3** | Public API for third parties; mobile native apps; advanced forecasting; external POS/ERP integrations; multi-entity consolidation |

Explicitly **out of scope for all tiers** unless a future decision says otherwise:
microservices, AI assistant features inside the product, blockchain, self-service
tenant signup with payment.

---

## 25. Delivery plan

Delivery is **Phase 0 (this blueprint) plus eight implementation phases**
(ADR-0040). Each phase ends in a demonstrable, verifiable increment, and
infrastructure belongs in Phase 1 where it is cheap to do properly — not in the
last phase where it is guaranteed to be rushed.

| Phase | Title | Ends with |
|---|---|---|
| 1 | **Foundation + Architecture + Database + Security Core** | Solution scaffold, all 12 schemas created by migration, the tenant-isolation data-access layer, the security spine (identity primitives, session/scope model, audit, error contract) and passing S1–S12 |
| 2 | **Identity + Tenants/Houses + Warehouses + RBAC** | All 105 permissions seeded, 11 tenant roles seeded by the D1–D5 derivation, the full authorization chain, login/logout/refresh rotation, lockout and rate limits |
| 3 | **Products + Units + Inventory Engine** | Master data, `StockBalance`/`InventoryLot`, the immutable ledger, the guarded `UPDATE` path, stock in/out/adjust/waste |
| 4 | **Devices + Shared Terminal + Purchasing + Suppliers** | Device administration and enrollment, the terminal PIN flow, suppliers, price history, requests, approvals, orders, partial receiving |
| 5 | **Batches + Expiry + Waste + Transfers + Stocktake + Locations + Barcode/QR** | Batch/expiry tracking, FEFO allocation, near-expiry job, transfers, stocktake, sub-locations, barcode scanning and printing |
| 6 | **Events + Recipes + Food Cost + Forecasting** | Events, attendance, requirements, consumption, shortage snapshots and recommendations, recipes, costing snapshots, forecasting |
| 7 | **Reports + Notifications + SaaS + Search + Customization** | Report and export endpoints, in-app and push notifications, plans/entitlements/usage, global search, custom fields, attachments |
| 8 | **Frontend PWA + Shared Terminal UI + Full System Testing + Production** | The PWA, the shared-terminal UI, the full S1–S33 suite, concurrency and DB-integrity suites, production hardening |

### 25.1 Legacy 11-phase mapping

ADR-0040 replaced an earlier eleven-phase plan. Documents that referenced the
old numbering must be remapped:

| Old | New | Note |
|---|---|---|
| Phase 1 Solution scaffold + Infrastructure | Phase 1 | Infrastructure stays in Phase 1 by decision |
| Phase 2 Identity & security core | Phase 1 + 2 | Identity primitives in 1, RBAC and login in 2 |
| Phase 3 Authentication | Phase 2 | Authentication is not independently demonstrable without identity |
| Phase 4 Organization + master data | Phase 2 + 3 | Warehouses in 2, products/units in 3 |
| Phase 5 Inventory engine | Phase 3 + 5 | Core engine in 3, batch/expiry/FEFO in 5 |
| Phase 6 Purchasing | Phase 4 | — |
| Phase 7 Events + food costing | Phase 6 | — |
| Phase 8 Reporting, audit, search, notifications | Phase 7 | Audit itself begins in Phase 1 |
| Phase 9 Security hardening + full security test suite | Removed as a phase | A security regression is a build failure at every phase (`AGENTS.md` §4) |
| Phase 10 Frontend PWA | Phase 8 | — |
| Phase 11 Production infrastructure + CI/CD | Phase 1 + 8 | Built in 1, hardened in 8 |

### 25.2 Ordering rule

Backend and security are established **before** the complete frontend. The
frontend may start early against a mocked or partial API, but the product is not
"built" until backend + security + tests are done. The product contains **no AI
assistant** at any phase (ADR-0023).

---

## 26. Definition of Done for a phase

```text
Design reviewed
  → Implementation complete
  → Unit tests written and passing
  → Integration tests against a real SQL Server passing
  → Security tests passing (tenant escape, IDOR, privilege escalation, brute force, replay)
  → Edge cases covered
  → Concurrency tests passing (where relevant)
  → Database integrity tests passing (constraints verified)
  → Performance sanity (where relevant)
  → Manual scenario walkthrough
  → Full regression green
  → Final security review
  → PASS recorded in docs/DEVELOPMENT_STATUS.md
```

**A phase is not complete because the project builds.** A security regression is
a build failure, not a follow-up ticket.

---

## 27. Approval and ratification

Phase 1 MUST NOT begin until the product owner has:

1. **Ratified this blueprint v1.0** and the 44 `Proposed` ADRs in
   `DECISIONS.md`, and
2. **Answered or explicitly deferred each of the 20 unresolved items** in §23.1,
   recording each answer as an ADR in `DECISIONS.md`, and
3. **Acknowledged the 9 registered security limitations** in
   `SECURITY_ARCHITECTURE.md` §15 (SEC-KNW-01 … SEC-KNW-09) as accepted for V1.

Ratification is recorded by promoting the relevant ADRs from `Proposed` to
`Accepted` and by updating the status line of this document. Until that happens
the status remains **STOCKLY BLUEPRINT v1.0 — BLOCKED BY OWNER DECISIONS**.

| Role | Name | Date | Status |
|---|---|---|---|
| Product owner | TBD | TBD | Pending ratification |
| Technical lead | TBD | TBD | Pending ratification |
