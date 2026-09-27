# STOCKLY — WORKFLOWS

| Field | Value |
|---|---|
| Document | `WORKFLOWS.md` |
| Version | 0.1.0 |
| Status | DRAFT — Phase 0 |
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
2. Throttle check (per IP + per identity)
3. Resolve AppUser (constant time; unknown user follows the same path as a wrong
   password by hashing a dummy value to equalise timing)
4. Verify password hash (constant-time)
5. Enforce lockout / progressive delay
6. Load Active memberships for the user
      0 memberships → generic failure (audit: no_tenant_access)
      1 membership  → continue
      >1 membership → client must call /auth/select-tenant (no token yet)
7. Resolve the warehouse context (optional; validated against assignments)
8. Create AuthSession (deviceId optional) + refresh token family
9. Issue access token (15 min) + Set-Cookie refresh (HttpOnly, Secure, Strict)
10. Audit: auth.password.login (Success|Failure)
```

**Invariants:** identical response for every failure cause; no token issued for a
suspended membership; device must belong to the caller's tenant.

**Failures:** `401 invalid_credentials`, `429 rate_limited`, `403 token_invalid`
(device not in tenant).

---

## 2. Authentication — PIN (shared terminal)

**Actor:** a warehouse worker at a shared terminal.

```text
A. Open the terminal
   GET  /api/v1/terminal/devices/{deviceCode}/public-info
        → { deviceId, tenantDisplayName, displayNameRequired: true }   // no secrets
   POST /api/v1/terminal/sessions/{deviceId}/pin-challenge
        → { challengeId, expiresAt }    // nonce; defeats replay of a captured request
   GET  /api/v1/terminal/sessions/{deviceId}/identities
        → [ { membershipId, displayName, avatarRef } ]   // names only, NOT authentication

B. User selects their name
   POST /api/v1/terminal/sessions/{deviceId}/pin-auth
        { membershipId, pin, challengeId }
   1. Throttle check (per device + per membership + per IP)
   2. Challenge valid and unused
   3. Device Active + shared terminal
   4. Membership Active
   5. Membership assigned to the device's warehouse
   6. PinCredential present
   7. Verify PIN hash (constant time)
   8. MustChangePin = false (otherwise 403 pin_change_required, PIN is set here)
   9. Revoke any existing active session on the device (single-session policy)
  10. Create AuthSession (AuthenticationMethod = Pin) + refresh family
  11. Issue access token + refresh cookie
  12. Audit: auth.pin.login (Success|Failure) — never the PIN value
```

**Invariants:** the selected name is not authentication; failure responses are
indistinguishable; every failure updates the throttle ledger.

**Failures:** `401 invalid_credentials`, `403 pin_change_required`,
`404 not_found` (device/identity not available), `429 rate_limited`.

---

## 3. Session lifecycle

```text
Every authorized request:
   validate JWT (iss, aud, exp, alg)      ─┐
   load AuthSession; must be Active        │ authorization pipeline
   membership still Active                 │
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
Rules: source ≠ target; single transaction; the caller must be assigned to both.

Server, in ONE transaction:
  1. Lock both StockBalance rows in a deterministic order (WarehouseId asc)
  2. Guarded decrement in the source
  3. Guarded increment in the target
  4. Two StockTransactions sharing one transfer reference
  5. Two documents linked via Reference (V1: one document, two legs)
  6. Audit both legs
```

**In-transit modelling is TBD-22.** V1 assumes instantaneous transfer.

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
  - requires platform.* permission
  - rejects a tenant-scoped token
  - writes an audit row with Category = Platform, Severity = Warning
  - invalidates the entitlement cache
Usage evaluator (scheduled):
  recompute SubscriptionUsage; flag tenants over limit
```

---

## 16. Device administration

```text
Register device      POST /api/v1/tenant/devices            device.manage
                     { warehouseId, deviceCode, name, type }
Suspend device       POST /api/v1/tenant/devices/{id}/suspend
Revoke device        POST /api/v1/tenant/devices/{id}/revoke
                     → revokes all sessions bound to the device
Move to warehouse    POST /api/v1/tenant/devices/{id}/warehouse
                     { warehouseId }  device.assign_warehouse
                     → revokes sessions
Heartbeat            POST /api/v1/terminal/devices/{deviceId}/heartbeat
                     → updates LastSeenAtUtc (throttled)
```

---

## 17. Cross-cutting workflow rules

| Rule | Statement |
|---|---|
| W-01 | No workflow accepts a tenant identifier from the client. |
| W-02 | No workflow trusts a warehouse identifier without an assignment check. |
| W-03 | No workflow trusts a price, cost, approval state, or role from the client. |
| W-04 | Every state-changing workflow is audited in the same transaction. |
| W-05 | Every retryable workflow accepts `Idempotency-Key`. |
| W-06 | Every destructive-sounding action is actually a soft delete or a reversal. |
| W-07 | Every workflow is re-authorised at execution time, not only at navigation time. |
| W-08 | Every workflow that creates a resource also checks entitlements and usage limits. |
