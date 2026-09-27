# STOCKLY — WORKFLOWS

| Field | Value |
|---|---|
| Document | `WORKFLOWS.md` |
| Version | 1.0.0 |
| Status | FINAL DRAFT — consistent with `PROJECT_BLUEPRINT.md` v1.0 |
| Last updated | 2026-09-27 |
| Related | `BUSINESS_RULES.md`, `API_CONVENTIONS.md`, `ROLES_PERMISSIONS.md` |

Each workflow lists: actor, preconditions, permission, steps (API), invariants,
audit actions, and failure handling.

---

## 1. Authentication — password (office / admin)

**Actor:** an admin or office user on a personal device.
**Permission:** none (anonymous endpoint, heavily throttled).

```text
1. POST /api/v1/auth/login
      { emailOrUsername, password, deviceId? }
2. Throttle check (per IP + per identity; the identity key is
   HMAC-SHA256(identifier, pepper), never the plaintext)
3. Resolve AppUser (constant time; unknown user follows the same path as a wrong
   password by hashing a dummy value to equalise timing)
4. Verify password hash (constant-time, Argon2id)
5. Enforce lockout / progressive delay
6. Load Active memberships for the user
      0 memberships → generic failure (audit: auth.login.no_tenant_access)
                      → a platform operator uses §1b instead
      1 membership  → continue
      >1 membership → client must call /auth/select-tenant (no token yet)
7. Resolve the warehouse context (optional; validated against assignments —
   an unassigned warehouse is 404, not 400)
8. Create AuthSession (SessionScope = Tenant, deviceId optional) + refresh family
9. Issue access token (15 min, scp=tenant) + Set-Cookie refresh (HttpOnly, Secure, Strict)
10. Audit: auth.password.login (Success|Failure)
```

**Invariants:** identical response for every failure cause; no token issued for a
suspended membership; device must belong to the caller's tenant.

**Failures:** `401 invalid_credentials`, `429 rate_limited`.

---

## 1b. Authentication — platform operator

**Actor:** a person with `AppUser.IsPlatformAdmin = true`, who may belong to no
tenant at all. Without this flow the platform cannot be operated (ADR-0034).

```text
1. POST /api/v1/platform/auth/login
      { emailOrUsername, password }
2. Identical throttle, password verification, and lockout as §1
3. Require IsPlatformAdmin = true
      false → the same generic invalid_credentials response; no session row
4. Create AuthSession (SessionScope = Platform, TenantId = NULL, MembershipId = NULL)
5. Issue access token (15 min, scp=platform, no tid/mid claim) + refresh cookie
6. Audit: platform.auth.login (Success|Failure)
```

**Invariants:** a platform token cannot reach any tenant route, and a tenant
token cannot reach any platform route. There is no cross-scope switch: the
operator signs in again on the tenant path if they also work there.

**Failures:** `401 invalid_credentials` (identical to a wrong password),
`429 rate_limited`.

---

## 2. Authentication — PIN (shared terminal)

**Actor:** a warehouse worker at a shared terminal.
**Addressing:** the terminal is identified by its 128-bit **enrollment code**,
not by `deviceId` and never by the human `DeviceCode` (ADR-0035). A terminal URL
looks like `https://terminal.stockly/…/k7m2qp9xr4td`.

```text
A. Open the terminal
   GET  /api/v1/terminal/sessions/{enrollmentCode}/public-info
        → { tenantDisplayName, branding, displayNameRequired: true }  // no secrets
   POST /api/v1/terminal/sessions/{enrollmentCode}/pin-challenge
        → { challengeId, expiresAt }    // nonce; defeats replay of a captured request
   GET  /api/v1/terminal/sessions/{enrollmentCode}/identities
        → [ { membershipId, displayName, avatarRef } ]   // names only, NOT authentication
        // only memberships assigned to this device's warehouse; rate limited per
        // IP and per code; an unknown code is indistinguishable from an empty list

B. User selects their name
   POST /api/v1/terminal/sessions/{enrollmentCode}/pin-auth
        { membershipId, pin, challengeId }
   1. Throttle check (per enrollment code + per device + per membership + per IP)
   2. Challenge valid and unused
   3. Enrollment code resolves to a device that is Active + shared terminal
   4. Membership Active
   5. Membership assigned to the device's warehouse
   6. PinCredential present
   7. Verify PIN hash (constant time)
   8. MustChangePin = false (otherwise 428 pin_change_required; the PIN is set here)
   9. Revoke any existing active session on the device (single-session policy)
  10. Create AuthSession (SessionScope = Tenant, AuthenticationMethod = Pin) + refresh family
  11. Issue access token (scp=tenant) + refresh cookie
  12. Audit: auth.pin.login (Success|Failure) — never the PIN value
```

**Invariants:** the selected name is not authentication; failure responses are
indistinguishable; every failure updates the throttle ledger; the identities
response never contains roles, permissions, PIN state, or account status.

**Failures:** `401 invalid_credentials`, `428 pin_change_required`,
`404 not_found` (unknown code, revoked device, or identity not available for this
device — all identical), `429 rate_limited`.

**Residual risk (SEC-KNW-08):** holding the enrollment code still reveals a
staffing list. Mitigations are rate limiting, a rotatable code, a minimal
response, and full audit; it is not eliminated in V1.

---

## 3. Session lifecycle

```text
Every authorized request:
   validate JWT (iss, aud, exp, alg)      ─┐
   load AuthSession; must be Active        │ authorization pipeline
   scp matches the route's scope            │  (tenant routes reject scp=platform
   membership still Active                 │   and platform routes reject scp=tenant)
   entitlement allows the module           │
   permission present                      │
   warehouse assigned                      │
   resource owned by tenant/warehouse     ─┘

Idle timeout        → session Expired, access denied (client auto-locks)
Absolute timeout    → session Expired
Logout              → session Revoked + family revoked + audit
Switch user         → revoke current → authenticate new
Device revoked      → all its sessions revoked (immediate)
Membership suspended→ all its sessions revoked (immediate)
Role change         → permission cache invalidated (immediate)
Token refresh       → rotate; reuse of a rotated token ⇒ whole family revoked + audit
Heartbeat           → server-enforced idle-window extension; a client cannot
                      extend a session past the absolute timeout or revive an
                      Expired/Revoked session
```

---

## 4. Goods receiving (purchase)

```text
A. Request
   POST /api/v1/purchasing/purchase-requests
        { warehouseId, requiredByDate, lines[] }            purchasing.requests.create
   POST /api/v1/purchasing/purchase-requests/{id}/submit  → PendingApproval
   POST /api/v1/purchasing/purchase-requests/{id}/approve
        { decision, comments }                             purchasing.requests.approve
        - approver ≠ requester (TBD-09)
        - threshold checks
   POST /api/v1/purchasing/purchase-orders   (from an approved request)
        { supplierId, warehouseId, lines[] }              purchasing.orders.create
   POST /api/v1/purchasing/purchase-orders/{id}/submit → PendingApproval
   POST /api/v1/purchasing/purchase-orders/{id}/approve                orders.approve
        → Approved

B. Physical receipt
   POST /api/v1/purchasing/goods-receipts                   orders.receive
        { purchaseOrderId, warehouseId, deliveryNoteNumber,
          lines: [ { productId, quantity, unitId, unitCost,
                     batchNumber?, expiryDate?, productionDate? } ] }
        Header: Idempotency-Key: <uuid>
   Server, in ONE transaction:
        1. Order must be Approved / PartiallyReceived
        2. Warehouse must equal the PO warehouse
        3. Over-receipt check (tolerance / AllowOverReceipt)
        4. Create/resolve Batch per line (batch + expiry unique per tenant)
        5. Create GoodsReceipt (Draft → Posted)
        6. Create inventory.StockDocument(STOCK_IN) + lines
        7. For each line: guarded balance UPDATE (+), InventoryLot upsert (+)
        8. Insert StockTransaction rows with BalanceAfter
        9. Update moving average cost
       10. Insert ProductPriceHistory
       11. Update PO line ReceivedQuantity (guarded monotonic UPDATE)
       12. Audit + outbox notification
   Result: order → PartiallyReceived or Received
```

**Invariants:** inventory is only ever changed through the inventory pipeline;
partial receiving is safe and idempotent; the same idempotency key returns the
original receipt.

**Failures:** `409 over_receipt_exceeded`, `409 approval_required`,
`400 batch_required`, `409 lot_expired`, `429 rate_limited`.

---

## 5. Stock out (issue / consumption)

```text
POST /api/v1/inventory/stock-out
     { warehouseId, lines: [ { productId, quantity, unitId, batchId?, reasonCodeId? } ],
       eventId?, reference?, occurredAt?, notes? }
     Header: Idempotency-Key: <uuid>
Permission: inventory.stock.out  AND warehouse ∈ MembershipWarehouse
            AND MembershipWarehouse.CanIssue

Server, in ONE transaction:
  1. Validate the request; reject unknown fields
  2. Resolve the stock document (Draft)
  3. For each line (ordered):
       a. Allocate FEFO across InventoryLot rows
          (expiry → received → id), or the explicit batch
       b. For each allocation:
            guarded UPDATE StockBalance
              SET OnHand = OnHand - @qty
             WHERE Id = @id AND TenantId = @tid AND WarehouseId = @wid
               AND OnHand >= @qty          -- 0 rows ⇒ insufficient_stock
            guarded UPDATE InventoryLot
            INSERT StockTransaction (Direction = Out, BalanceAfter = new balance)
  4. Update document status → Posted, totals
  5. If eventId present: update EventConsumption (consumed quantity)
  6. Audit + outbox
```

**Invariants:** stock never goes negative; every line leaves a ledger trace with
a running balance; FEFO is the default allocation.

**Failures:** `409 insufficient_stock` (with the failing product identified in a
safe error payload), `404` (warehouse not assigned), `403` (no permission),
`409 lot_expired`.

---

## 6. Stock in (manual, not from purchasing)

```text
POST /api/v1/inventory/stock-in
     { warehouseId, lines: [...], reasonCodeId, reference?, occurredAt? }
Permission: inventory.stock.in  AND MembershipWarehouse.CanReceive
Same posting pipeline as §5 with Direction = In.
```

---

## 7. Warehouse transfer

```text
POST /api/v1/inventory/transfers
     { sourceWarehouseId, targetWarehouseId, lines: [...] }
Permission: inventory.stock.transfer in BOTH warehouses
Rules: source ≠ target; single transaction; the caller must be assigned to both;
       an unassigned warehouse is 404 (never 400 — no warehouse probing).

Server, in ONE transaction:
  1. Lock both StockBalance rows in a deterministic order (WarehouseId asc) to
     make the two-warehouse deadlock impossible
  2. Guarded decrement in the source (absolute invariant: OnHand >= 0)
  3. Guarded increment in the target
  4. ONE StockDocument (MovementType = TRANSFER) with WarehouseId = source and
     CounterWarehouseId = target — the two legs are not two documents
  5. Two StockTransaction rows sharing the document reference, one negative
     (source) and one positive (target)
  6. InventoryLot rows updated in both warehouses (FEFO keys preserved)
  7. Audit both legs (stock.transfer.posted) + outbox notification
```

**Invariants:** either both warehouses move or neither does; the document's
`CounterWarehouseId` is required when `MovementType = TRANSFER` and forbidden
otherwise; balances are never set directly, only guarded.

**In-transit modelling is §23 TBD-22 (owner decision).** V1 assumes an
instantaneous transfer; a virtual in-transit warehouse is V2.

**Failures:** `409 insufficient_stock`, `409 document_not_postable`,
`429 rate_limited`.

---

## 8. Adjustment and waste

```text
POST /api/v1/inventory/adjustments
     { warehouseId, direction: In|Out, reasonCodeId (required), lines: [...] }
Permission: inventory.stock.adjust

POST /api/v1/inventory/waste
     { warehouseId, reasonCodeId (required, waste category), lines: [...],
       notes (required) }
Permission: inventory.stock.waste
```

Both post through the same pipeline. Waste is separately reportable. Adjustments
of a size beyond a tenant threshold may require approval — **TBD-09**.

---

## 9. Correction / reversal

```text
POST /api/v1/inventory/stock-documents/{id}/reverse
     { reasonCodeId, notes }
Permission: the same permission as the original movement type, plus
            inventory.stock.adjust for adjustments

Server:
  1. Original document must be Posted (not already reversed)
  2. Create a NEW document (ReversesDocumentId = original), same warehouse/type
  3. Lines are mirrored with opposite direction
  4. Guarded balance updates; ledger rows with IsReversal = 1
  5. Original marked Status = Reversed (status only; no data changed)
  6. Audit: stock.document.reversed
```

Nothing is ever deleted or edited. History stays provable.

---

## 10. Stocktake (cycle count)

```text
1. Create count
   POST /api/v1/inventory/stock-counts
        { warehouseId, scopeType, categoryId?, productIds? }
        → snapshots nothing yet; stores the cutoff time (default = now)
   Server computes and stores ExpectedQuantity per line from the ledger at
   CutoffAtUtc (see BR-INV-30)

2. Count
   PATCH /api/v1/inventory/stock-counts/{id}/lines/{lineId}
        { countedQuantity }
   Permission: inventory.stock.count

3. Submit
   POST /api/v1/inventory/stock-counts/{id}/submit
        Header: Idempotency-Key

4. Post (creates adjustments)
   POST /api/v1/inventory/stock-counts/{id}/post
        { reasonCodeId, notes }
   Permission: inventory.stock.count.post   ← strictly stronger than step 2
   Server, in ONE transaction:
        - Lines with variance ≠ 0 produce an ADJUSTMENT document
        - Guarded balance updates to the counted quantity
        - Ledger rows; LastCountedAtUtc updated
        - StockCount status → Posted
        - Audit
```

**Invariants:** the count reconciles against the cutoff snapshot, not live stock,
so concurrent operations cannot corrupt the count; the person counting may not
be the person posting (configurable, default on).

---

## 11. Event planning → shortage → purchase recommendation

```text
1. Create event
   POST /api/v1/events  { code, name, warehouseId, startAtUtc, endAtUtc,
                          expectedAttendees, type }

2. Attendance
   POST /api/v1/events/{id}/attendance   [ { membershipId?|guestName, daysAttending, mealsPerDay } ]

3. Requirements — either derived or entered
   POST /api/v1/events/{id}/requirements/derive
        → uses meal types, duration, attendance and recipe-based consumption
   POST /api/v1/events/{id}/requirements        (manual)

4. Shortage calculation
   POST /api/v1/events/{id}/shortage/calculate
        Permission: events.shortage.recalculate
        For each required product:
            available = StockBalance.OnHand + StockBalance.Incoming (open POs)
            shortage  = max(0, required - available)
            recommended = shortage (optionally rounded to pack size — TBD-10)
        Inserts an immutable EventShortageSnapshot

5. Recommendation → purchase request
   POST /api/v1/events/{id}/shortage/{snapshotId}/create-requests
        Permission: purchasing.requests.create
        Creates draft PurchaseRequests (the user reviews and submits them)
```

**Invariants:** shortage is a snapshot, never live; recommendations never post
inventory; generating a request requires the purchasing permission, not just the
events permission.

---

## 12. Recipe and food costing

```text
1. Meal types      POST /api/v1/food/meal-types
2. Recipe          POST /api/v1/food/recipes
                     { code, name, mealTypeId, yieldQuantity, yieldUnitId,
                       servingsPerYield?, instructions? }
3. Items           POST /api/v1/food/recipes/{id}/items
                     { productId, quantityPerYield, unitId, wastagePercent }

4. Cost the recipe
   POST /api/v1/food/recipes/{id}/cost
   In ONE transaction (consistent snapshot):
       for each item:
           qty       = quantityPerYield * (1 + wastage/100)
           unitCost  = valuation(product, warehouse)
           line      = qty * unitCost
       total   = Σ line
       perUnit = total / yieldQuantity
       perServ = servingsPerYield ? total / servingsPerYield : null
       insert RecipeCostSnapshot (immutable) with a provenance summary
5. Event food cost
   POST /api/v1/events/{id}/food-cost
       { mealTypeId?, totalServings? }
       → EventFoodCostSnapshot (immutable, current flag flipped)
```

**Invariants:** a snapshot is never rewritten; estimated costs are explicitly
flagged; cost-per-serving is `null` when `servingsPerYield` is unset.

---

## 13. Notifications

```text
Business transaction → INSERT ops.OutboxMessage
Worker (every few seconds):
   claim batch (UPDLOCK, ROWLOCK) → dispatch
     Notification INSERT (in-app)
     Web push (if a subscription exists and the category is enabled)
   success → Processed
   failure → Attempts++, NextAttemptAtUtc backoff
   exhausted → DeadLetter + alert
User action: mark read, mute category
Security-category notifications bypass user muting.
```

---

## 14. Reporting and export

```text
1. GET /api/v1/reports/{report}?filters…   (synchronous, paged, capped)
   - Tenant forced from the session
   - Warehouse list forced to the caller's assignments
   - Cost columns require a cost permission
2. POST /api/v1/reports/{report}/exports   (asynchronous for large sets)
   → OutboxMessage job → generates PDF/Excel/CSV → Attachment
3. GET  /api/v1/reports/exports/{id}/download
   → re-authorizes the caller before streaming
4. Audit: report.exported with filters and row count
```

---

## 15. Platform administration

```text
Create tenant            POST /api/v1/platform/tenants
Assign plan              POST /api/v1/platform/tenants/{id}/subscription
Override entitlement     POST /api/v1/platform/tenants/{id}/entitlements
Deactivate tenant        POST /api/v1/platform/tenants/{id}/deactivate

Every platform action:
  - requires a platform-scoped token (scp=platform) AND the specific
    platform.* code; a tenant-scoped token is rejected before authorization
  - writes an audit row with Category = Platform, Severity = Warning, including
    the target tenant id and the before/after values
  - invalidates the entitlement cache
  - is never reachable with a body-supplied tenant scope; the target tenant is a
    path parameter and is re-validated as an active platform target
Usage evaluator (scheduled):
  recompute SubscriptionUsage; flag tenants over limit
```

**Invariants:** a platform operator who also holds a tenant membership still
cannot perform tenant operations with the platform token; they sign in on the
tenant path. `IsPlatformAdmin` is the only way to set another user's
`IsPlatformAdmin`, and it is audited (SEC-KNW-09).

---

## 16. Device administration

```text
Register device      POST /api/v1/tenant/devices            device.manage
                     { warehouseId, deviceCode, name, type }
                     → server generates a 128-bit EnrollmentCode (base32url) and
                       returns it ONCE, in plaintext, for printing/QR. It is
                       stored in clear (it is a lookup key) and hashed in the
                       throttle ledger. The client never chooses it.
Rotate code          POST /api/v1/tenant/devices/{id}/rotate-code   device.manage
                     → new code, old code dies immediately, audit
                     → use when a code is exposed (lost/photographed terminal)
Suspend device       POST /api/v1/tenant/devices/{id}/suspend
Revoke device        POST /api/v1/tenant/devices/{id}/revoke
                     → revokes all sessions bound to the device and refuses
                       the enrollment code
Move to warehouse    POST /api/v1/tenant/devices/{id}/warehouse
                     { warehouseId }  device.assign_warehouse
                     → revokes sessions
Heartbeat            POST /api/v1/terminal/sessions/{enrollmentCode}/heartbeat
                     → updates LastSeenAtUtc (throttled) and extends the idle
                       window within the absolute timeout
```

**Invariants:** `DeviceCode` is a human label only — it is never accepted as an
authentication key and is not unique enough to be one. All terminal routes are
addressed by `EnrollmentCode`. A revoked device's code returns the same generic
`404 not_found` as an unknown code.

**Failures:** `404 not_found`, `409 conflict` (code rotation while a device is
suspended), `429 rate_limited`.

---

## 16b. Device enrollment (operator procedure)

```text
1. Device created in a tenant (device.manage); the device is provisioned in
   stockout state, not yet registered
2. System generates EnrollmentCode and prints a label (QR + short human code)
3. Operator activates the terminal and scans/typed the code at
   /api/v1/terminal/sessions/{code}/public-info
4. Terminal shows the tenant branding; operator confirms
5. First authentication is a PIN set on that terminal; the PIN is not chosen by
   the owner but by the worker during pin-challenge flow (428 flow)
6. Rotation: device.manage rotates the code; the previous code stops working
   immediately and existing sessions are revoked
7. Loss/theft: revoke the device; the code is dead and all sessions end
```

Audit: `device.registered`, `device.code_rotated`, `device.revoked` with the
actor and the affected device. The enrollment code itself is never logged, never
emailed, and never returned by a list or get endpoint (only the masked hint).

---

## 17. Cross-cutting workflow rules

| Rule | Statement |
|---|---|
| W-01 | No workflow accepts a tenant identifier from the client. |
| W-02 | No workflow trusts a warehouse identifier without an assignment check; an unassigned warehouse is 404, not 400. |
| W-03 | No workflow trusts a price, cost, approval state, or role from the client. |
| W-04 | Every state-changing workflow is audited in the same transaction. |
| W-05 | Every retryable workflow accepts `Idempotency-Key`. |
| W-06 | Every destructive-sounding action is actually a soft delete or a reversal. |
| W-07 | Every workflow is re-authorised at execution time, not only at navigation time. |
| W-08 | Every workflow that creates a resource also checks entitlements and usage limits. |
| W-09 | No workflow crosses a token scope: a platform token never reaches a tenant route and a tenant token never reaches a platform route (ADR-0034). |
| W-10 | No workflow accepts a raw identifier as an authentication key: the terminal uses `EnrollmentCode`, the server uses `Id`, and the throttle ledger uses HMACs. |
| W-11 | No workflow mutates a balance directly; every quantity change goes through a document → guarded update → immutable ledger row. |
| W-12 | No workflow writes a tenant-scoped row without the `TenantId` predicate in the SQL, not only in C#. |
