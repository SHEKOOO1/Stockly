# STOCKLY (ستوكلي) — PROJECT BLUEPRINT

| Field | Value |
|---|---|
| Document | `PROJECT_BLUEPRINT.md` |
| Status | **DRAFT — awaiting review and approval** |
| Version | 0.1.0 |
| Last updated | 2026-09-27 |
| Phase | 0 — Blueprint |
| Authority | This document is the master specification. `DATABASE_DESIGN.md`, `ARCHITECTURE.md`, `SECURITY_ARCHITECTURE.md`, `ROLES_PERMISSIONS.md`, `BUSINESS_RULES.md`, `WORKFLOWS.md`, `API_CONVENTIONS.md` and `TESTING_STRATEGY.md` are its detailed expansions. If they conflict, the conflict must be raised and resolved here, not silently in a sub-document. |

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

### 2.1 The six core concepts MUST remain separate

| Concept | Question it answers | Stockly entity |
|---|---|---|
| **User** | *Who am I?* | `AppUser` (global identity) |
| **Tenant Membership** | *Which organization am I acting for?* | `TenantMembership` |
| **Role / Permission** | *What may I do?* | `Role`, `Permission`, `MembershipRole` |
| **Warehouse Assignment** | *Where may I do it?* | `MembershipWarehouse` |
| **Tenant** | *Which organization owns the data?* | `Tenant` |
| **Device** | *Which physical terminal am I using?* | `Device` |
| **Session** | *Who is authenticated on this terminal right now?* | `AuthSession` |

These MUST NOT be collapsed into one model. In particular:

- A user with role `Worker` in `Kitchen Warehouse` and role `StoreKeeper` in
  `Cleaning Warehouse` is one identity with **two different effective capability
  sets per warehouse**.
- A device is **not** owned by a user. It is bound to a warehouse. Many users
  use it over time; none of them own it.

---

## 3. Module map

| # | Module | Purpose | Phase |
|---|---|---|---|
| 1 | **Platform / SaaS** | Tenants, plans, subscriptions, entitlements, usage limits | V1 |
| 2 | **Identity & Security** | Users, memberships, roles, permissions, sessions, tokens, PINs, devices, lockout, rate limiting, audit | V1 |
| 3 | **Organization** | Warehouses, warehouse assignments, devices, locations | V1 |
| 4 | **Inventory** | Categories, units, conversions, products, balances, lots/batches, expiry, FEFO, stock in/out/return/transfer/adjust/waste, stocktake | V1 |
| 5 | **Purchasing** | Suppliers, supplier products, price history, purchase requests, approvals, purchase orders, partial receiving, goods receipts | V1 |
| 6 | **Events / Conferences** | Events, requirements, consumption, attendance, planning, shortage calculation, purchase recommendations | V1 |
| 7 | **Food** | Meal types, recipes, recipe items, food costing, cost per meal/serving, event food cost | V1 |
| 8 | **Notifications** | In-app notifications, PWA push, notification preferences | V1.1 (core V1) |
| 9 | **Reporting** | Inventory, movement, consumption, waste, purchase, supplier, price history, event, user activity, audit, dashboard; PDF/Excel/CSV export | V1 / V1.1 |
| 10 | **Search & Customization** | Global search, custom fields, attachments, OCR-ready file architecture | V1 (global search) / V1.1 (custom fields, attachments) / V2 (OCR) |
| 11 | **Localization** | Arabic + English, RTL + LTR | V1 |

---

## 4. Entity catalogue (complete, authoritative list)

Tenant scope legend: **G** = global (no tenant), **T** = tenant-scoped, **W** = additionally warehouse-scoped.

### 4.1 Platform (`platform` schema)

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `AppUser` | `Id` | G | Global human identity (login credentials, lockout state) |
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
| `Role` | `Id` | T | Named bundle of permissions (system or custom) |
| `Permission` | `Id` | G | Seeded permission catalogue |
| `RolePermission` | `Id` | T | Role ↔ permission grant |
| `MembershipRole` | `Id` | T | Membership ↔ role assignment (per warehouse scope, see §5) |
| `MembershipRoleWarehouse` | `Id` | T | Junction restricting a `MembershipRole` to specific warehouses |
| `Warehouse` | `Id` | T | Physical/logical stock location |
| `MembershipWarehouse` | `Id` | T | Membership ↔ warehouse authorization (the "WHERE") |
| `Device` | `Id` | W | Shared terminal, bound to exactly one warehouse |
| `AuthSession` | `Id` | T | An authenticated user acting on a device in a warehouse |
| `RefreshToken` | `Id` | T | Rotating opaque refresh token (hashed, family-tracked) |
| `PinCredential` | `Id` | T | Terminal PIN for a membership (hashed, never returned) |
| `LoginAttempt` | `Id` | G | Throttle ledger for password/PIN authentication |
| `PasswordResetToken` | `Id` | T | Single-use password reset token (hashed) |
| `DocumentSequence` | `Id` | T | Per-tenant, per-document-type atomic number generator |

### 4.3 Master data (`master` schema)

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `Category` | `Id` | T | Hierarchical product category |
| `Unit` | `Id` | T | Unit of measure |
| `UnitConversion` | `Id` | T | Product/unit conversion factor |
| `Product` | `Id` | T | Sellable/storable item master |
| `ProductBarcode` | `Id` | T | One or more barcodes per product |
| `ProductWarehouseSetting` | `Id` | W | Min/max stock, reorder point, default supplier, shelf-life override |
| `ReasonCode` | `Id` | T | Controlled reason codes for movements and adjustments |

### 4.4 Inventory (`inventory` schema)

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `Batch` | `Id` | T | Lot identity: product + batch number + expiry |
| `InventoryLot` | `Id` | W | Stock of one batch in one warehouse (**concurrency-controlled row**) |
| `StockBalance` | `Id` | W | Aggregated on-hand/reserved per warehouse + product (**concurrency-controlled row**) |
| `StockDocument` | `Id` | W | Movement header (`STOCK_IN`, `STOCK_OUT`, `RETURN`, `TRANSFER`, `ADJUSTMENT`, `WASTE`, `COUNT`) |
| `StockDocumentLine` | `Id` | W | Movement line: product, quantity, unit, lot, unit cost |
| `StockTransaction` | `Id` | W | **Immutable posted ledger entry** produced by posting a document line |
| `StockCount` | `Id` | W | Stocktake session header |
| `StockCountLine` | `Id` | W | Expected vs counted quantity, variance, resolution |

`StockReservation` for event planning is **TBD** (V2) — see §24 and §22.9.

### 4.5 Purchasing (`purchasing` schema)

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `Supplier` | `Id` | T | Vendor master |
| `SupplierProduct` | `Id` | T | Supplier ↔ product with SKU, unit, last price, min order qty |
| `ProductPriceHistory` | `Id` | T | Effective-dated price records (source: PO / receipt) |
| `PurchaseRequest` | `Id` | T | Requisition header |
| `PurchaseRequestLine` | `Id` | T | Requisition line |
| `PurchaseApproval` | `Id` | T | Approval decision record for a request or order |
| `PurchaseOrder` | `Id` | W | PO header to a supplier, receiving warehouse |
| `PurchaseOrderLine` | `Id` | W | PO line: product, ordered qty, received qty, unit price |
| `GoodsReceipt` | `Id` | W | Goods receipt header against a PO |
| `GoodsReceiptLine` | `Id` | W | Received qty, lot, expiry, actual unit cost |

`SupplierPerformance` scoring is **TBD** (V2).

### 4.6 Events (`events` schema)

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `Event` | `Id` | T | Conference / gathering with dates, attendance, warehouse |
| `EventRequirement` | `Id` | T | Required product quantity for the event (total or per person) |
| `EventConsumption` | `Id` | W | Planned vs actual consumption per product |
| `EventAttendance` | `Id` | T | Per-person attendance + meal participation |
| `EventShortageSnapshot` | `Id` | W | Computed shortage and recommended purchase quantity at a point in time |

### 4.7 Food (`food` schema)

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `MealType` | `Id` | T | e.g. Breakfast / Lunch / Dinner / Snack |
| `Recipe` | `Id` | T | Recipe with yield, meal type, preparation metadata |
| `RecipeItem` | `Id` | T | Ingredient: product, quantity per yield, unit, wastage % |
| `RecipeCostSnapshot` | `Id` | T | Point-in-time recipe cost, cost per unit, cost per serving |
| `EventFoodCostSnapshot` | `Id` | T | Point-in-time food cost for an event and meal type |

### 4.8 Notifications, Files, Extensibility, Audit

| Entity | PK | Scope | Purpose |
|---|---|---|---|
| `Notification` | `Id` | T | In-app notification for a membership |
| `NotificationPreference` | `Id` | T | Per-membership channel/category preferences |
| `PushSubscription` | `Id` | T | Web-push endpoint + keys for PWA |
| `OutboxMessage` | `Id` | G | Transactional outbox for reliable side effects |
| `Attachment` | `Id` | T | File metadata; binary in object storage |
| `CustomFieldDefinition` | `Id` | T | User-defined field on an entity |
| `CustomFieldValue` | `Id` | T | Value of a custom field for a record (JSON justified — see §21) |
| `AuditLog` | `Id` | G | Append-only security/business audit trail |

Total V1 entity count: **66**. Full column-level design: `DATABASE_DESIGN.md`.

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
   leading column, so uniqueness is enforced **within** a tenant only.
5. Every foreign key from a tenant-scoped table to a tenant-scoped table MUST be
   constrained so that a row can only reference a row of the **same tenant**.
   (Achieved via composite FKs on `(Id, TenantId)`.)
6. A composite index MUST exist on every `*Transaction`, `*Line`, and audit
   table covering `(TenantId, …)` and `(WarehouseId, …)` access patterns.

### 5.2 Global (non-tenant) entities and why

| Entity | Why it is global |
|---|---|
| `AppUser` | A person is one identity across the platform. Tenant access is granted through `TenantMembership`, which keeps the multi-tenant model clean and avoids duplicate accounts per tenant. (ADR-0003) |
| `Permission` | Platform-wide permission catalogue, seeded by migration, read-only at runtime. |
| `Plan`, `PlanFeature` | SaaS commercial catalogue managed by the platform operator. |
| `LoginAttempt` | Throttle ledger keyed by identity/IP, must work before a tenant is known. |
| `AuditLog` | Security audit must survive tenant deletion and must be queryable platform-wide by platform admins. |
| `OutboxMessage` | Infrastructure concern spanning tenants. |

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
- `AuthSession` belongs to exactly one `TenantMembership` and at most one `Device`.
- A `StockDocument` records the `DeviceId` and `SessionId` that created it, so
  movement attribution survives session revocation.

---

## 6. Roles (summary)

Full matrix: `ROLES_PERMISSIONS.md`. Tenancy note: system roles are **seeded per
tenant** so each tenant owns its own role rows and can safely customize later.

| Role | Purpose |
|---|---|
| `PlatformAdmin` | SaaS operator; not a tenant role. Cross-tenant, heavily audited |
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

Authorization model: **roles are named permission bundles; permissions are
global codes.** Effective permission set of a membership in a given warehouse =
union of `MembershipRole` grants where the assignment's warehouse scope
intersects the requested warehouse, filtered by `WarehouseId ∈ MembershipWarehouse`.
See `ARCHITECTURE.md` §Authorization and `ROLES_PERMISSIONS.md`.

---

## 7. Authentication model

Stockly supports **two distinct authentication flows**. They converge on the
same session and authorization model.

### 7.1 Flow A — Password authentication (office / admin / personal devices)

```text
POST /api/v1/auth/login  { emailOrUsername, password, deviceId? }
    │
    ├─ Rate limit: per-IP + per-identity (sliding window)
    ├─ Resolve AppUser by email/username (constant-time, no enumeration)
    ├─ Verify password (Argon2id / BCrypt — see DECISIONS TBD)
    ├─ Enforce lockout policy (progressive delay → temporary lock)
    ├─ Require tenant membership selection (user may belong to >1 tenant)
    └─ Issue: access token (JWT, 15 min) + refresh token (opaque, rotated)
```

### 7.2 Flow B — PIN authentication (shared warehouse terminal)

```text
GET  /api/v1/terminal/sessions/{deviceId}/identities
    → returns ONLY: { membershipId, displayName, avatarRef }
      (explicitly NOT an authentication grant)

POST /api/v1/terminal/sessions/{deviceId}/pin-auth
    { membershipId, pin }
    │
    ├─ Rate limit: per-device + per-membership + per-IP
    ├─ Verify device exists, is active, and belongs to a warehouse
    ├─ Verify membership is Active, assigned to the device's warehouse,
    │   and holds a PIN credential
    ├─ Verify PIN (6+ digits, Argon2id/BCrypt hash, constant-time compare)
    ├─ Enforce PIN brute-force protection (progressive delay + lockout)
    ├─ Enforce one active session per device (configurable) — see TBD-11
    └─ Issue: access token + refresh token, session bound to device
```

**Hard rules (security-critical):**

- The selected name is **NOT** authentication. The backend authenticates the
  **PIN** against the selected membership. A client that simply asserts
  `userId = X` gets nothing.
- `GET /terminal/sessions/{deviceId}/identities` MUST return only the minimum
  data needed to render a list of names. It MUST NOT reveal whether a PIN is
  set, account status, roles, or any permission.
- A terminal MUST NOT support self-registration or account creation.
- PIN responses MUST be uniform in shape and timing regardless of whether the
  membership exists, the PIN is wrong, or the account is locked.

### 7.3 Token model

| Token | Type | Lifetime | Storage | Notes |
|---|---|---|---|---|
| Access token | JWT | 15 minutes | Memory only in PWA | Claims: `sub` (user), `mid` (membership), `tid`, `sid`, `did`, `jti` |
| Refresh token | Opaque random 256-bit | 7 days, rotating | `HttpOnly; Secure; SameSite=Strict` cookie | Stored **hashed**; family-tracked; reuse ⇒ revoke whole family |

Permissions are **NOT** embedded in the access token. Authorization is resolved
server-side per request from a short-lived cache. See ADR-0013.

---

## 8. PIN model

| Aspect | Rule |
|---|---|
| Length | Minimum **6 digits**, maximum 12 digits. Digits only. |
| Storage | Hashed with a memory-hard password KDF (Argon2id preferred; PBKDF2-HMAC-SHA256 as fallback). Never reversible. |
| Comparison | Constant-time comparison of the hash. No early-exit string compare on plaintext. |
| Reuse | PIN MUST NOT be equal to the user's password, the tenant code, the current year, or a trivially sequential value (`123456`, `111111`, …). Validation at set/change time. |
| Rotation | `PinVersion` on the credential invalidates all sessions authenticated by that PIN. |
| Lockout | Progressive delay after each failure, then temporary lock, then administrative unlock. Per-membership **and** per-device counters. |
| Reset | Only a holder of `security.pin.reset` may reset. Reset by an admin sets `MustChangePin = true`; the terminal forces a PIN change before granting stock permissions. |
| Enumeration | The system MUST NOT reveal whether a PIN exists, or which PINs are weak, in any API response. |
| Logging | PINs MUST NEVER appear in logs, audit records, exception messages, telemetry, or API responses. |

PIN brute-force parameters (exact thresholds) are **TBD** — see §23 TBD-04.

---

## 9. Session model

`AuthSession` fields: `Id`, `TenantId`, `UserId`, `MembershipId`, `DeviceId`,
`Status` (`Active`, `Expired`, `Revoked`, `Superseded`), `IssuedAt`, `ExpiresAt`,
`LastSeenAt`, `LastActivityAt`, `RevokedAt`, `RevokedReason`, `RevokedByMembershipId`,
`IpAddress`, `UserAgent`, `AuthenticationMethod` (`Password`, `Pin`), `PinCredentialVersion`,
`RefreshFamilyId`, `CorrelationId`.

Session rules (MUST):

1. **Idle timeout** — a session with no activity for the configured window is
   invalidated server-side. Default 15 minutes for terminals, 60 minutes for
   password sessions. *(TBD-05)*
2. **Absolute timeout** — maximum session lifetime regardless of activity.
3. **Auto-lock** — the terminal UI locks and clears in-memory state, but
   **server-side invalidation is the real control**.
4. **Explicit logout** — revokes the session and the whole refresh family.
5. **Switch user** — revokes the current session before a new one is created.
6. **Revocation** — an administrator with `security.sessions.revoke` can revoke
   any session; the effect is immediate because every authorized request checks
   session state.
7. **Last-seen** — `LastSeenAt` updated at most once per 60 seconds to avoid a
   write per request.
8. A revoked/expired session's access token MUST stop working immediately
   (checked server-side, not only via token expiry).

---

## 10. Device model

`Device` fields: `Id`, `TenantId`, `WarehouseId`, `DeviceCode` (human label,
e.g. `KITCHEN-01`), `Name`, `Type` (`Kiosk`, `Tablet`, `Desktop`, `Handheld`),
`Status` (`Active`, `Suspended`, `Revoked`), `IsSharedTerminal`,
`LastSeenAt`, `RegisteredAt`, `RegisteredByMembershipId`, `RevokedAt`,
`Notes`.

Device rules (MUST):

- A device belongs to **exactly one** warehouse. A terminal physically located
  in the kitchen cannot be used to act on the cleaning warehouse.
- Device registration is an administrative action requiring
  `device.manage` — never a user self-service action.
- Revoking a device revokes **all** active sessions bound to it.
- A device cannot be used to authenticate unless `IsSharedTerminal` is true and
  its status is `Active`.
- Devices are audited: register, suspend, revoke, reassign warehouse.
- Device identity is a **claim** (the client asserts `deviceId`), never proof.
  Proof of physical possession is out of scope for V1; the security boundary is
  PIN + rate limiting + short sessions. *(This limitation MUST be recorded in
  `DEVELOPMENT_STATUS.md` → Known Security Issues and re-assessed in V2.)*

---

## 11. Inventory model

### 11.1 Principle

Stockly inventory is **transaction-based**. A freely editable quantity field is
**never** the source of truth. The authoritative history is the immutable
`StockTransaction` ledger. `StockBalance` and `InventoryLot` are
transactionally-maintained projections that may be rebuilt from the ledger
(a documented, tested rebuild job).

### 11.2 Movement types

| Type | Direction | Effect on balance | Notes |
|---|---|---|---|
| `STOCK_IN` | + | Increase | Goods receipt, manual receipt |
| `STOCK_OUT` | − | Decrease | Consumption, issue |
| `RETURN` | + | Increase | Return from consumption; or − if returning to supplier (TBD-06) |
| `TRANSFER` | − / + | Move between warehouses | Paired transactions, same reference, atomic |
| `ADJUSTMENT` | + / − | Correct | Requires `ReasonCode` and elevated permission |
| `WASTE` | − | Decrease | Requires `ReasonCode`; separately reportable |
| `COUNT` | Derived | Reconciliation | Produced by posting a `StockCount`; generates `ADJUSTMENT` transactions |

### 11.3 Concurrency model (MUST)

Stock balance mutation MUST use a single atomic conditional `UPDATE` inside an
explicit database transaction:

```sql
UPDATE inventory.StockBalance
   SET OnHandQuantity = OnHandQuantity - @qty,
       RowVersion    = ROWVERSION
 WHERE Id = @balanceId
   AND TenantId = @tenantId
   AND WarehouseId = @warehouseId
   AND OnHandQuantity >= @qty;   -- guarded predicate
```

If `@@ROWCOUNT = 0`, the operation fails with `409 Conflict` and a safe
business error (`insufficient_stock`). This makes the classic race
("stock out 8" vs "stock out 5" from 10) impossible to resolve to a negative
quantity. Additionally:

- `CHECK (OnHandQuantity >= 0)` and `CHECK (ReservedQuantity >= 0)` constraints
  exist on the table as a last line of defence.
- `RowVersion` (`rowversion`) columns exist on all concurrency-controlled
  tables, and APIs expose them as ETags for optimistic client-side conflict
  detection.
- Document posting is all-or-nothing per document.
- Idempotency keys prevent duplicate postings from retried requests.

### 11.4 Batches, expiry, FEFO

- `Batch` = product + batch number + expiry date. Unique per tenant.
- `InventoryLot` = batch × warehouse. Owns the per-warehouse quantity.
- Products flagged `TrackBatch` MUST have a batch on every inbound movement.
- Products flagged `TrackExpiry` MUST have an expiry date on every inbound
  movement; inbound lots already expired MUST be rejected.
- FEFO (First-Expired-First-Out) is the default outbound allocation order:
  ascending `ExpiryDate`, then `ReceivedAt`, then `Id`. Products without
  tracking ignore expiry.
- Near-expiry reporting: lots expiring within a configurable window (default
  30 days) are surfaced in dashboard and notifications. *(TBD-07)*

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
   `OrderedQuantity + TBD-08 (over-receipt tolerance)`.
4. Over-receipt beyond tolerance is rejected unless the order has
   `AllowOverReceipt = true` and the actor holds `purchase.order.manage`.
5. Approval is a server-side state machine. A document cannot be posted or
   received while in `Draft`/`PendingApproval`/`Rejected`/`Cancelled`.
6. Approval thresholds per tenant are configurable (amount, or role-based).
   Exact threshold model is **TBD-09**.
7. Unit cost on a goods receipt becomes the latest `ProductPriceHistory` entry
   and updates the moving average (ADR-0017).
8. A purchase order can be posted as a `STOCK_IN` stock document, so
   receiving is auditable through the same inventory pipeline.

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

1. Requirements are computed from: attendance count, event duration in days,
   meal types served, and per-person consumption factors — all tenant-configurable.
2. Shortage = `required − (on-hand + already reserved/committed + inbound on open POs)`.
3. A shortage MAY generate a draft `PurchaseRequest`; generating it requires
   `purchasing.requests.create` — recommendations themselves only require
   `events.shortage.read`.
4. Event consumption records link to real `StockDocument` rows so that
   "consumed at event X" is provable, not asserted.
5. Event food cost = Σ (recipe cost per serving × servings) across meal types,
   plus non-recipe items, snapshot at calculation time.
6. Deleting an event that has posted stock documents is forbidden; events are
   archived instead (status `Cancelled`/`Completed`).

Meal type and recipe-driven planning details are **TBD-10** (see §23).

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
3. `CostPerServing = RecipeCost / servingsPerYield`, where `servingsPerYield`
   is a recipe field (**TBD-10** — servings per unit of yield is a product
   decision, e.g. "1 recipe yields 40 portions").
4. Inventory valuation primary method: **moving weighted average** per
   `(Tenant, Warehouse, Product)` (ADR-0017). Fallback hierarchy for products
   with no purchase history: last purchase price → standard cost → zero, with
   the fallback explicitly reported in the snapshot so a reader can tell
   estimated costs from real costs.
5. Cost snapshots are **immutable point-in-time records**. Recalculating
   creates a new snapshot; it never rewrites history.
6. Costing must be computed with a consistent view of price and quantity data
   inside a single transaction to avoid mixed-snapshot results.

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
   resource (creating a 5th user when the plan allows 4 MUST fail with
   `403`/`409` and a clear business error).
2. The frontend MUST NOT hide features as an enforcement mechanism. Hiding is a
   UX courtesy; the API is the boundary.
3. A tenant with no active subscription, or in `PastDue` past the grace period,
   enters a **read-only** mode: data reads allowed, all writes rejected with
   `402`-style business error. Exact grace period is **TBD-12**.
4. Usage counters are updated in the same transaction as the resource creation
   to avoid over-provisioning under concurrency.
5. Platform admins may grant overrides; every override is audited.

Billing provider integration is **TBD-13** (V1 has no payment processing; the
subscription state is managed internally or by an operator).

---

## 16. Audit model

`AuditLog` fields: `Id`, `OccurredAtUtc`, `TenantId` (nullable for platform-level
events), `Category` (`Security`, `Identity`, `Inventory`, `Purchasing`, `Events`,
`Administration`, `Platform`), `Severity`, `Action` (e.g.
`stock.out.post`, `auth.pin.failed`, `role.permissions.changed`),
`Result` (`Success`, `Failure`, `Denied`), `EntityName`, `EntityId`,
`ActorUserId`, `ActorMembershipId`, `DeviceId`, `SessionId`, `IpAddress`,
`UserAgent`, `CorrelationId`, `ReasonCode`, `BeforeJson`, `AfterJson`,
`Quantity`, `UnitId`, `ProductId`.

Audit rules (MUST):

1. Audit records are **append-only**. No update. No delete through the
   application. No `DELETE` grant for the application database principal on
   `audit.AuditLog`.
2. The table is **partitioned by month** on `OccurredAtUtc`; older partitions
   are archived to cold storage. Retention: **TBD-14**.
3. Ordinary users MUST NOT be able to read, modify, or delete audit records.
   Read access requires `report.audit` or `tenant.audit.read`, and the API
   never returns raw `BeforeJson`/`AfterJson` to non-admin roles.
4. Security-category events are recorded even on **failure** — failed logins,
   PIN brute force, denied authorization, tenant-escape attempts, and rejected
   token replays.
5. Audit MUST NOT contain secrets: no passwords, PINs, PIN hashes, JWTs,
   refresh tokens, encryption keys, connection strings. A redaction helper is
   applied to all before/after payloads and is covered by a test.
6. Audit writes MUST NOT be able to fail the business transaction silently:
   they are written **inside the same database transaction** as the business
   change (no distributed transaction is needed for V1 because both are in the
   same SQL Server database).
7. High-volume non-security events may be batched through the outbox
   (ADR-0011); **security** events are always written synchronously.

---

## 17. Security guarantees (summary; details in `SECURITY_ARCHITECTURE.md`)

The API MUST defend against, with automated tests: tenant escape, warehouse
escape, IDOR/BOLA, privilege escalation, role/permission tampering, mass
assignment, SQL injection, XSS, CSRF, unsafe file upload, path traversal,
oversized payloads, token replay, session hijacking, PIN brute force, password
brute force, rate-limit bypass, account enumeration, race conditions,
duplicate transactions, replayed requests, information leakage, and unsafe
error messages.

Non-negotiable guarantees:

| # | Guarantee |
|---|---|
| G1 | The backend is authoritative for identity, tenant, warehouse, permission, role, price, quantity, and approval state. |
| G2 | Tenant scope is derived from the authenticated session only. |
| G3 | Every warehouse-scoped request verifies user → tenant → permission → warehouse assignment → resource ownership. |
| G4 | Security is never implemented by frontend visibility. |
| G5 | Inventory cannot be driven negative by concurrent requests. |
| G6 | No secret is ever logged, returned, seeded, or committed. |
| G7 | Errors never leak stack traces, SQL, internal identifiers of other tenants, or existence of resources the caller cannot see (404 instead of 403 for cross-tenant existence disclosure). |
| G8 | A revoked session or device stops working immediately, not at token expiry. |
| G9 | All state-changing inventory operations are idempotent and auditable. |

---

## 18. API boundaries

Base path: `/api/v1`. Full contract: `API_CONVENTIONS.md`.

| Boundary | Owner | Rule |
|---|---|---|
| `/api/v1/platform/**` | Platform admin only | Separate permission namespace, separate audit category, separate rate limits |
| `/api/v1/tenant/**` | Tenant admin | Tenant settings, memberships, roles, warehouses, devices, audit |
| `/api/v1/master/**` | Catalog managers | Categories, units, products, suppliers, reasons |
| `/api/v1/inventory/**` | Warehouse operations | Balances, lots, documents, counts, ledger |
| `/api/v1/purchasing/**` | Purchasing | Requests, orders, receipts, prices |
| `/api/v1/events/**` | Event managers | Events, requirements, consumption, shortage |
| `/api/v1/food/**` | Kitchen | Recipes, costing |
| `/api/v1/reports/**` | Reporting | Aggregations and exports |
| `/api/v1/notifications/**` | All users | In-app and push |
| `/api/v1/auth/**`, `/api/v1/terminal/**` | Anonymous + authenticated | Authentication flows |

Boundary rules (MUST):

1. Each module owns its routes, its DTOs, and its authorization policy. A
   module MUST NOT query another module's tables directly; it calls that
   module's application service.
2. Cross-module writes go through the owning module's use case, so
   authorization and audit are not bypassed.
3. Platform routes are physically separated and MUST NOT be reachable with a
   tenant-scoped token.
4. Report endpoints are read-only and MUST NOT leak rows outside the caller's
   warehouse assignment scope.

---

## 19. Validation rules (summary; full list in `BUSINESS_RULES.md`)

| Area | Rule |
|---|---|
| Identifiers | Client-supplied IDs MUST be validated for ownership in the same query that loads the entity. Never load-then-check. |
| Quantities | Positive, within `decimal(18,4)`, no more than 4 decimal places, no scientific notation. Quantity 0 allowed only for explicit zero-lines in stocktake. |
| Dates | `OccurredAt` MAY NOT be in the future beyond a small clock-skew tolerance (default 5 min). Expiry date MAY NOT be before the batch production date. |
| Names | 1–200 characters, trimmed, no control characters. |
| Barcodes | 4–64 characters, uppercase-normalized, unique per tenant. |
| Email | RFC-shaped, normalized to lowercase, unique globally. |
| PIN | 6–12 digits, strength rules from §8. |
| Password | **TBD-15** (length/complexity/breach-list policy). |
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
| Unique indexes as concurrency guards | `DocumentSequence`, idempotency keys, barcode uniqueness |
| `SELECT … WITH (UPDLOCK, HOLDLOCK)` | Read-then-write sequences that cannot be expressed as a single guarded `UPDATE` |
| `IsolationLevel.ReadCommitted` by default | Raise to `Serializable`/`RepeatableRead` per use case, never `ReadUncommitted` |
| Idempotency-Key | Document posting, goods receipts, stock counts submission |
| Optimistic retry in the outbox worker | Notification/audit fan-out |

**MUST NOT** use: distributed locks, in-process locks as correctness guarantees,
application-level read-then-write without a guard, or `ReadUncommitted`.

---

## 21. File / attachment strategy

| Aspect | V1 | Later |
|---|---|---|
| Storage | Local filesystem behind an `IFileStore` abstraction (volume-mounted) | S3-compatible object storage |
| Metadata | `Attachment` table: tenant, entity, entity id, file name, content type, size, SHA-256, storage key, uploaded by | + scan status, version, retention |
| Naming | Random GUID storage key; original name is metadata only | |
| Validation | Extension **and** content-type allowlist, magic-byte sniffing, size limit, image re-encode for images | + malware scanning (ClamAV) |
| Serving | Authenticated, authorized download endpoint that re-checks entity permission; `Content-Disposition: attachment`; `X-Content-Type-Options: nosniff` | CDN signed URLs |
| Path traversal | Storage key is a server-generated GUID — the client file name is NEVER used to build a path | |
| OCR | Architecture reserves `Attachment.OcrStatus` and `ExtractedText` fields so OCR can be added without breaking the model | OCR pipeline in V2 |

Custom fields are the **only** justified JSON storage in V1: user-defined
schemas cannot be modelled relationally ahead of time. Every other business
concept is relationally modelled (per §19 of the operating rules).

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

The backend MUST NOT store pre-translated sentences. Master data names MAY be
translated because they are tenant data, not product strings.

---

## 23. Open requirements (TBD) — must be answered before Phase 1

| ID | Question | Blocks |
|---|---|---|
| TBD-01 | Password policy: minimum length, complexity, breach-list check, rotation? | Identity |
| TBD-02 | Password hashing algorithm: Argon2id (preferred) or BCrypt, and work factors? | Identity |
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
| TBD-16 | Units of measure: are fractional quantities (e.g. 0.5 kg) required from V1, and which base units? | Inventory |
| TBD-17 | Global search: SQL Server full-text index, or a dedicated search engine? | Search |
| TBD-18 | Barcode symbologies required for camera scanning (EAN-13, Code-128, QR, DataMatrix)? | Inventory |
| TBD-19 | Warehouses need sub-locations (aisles/bins/racks) in V1 or V1.1? | Organization |
| TBD-20 | Is the PWA required to work offline for **reads**, or fully online-only in V1? | Frontend |
| TBD-21 | Currency: single currency per tenant, or multi-currency with FX rates? | Platform |
| TBD-22 | Warehouse transfers in transit: is there an "in transit" virtual warehouse, or is transfer instantaneous? | Inventory |
| TBD-23 | Should event attendance be integrated with an external registration system? | Events |
| TBD-24 | Are there multiple business entities/legal structures inside one tenant? | Platform |
| TBD-25 | Reporting scale target (rows/users/warehouses) to size indexes and pagination. | Performance |

---

## 24. V1 / V1.1 / V2 / V3 boundaries

| Tier | Scope |
|---|---|
| **V1** | Platform/tenants/plans/subscriptions/entitlements; identity (users, memberships, roles, permissions, sessions, refresh tokens, password auth, PIN auth, lockout, rate limiting, devices); warehouses and assignments; master data (categories, units, conversions, products, barcodes, reason codes); inventory (balances, batches, expiry, FEFO, stock in/out/return/transfer/adjust/waste, stocktake, ledger); purchasing (suppliers, price history, requests, approvals, orders, partial receiving); events (events, requirements, consumption, attendance, shortage, recommendations); food (meal types, recipes, costing, event food cost); reporting (dashboard, inventory, movement, consumption, waste, purchase, supplier, price history, event, user activity) + CSV export; global search over products/events/suppliers; audit log; Arabic/English UI |
| **V1.1** | PDF + Excel export; custom fields; attachments; in-app + PWA push notifications; per-warehouse min/max stock; expiry dashboards; warehouse sub-locations; device management UI hardening |
| **V2** | OCR ingestion; stock reservations for event planning; supplier performance scoring; dedicated search engine if needed; multi-currency; two-factor authentication; SSO/OIDC; in-transit virtual warehouse; custom approval workflows UI; scheduled report delivery |
| **V3** | Public API for third parties; mobile native apps; advanced forecasting; external POS/ERP integrations; multi-entity consolidation |

Explicitly **out of scope for all tiers** unless a future decision says otherwise:
microservices, AI assistant features inside the product, blockchain,
self-service tenant signup with payment.

---

## 25. Implementation order

```text
Phase 0  Blueprint (this document)                 ← CURRENT
Phase 1  Solution scaffold + Infrastructure       (Domain, Application, Api, DbContext, migrations)
Phase 2  Identity & security core                  (tenants, users, memberships, roles, permissions, devices)
Phase 3  Authentication                             (password + PIN, sessions, refresh rotation, lockout, rate limits)
Phase 4  Organization + master data                (warehouses, assignments, categories, units, products)
Phase 5  Inventory engine                           (balances, lots, documents, ledger, FEFO, counts)
Phase 6  Purchasing                                 (suppliers, requests, approvals, orders, receipts)
Phase 7  Events + food costing
Phase 8  Reporting, audit, search, notifications
Phase 9  Security hardening + full security test suite
Phase 10 Frontend PWA
Phase 11 Production infrastructure + CI/CD
```

Backend and security are established **before** the complete frontend. The
frontend may start early against a mocked or partial API, but the product is not
"built" until backend + security + tests are done.

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

**A phase is not complete because the project builds.**

---

## 27. Approval

Phase 1 MUST NOT begin until this blueprint is reviewed and approved by the
product owner, and all `TBD` items in §23 are either answered or explicitly
deferred with a recorded decision in `DECISIONS.md`.

| Role | Name | Date | Status |
|---|---|---|---|
| Product owner | TBD | TBD | Pending |
| Technical lead | TBD | TBD | Pending |
