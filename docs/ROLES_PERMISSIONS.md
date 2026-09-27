# STOCKLY — ROLES & PERMISSIONS

| Field | Value |
|---|---|
| Document | `ROLES_PERMISSIONS.md` |
| Version | 0.1.0 |
| Status | DRAFT — Phase 0 |
| Last updated | 2026-09-27 |
| Related | `SECURITY_ARCHITECTURE.md`, `DATABASE_DESIGN.md`, `BUSINESS_RULES.md` |

---

## 1. Model

```text
Permission          global code, seeded, read-only at runtime
   ▲
Role                tenant-scoped named bundle of permissions + AuthorityLevel
   ▲
MembershipRole      membership ↔ role  (optionally scoped to specific warehouses)
   ▲
TenantMembership    user ↔ tenant
   ▲
AppUser            global identity
```

Two independent axes:

| Axis | Question | Table |
|---|---|---|
| **Permission (role)** | *What may I do?* | `Role` → `RolePermission` → `Permission` |
| **Warehouse assignment** | *Where may I do it?* | `MembershipWarehouse` |

**The effective authorization for a request is the intersection of the two.**
A `WarehouseWorker` holding `inventory.stock.out` in the kitchen cannot issue
stock from the cleaning warehouse, because the cleaning warehouse is not in
`MembershipWarehouse` for that membership.

```text
effective(permission, warehouse)
    = ∃ MembershipRole m
        where m.Membership = current
          AND permission ∈ m.Role.Permissions
          AND (m.ScopeType = AllAssignedWarehouses
               OR warehouse ∈ m.ScopeWarehouses)
        AND warehouse ∈ MembershipWarehouse(current)
        AND TenantMembership(current).Status = Active
        AND AuthSession(current).Status = Active
```

---

## 2. Permission catalogue

Codes are dot-notation `module.resource.action`. Codes are **permanent API
contract**; renaming a code is a breaking change requiring a decision record.

### 2.1 Platform (`platform.*`) — platform operators only

| Code | Dangerous | Purpose |
|---|---|---|
| `platform.tenants.read` | | List and inspect tenants |
| `platform.tenants.create` | ✔ | Create a tenant |
| `platform.tenants.manage` | ✔ | Update tenant profile/status |
| `platform.plans.read` | | Read plans |
| `platform.plans.manage` | ✔ | Create/edit plans and features |
| `platform.subscriptions.read` | | Read subscriptions |
| `platform.subscriptions.manage` | ✔ | Change subscription state, periods |
| `platform.entitlements.manage` | ✔ | Grant/override tenant entitlements |
| `platform.usage.read` | | Read usage counters |
| `platform.audit.read` | ✔ | Read cross-tenant audit (privileged) |
| `platform.maintenance` | ✔ | Operational tools (rebuild projections, replay outbox) |

### 2.2 Tenant administration (`tenant.*`)

| Code | Dangerous | Purpose |
|---|---|---|
| `tenant.read` | | Read tenant profile/settings |
| `tenant.settings.manage` | ✔ | Edit tenant settings |
| `tenant.users.read` | | List memberships |
| `tenant.users.manage` | ✔ | Create/suspend/reactivate memberships |
| `tenant.roles.read` | | List roles and their permissions |
| `tenant.roles.manage` | ✔ | Create roles, change role permissions, assign roles |
| `tenant.warehouses.read` | | List warehouses |
| `tenant.warehouses.manage` | ✔ | Create/edit/deactivate warehouses |
| `tenant.assignments.manage` | ✔ | Assign/unassign a membership to a warehouse |
| `tenant.audit.read` | ✔ | Read own-tenant audit log |
| `tenant.audit.export` | ✔ | Export audit data (redacted) |
| `tenant.subscription.read` | | Read own subscription/entitlements |

### 2.3 Devices & sessions (`device.*`, `security.*`)

| Code | Dangerous | Purpose |
|---|---|---|
| `device.read` | | List devices and last-seen |
| `device.manage` | ✔ | Register, edit, suspend, revoke devices |
| `device.assign_warehouse` | ✔ | Move a device to another warehouse |
| `security.sessions.read` | | List active sessions |
| `security.sessions.revoke` | ✔ | Revoke sessions (own or others') |
| `security.pin.reset` | ✔ | Reset a member's terminal PIN |
| `security.password.reset` | ✔ | Reset a member's password |
| `security.lockout.manage` | ✔ | Clear a lockout |
| `security.mfa.manage` | | Enrol/reset second factor (V2) |

### 2.4 Master data (`master.*`)

| Code | Purpose |
|---|---|
| `master.categories.read` / `master.categories.manage` | Product categories |
| `master.units.read` / `master.units.manage` | Units and conversions |
| `master.products.read` / `master.products.manage` | Product master |
| `master.products.cost.read` | See unit cost / average cost |
| `master.barcodes.manage` | Manage product barcodes |
| `master.reason_codes.read` / `master.reason_codes.manage` | Movement reason codes |
| `master.attachments.read` / `master.attachments.manage` | Attachments |

### 2.5 Inventory (`inventory.*`)

| Code | Dangerous | Purpose |
|---|---|---|
| `inventory.balances.read` | | View stock balances (warehouse-scoped) |
| `inventory.lots.read` | | View batches/expiry/FEFO state |
| `inventory.ledger.read` | | Read the stock transaction ledger |
| `inventory.stock.in` | | Post stock in |
| `inventory.stock.out` | | Post stock out |
| `inventory.stock.return` | | Post returns |
| `inventory.stock.transfer` | | Transfer between warehouses |
| `inventory.stock.adjust` | | Post adjustments (requires a reason code) |
| `inventory.stock.waste` | | Post waste (requires a reason code) |
| `inventory.stock.count` | ✔ | Create and fill in a stock count |
| `inventory.stock.count.post` | ✔ | Post a stock count (creates adjustments) |
| `inventory.reason_codes.manage` | ✔ | Manage reason codes |
| `inventory.min_levels.manage` | | Per-warehouse min/max/reorder settings |
| `inventory.reservations.manage` | | Reserve stock (V2) |

### 2.6 Purchasing (`purchasing.*`)

| Code | Dangerous | Purpose |
|---|---|---|
| `purchasing.suppliers.read` / `purchasing.suppliers.manage` | | Supplier master |
| `purchasing.supplier_products.read` / `purchasing.supplier_products.manage` | | Supplier catalogue |
| `purchasing.prices.read` / `purchasing.prices.manage` | | Price history |
| `purchasing.requests.read` | | View purchase requests |
| `purchasing.requests.create` | | Create purchase requests |
| `purchasing.requests.manage` | | Edit/delete own-tenant requests |
| `purchasing.requests.approve` | ✔ | Approve/reject purchase requests |
| `purchasing.orders.read` | | View purchase orders |
| `purchasing.orders.create` | | Create purchase orders |
| `purchasing.orders.manage` | | Edit/cancel purchase orders |
| `purchasing.orders.approve` | ✔ | Approve purchase orders |
| `purchasing.orders.receive` | | Post goods receipts (partial) |
| `purchasing.orders.close` | | Close a fully received order |

### 2.7 Events (`events.*`)

| Code | Purpose |
|---|---|
| `events.read` | View events |
| `events.manage` | ✔ Create/edit/cancel events |
| `events.requirements.manage` | Manage event requirements |
| `events.attendance.manage` | Manage attendance |
| `events.consumption.read` | View consumption |
| `events.consumption.manage` | Record consumption |
| `events.shortage.read` | View shortage calculations |
| `events.shortage.recalculate` | Recalculate shortages |
| `events.reports.read` | Event reports |

### 2.8 Food (`food.*`)

| Code | Purpose |
|---|---|
| `food.meal_types.read` / `food.meal_types.manage` | Meal types |
| `food.recipes.read` / `food.recipes.manage` | Recipes and items |
| `food.costing.read` | Recipe and event cost |
| `food.costing.calculate` | Trigger cost recalculation |

### 2.9 Reporting (`reports.*`)

| Code | Purpose |
|---|---|
| `reports.dashboard` | Dashboard analytics |
| `reports.inventory` | Inventory/stock valuation reports |
| `reports.movement` | Movement reports |
| `reports.consumption` | Consumption reports |
| `reports.waste` | Waste reports |
| `reports.purchasing` | Purchase reports |
| `reports.suppliers` | Supplier reports |
| `reports.price_history` | Price history reports |
| `reports.events` | Event reports |
| `reports.user_activity` | User activity reports |
| `reports.audit` | ✔ Audit reports (raw payload admin-only) |
| `reports.export` | Export in PDF/Excel/CSV |

### 2.10 Customization & notifications

| Code | Purpose |
|---|---|
| `custom_fields.read` / `custom_fields.manage` | Custom field definitions |
| `notifications.read` | Own notifications |
| `notifications.preferences.manage` | Own notification preferences |
| `admin.search` | Global search |

---

## 3. System roles

System roles are **seeded per tenant** so each tenant owns its own role rows and
can be customised in a future version without cross-tenant coupling.
`AuthorityLevel` is a numeric escalation guard (1 lowest, 100 highest).

| Role | Code | Level | Scope default | Notes |
|---|---|---|---|---|
| Platform Administrator | *(not a tenant role)* | — | all tenants | `IsPlatformAdmin` on `AppUser` |
| Owner | `tenant_owner` | 100 | All assigned | Full tenant authority. Cannot be removed if last |
| Tenant Administrator | `tenant_admin` | 80 | All assigned | Users, roles, warehouses, devices, settings |
| Warehouse Manager | `warehouse_manager` | 60 | All assigned | Full operations in assigned warehouses |
| Store Keeper | `store_keeper` | 45 | All assigned | Receiving, issuing, transfers, stock counts |
| Worker | `worker` | 25 | All assigned | Issue stock and record consumption only |
| Purchaser | `purchaser` | 45 | All assigned | Suppliers, requests, orders (no approval) |
| Purchasing Manager | `purchasing_manager` | 70 | All assigned | Approvals, supplier master, prices |
| Chef / Kitchen Manager | `kitchen_manager` | 50 | All assigned | Recipes, meal types, event food, costing |
| Accountant | `accountant` | 50 | All assigned | Costs, prices, purchase/supplier reports; read-only stock |
| Auditor | `auditor` | 40 | All assigned | Read everything incl. audit; no writes |
| Viewer | `viewer` | 10 | All assigned | Read-only operational data; no audit, no costs |

### 3.1 Role → permission matrix

Legend: ● full, ○ partial/limited, – none.

| Permission group | owner | admin | wh mgr | store kpr | worker | purchaser | purch mgr | kitchen | accountant | auditor | viewer |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `tenant.users.manage` | ● | ● | – | – | – | – | – | – | – | ○ | – |
| `tenant.roles.manage` | ● | ● | – | – | – | – | – | – | – | – | – |
| `tenant.warehouses.manage` | ● | ● | – | – | – | – | – | – | – | ○ | – |
| `tenant.assignments.manage` | ● | ● | – | – | – | – | – | – | – | – | – |
| `device.manage` | ● | ● | ○ | – | – | – | – | – | – | ○ | – |
| `security.sessions.revoke` | ● | ● | ○ | – | – | – | – | – | – | ○ | – |
| `security.pin.reset` | ● | ● | ○ | – | – | – | – | – | – | – | – |
| `master.products.manage` | ● | ● | ○ | ○ | – | ○ | ○ | ○ | – | – | – |
| `master.products.cost.read` | ● | ○ | ○ | ○ | – | ○ | ● | ○ | ● | ● | – |
| `inventory.balances.read` | ● | ● | ● | ● | ● | ● | ● | ● | ○ | ● | ● |
| `inventory.ledger.read` | ● | ● | ● | ● | ○ | ● | ● | ● | ○ | ● | ● |
| `inventory.stock.in` | ● | ● | ● | ● | – | – | ○ | ● | – | – | – |
| `inventory.stock.out` | ● | ● | ● | ● | ● | – | – | ● | – | – | – |
| `inventory.stock.return` | ● | ● | ● | ● | ○ | – | – | ○ | – | – | – |
| `inventory.stock.transfer` | ● | ● | ● | ● | – | – | – | – | – | – | – |
| `inventory.stock.adjust` | ● | ● | ● | ○ | – | – | – | – | – | – | – |
| `inventory.stock.waste` | ● | ● | ● | ● | ○ | – | – | ● | – | – | – |
| `inventory.stock.count` | ● | ● | ● | ● | – | – | – | – | – | – | – |
| `inventory.stock.count.post` | ● | ● | ● | ○ | – | – | – | – | – | – | – |
| `purchasing.requests.create` | ● | ● | ○ | ○ | – | ● | ● | – | – | – | – |
| `purchasing.requests.approve` | ● | ● | – | – | – | – | ● | – | – | – | – |
| `purchasing.orders.create` | ● | ● | ○ | – | – | ● | ● | – | – | – | – |
| `purchasing.orders.approve` | ● | ● | – | – | – | – | ● | – | ○ | – | – |
| `purchasing.orders.receive` | ● | ● | ● | ● | – | – | ○ | – | – | – | – |
| `events.manage` | ● | ● | ○ | – | – | – | – | ○ | – | – | – |
| `events.consumption.manage` | ● | ● | ● | ○ | ○ | – | – | ● | – | – | – |
| `events.shortage.read` | ● | ● | ● | ○ | ○ | ● | ● | ● | ○ | ● | ○ |
| `food.recipes.manage` | ● | ○ | – | – | – | – | – | ● | – | – | – |
| `food.costing.read` | ● | ● | ○ | – | – | ○ | ● | ● | ● | ● | – |
| `reports.audit` | ● | ● | – | – | – | – | – | – | – | ● | – |
| `tenant.audit.read` | ● | ● | – | – | – | – | – | – | – | ● | – |
| `reports.export` | ● | ● | ○ | – | – | ● | ● | ○ | ○ | ○ | – |
| `custom_fields.manage` | ● | ● | – | – | – | – | – | – | – | – | – |

Matrix refinements (exact per-permission grants) live in the seeding migration,
which is the single source of truth. The table above is the design intent; any
divergence between it and the seed must be corrected in the seed and reviewed.

---

## 4. Role scope and warehouse assignment

`MembershipRole.ScopeType`:

| ScopeType | Meaning | Effect |
|---|---|---|
| `1 = AllAssignedWarehouses` | Role applies in every warehouse the member is assigned to | Normal case |
| `2 = SpecificWarehouses` | Role applies only in the listed warehouses | Used for a manager who is a manager of one warehouse and a worker in another |

`MembershipWarehouse` is a hard boundary regardless of role scope: a warehouse
not present in `MembershipWarehouse` can never be operated by that membership,
even if the membership holds the role across all warehouses.

`MembershipWarehouse.CanReceive` / `CanIssue` add a further per-warehouse,
per-direction restriction (e.g. a cleaner may issue waste but not receive goods).

### 4.1 Examples

**A. Kitchen worker in the kitchen only**

```text
AppUser U1
└── TenantMembership M1 (Tenant T1, Active)
    ├── MembershipRole  → Worker (level 25), ScopeType=1
    │   └── permissions: inventory.stock.out, inventory.balances.read, events.consumption.manage
    └── MembershipWarehouse: { Warehouse W-KITCHEN }  (CanIssue=1, CanReceive=0)
```

- `POST /inventory/stock-out` on `W-KITCHEN` → **200**
- `POST /inventory/stock-out` on `W-CLEANING` → **404** (not assigned; existence not disclosed)
- `POST /inventory/stock-in` on `W-KITCHEN` → **403** (no permission; also `CanReceive=0`)
- `GET /inventory/balances?warehouseId=W-CLEANING` → **400** (invalid warehouse parameter for this user)

**B. Manager of the kitchen, worker in the cleaning store**

```text
Membership M2
├── MembershipRole → WarehouseManager, ScopeType=2, warehouses={W-KITCHEN}
├── MembershipRole → Worker,             ScopeType=1
└── MembershipWarehouse: { W-KITCHEN, W-CLEANING }
```

- `inventory.stock.adjust` in `W-KITCHEN` → **200** (manager scope covers it)
- `inventory.stock.adjust` in `W-CLEANING` → **403** (worker role does not include adjust)
- `inventory.stock.out` in `W-CLEANING` → **200** (worker scope covers all assigned)

This is exactly the separation the product requires: **role and location are
independent axes.**

---

## 5. Privilege escalation controls

| Rule | Enforcement |
|---|---|
| A membership can only be granted a role with `AuthorityLevel ≤` the granter's own highest level | API + audit + test |
| `tenant_owner` (level 100) can only be granted by a platform admin or the current owner | API |
| A custom role cannot include permissions the creator does not hold | API |
| The last active `tenant_owner` in a tenant cannot be demoted, suspended, or removed | API + integration test |
| A user cannot modify their own roles or warehouse assignments | API (`tenant.roles.manage` holder cannot be the target if the target is the holder's only admin path) |
| System role permission sets are immutable in V1 | API rejects edits; a decision is required to change this |
| Every grant/revoke is audited with before/after | Audit |

---

## 6. Frontend usage (advisory only)

The API exposes `GET /api/v1/auth/me` returning:

```json
{
  "userId": "…", "membershipId": "…", "tenantId": "…",
  "displayName": "…", "roles": ["store_keeper"],
  "permissions": ["inventory.balances.read", "inventory.stock.in", "…"],
  "warehouses": [
    { "id": "…", "code": "KITCHEN-01", "name": "…", "canReceive": true, "canIssue": true }
  ],
  "entitlements": { "inventory.batch_tracking": true }
}
```

The frontend uses this to show/hide controls. **This is cosmetic.** Every
endpoint re-authorizes. A security test calls each protected endpoint with a
token lacking the permission and asserts `403`, regardless of what the frontend
displays.

---

## 7. Open questions

| ID | Question |
|---|---|
| RP-TBD-01 | Should system role permission sets be editable by a `tenant_admin` in V1, or only in V2? (Currently: immutable.) |
| RP-TBD-02 | Does a `worker` need `events.consumption.manage` by default, or is recording consumption a warehouse-manager duty? |
| RP-TBD-03 | Is a separate `accounts_receiver` role needed (receiving without stock adjustments)? |
| RP-TBD-04 | Do warehouses need a hierarchy (site → zone → bin) affecting the permission model? (TBD-19) |
| RP-TBD-05 | Are approval thresholds per role or per amount band, and who defines them? (TBD-09) |
| RP-TBD-06 | Should `platform.admin` be split into several operator roles (support read-only vs full)? |
