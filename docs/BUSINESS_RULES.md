# STOCKLY — BUSINESS RULES

| Field | Value |
|---|---|
| Document | `BUSINESS_RULES.md` |
| Version | 1.0.0 |
| Status | FINAL DRAFT — consistent with `PROJECT_BLUEPRINT.md` v1.0 |
| Last updated | 2026-09-27 |
| Rules | **147**, verified by row count: BR-UNI 13, BR-SEC 20, BR-PIN 10, BR-ORG 7, BR-INV 29, BR-PUR 14, BR-EVT 12, BR-FOD 12, BR-SUB 8, BR-RPT 9, BR-NTF 5, BR-AUD 8 |
| Error codes | **27** documented (26 emitted + 1 reserved) — §14 |
| Related | `PROJECT_BLUEPRINT.md`, `DATABASE_DESIGN.md`, `WORKFLOWS.md` |

Rule identifiers (`BR-INV-01`, `BR-SEC-03`, …) are referenced from code, tests
and audit actions. A rule is only removed or changed via a `DECISIONS.md` entry.

---

## 1. Universal rules

| ID | Rule |
|---|---|
| BR-UNI-01 | Every business operation requires an authenticated session in an **Active** membership. |
| BR-UNI-02 | The tenant of a request is derived from the authenticated session. Any tenant identifier supplied by the client is ignored; a mismatch is `400`. |
| BR-UNI-03 | Warehouse-scoped operations require the warehouse to be in the caller's `MembershipWarehouse` set. Otherwise `404`. |
| BR-UNI-04 | A resource is loaded with a composite predicate (`Id` + `TenantId` [+ `WarehouseId`]) in the same query that reads it. Load-then-check is forbidden. |
| BR-UNI-05 | Unknown JSON properties are rejected (`400`). Server-owned fields in bodies are ignored. |
| BR-UNI-06 | All timestamps are stored UTC. `OccurredAtUtc` must not be more than 5 minutes in the future (clock-skew tolerance). |
| BR-UNI-07 | Deleting a record that appears in history is forbidden. Soft delete instead. |
| BR-UNI-08 | Every state change of a business document writes an audit record in the same transaction. |
| BR-UNI-09 | All monetary and quantity values use `decimal(18,4)`; at most 4 decimal places; no scientific notation. |
| BR-UNI-10 | Names are trimmed, 1–200 characters, and must not contain control characters. |
| BR-UNI-11 | Codes are uppercase-normalised, 1–50 characters, unique per tenant among non-deleted rows. |
| BR-UNI-12 | Any operation that can be retried safely accepts an `Idempotency-Key`; a repeat returns the original result. |
| BR-UNI-13 | A user cannot perform an action that would remove their own last administrative capability in a tenant. |

---

## 2. Identity & session rules

| ID | Rule |
|---|---|
| BR-SEC-01 | One `AppUser` per email (normalised lowercase). One optional username. |
| BR-SEC-02 | A user's access to a tenant exists only through an `Active` `TenantMembership`. |
| BR-SEC-03 | At most one **active** membership per (user, tenant). Suspended memberships are retained for audit. |
| BR-SEC-04 | Every new membership gets no roles and no warehouses by default. Least privilege is the default state. |
| BR-SEC-05 | A password change revokes all refresh families and invalidates all sessions of that user. |
| BR-SEC-06 | A membership suspension invalidates all sessions of that membership immediately. |
| BR-SEC-07 | Session idle timeout and absolute timeout are enforced server-side. Client timeouts are cosmetic. |
| BR-SEC-08 | Logout revokes the session and its whole refresh family. |
| BR-SEC-09 | "Switch user" revokes the current session before issuing a new one. A device has at most one active session by default (TBD-11). |
| BR-SEC-10 | Reusing a rotated refresh token revokes the entire family and writes a security audit event. |
| BR-SEC-11 | A password reset token is single-use, expires in 30 minutes (proposed), and is stored hashed. Using it revokes all sessions. |
| BR-SEC-12 | Failed login attempts produce a progressive delay; exceeding the configured budget causes a temporary lock. Thresholds: TBD-04. |
| BR-SEC-13 | Authentication failures are indistinguishable to the client regardless of cause (unknown user, wrong password, locked, no membership). |
| BR-SEC-14 | A device belongs to exactly one warehouse and exactly one tenant. Moving a device requires `device.assign_warehouse` and revokes its sessions. |
| BR-SEC-15 | Revoking a device revokes all its sessions immediately. |
| BR-SEC-16 | Only an actor with `tenant.users.manage` may create a membership. There is no self-registration path anywhere, including terminals. |
| BR-SEC-17 | A role may only be granted to a membership whose role `AuthorityLevel` is ≤ the granter's highest level. |
| BR-SEC-18 | A custom role's permissions must be a subset of the creator's effective permissions. |
| BR-SEC-19 | The last active `tenant_owner` of a tenant cannot be demoted, suspended or deleted. |
| BR-SEC-20 | `LastSeenAtUtc` is written at most once per 60 seconds per session. |

---

## 3. PIN rules

| ID | Rule |
|---|---|
| BR-PIN-01 | A PIN is 6–12 digits. Non-digit characters are rejected. |
| BR-PIN-02 | A PIN is never stored in plaintext, never returned by any API, and never logged. |
| BR-PIN-03 | A PIN must not equal the user's password, the tenant code, the current year, or a value in the trivial-sequence denylist (`123456`, `111111`, `000000`, `654321`, repeated digits, ascending/descending runs). |
| BR-PIN-04 | A PIN must be set before a membership can use the terminal, and must be changed on first use. |
| BR-PIN-05 | An admin PIN reset sets `MustChangePin = true`; stock permissions are blocked until the PIN is changed. |
| BR-PIN-06 | Bumping `PinCredential.Version` invalidates all sessions authenticated by that PIN. |
| BR-PIN-07 | PIN failures count against three independent budgets: per device, per membership, per IP. Exceeding any one blocks authentication. |
| BR-PIN-08 | The identity list endpoint returns only `membershipId`, `displayName`, `avatarRef`. It never reveals PIN state, roles, permissions, or account status. |
| BR-PIN-09 | Selecting a name grants nothing. The PIN authenticates the selected membership. |
| BR-PIN-10 | A membership must be `Active` and assigned to the device's warehouse to authenticate on that device. |

---

## 4. Organization rules

| ID | Rule |
|---|---|
| BR-ORG-01 | A warehouse belongs to exactly one tenant. |
| BR-ORG-02 | A warehouse code is unique per tenant among active warehouses. |
| BR-ORG-03 | A device belongs to exactly one warehouse. |
| BR-ORG-04 | A membership's warehouse assignment is required before any warehouse-scoped operation. |
| BR-ORG-05 | A warehouse with posted stock documents cannot be hard-deleted; it is deactivated. |
| BR-ORG-06 | Deactivating a warehouse prevents new operations there; historical data remains readable. |
| BR-ORG-07 | Warehouse sub-locations (aisles/bins) are out of V1 scope (TBD-19). |

---

## 5. Inventory rules

### 5.1 Movement integrity

| ID | Rule |
|---|---|
| BR-INV-01 | **There is no API to set a stock quantity.** All changes originate from documents that produce ledger transactions. |
| BR-INV-02 | Every posted line produces exactly one `StockTransaction` with a mandatory `BalanceAfter`. |
| BR-INV-03 | Balance mutation uses a single atomic guarded `UPDATE` inside an explicit transaction. |
| BR-INV-04 | `OnHandQuantity` and `ReservedQuantity` can never be negative. Enforced by the guarded `UPDATE` **and** a `CHECK` constraint. |
| BR-INV-05 | Document posting is all-or-nothing. A failure on any line rolls back the whole document. |
| BR-INV-06 | A document can be posted only from `Draft`. Double posting is rejected by the guarded status `UPDATE`. |
| BR-INV-07 | A posted document is immutable. Corrections are made by **reversal** (compensating transactions) or by a new adjustment document. |
| BR-INV-08 | A reversal creates a new document and transactions; it never deletes or edits the original. |
| BR-INV-09 | `ADJUSTMENT` and `WASTE` require a `ReasonCode`. `WASTE` requires a waste reason code specifically. |
| BR-INV-10 | A `TRANSFER` is atomic across both warehouses: source decrement and target increment in one transaction. Both sides carry the same transfer reference. |
| BR-INV-11 | A transfer is rejected if the source and target warehouses are the same, or if the membership lacks the permission in **both**. |
| BR-INV-12 | Multi-line documents lock rows in a consistent order (`LineNumber`, then `ProductId`, then `WarehouseId`) to prevent deadlocks. |
| BR-INV-13 | `StockBalance` and `InventoryLot` are rebuildable projections. A tested rebuild command recomputes them from the ledger. |
| BR-INV-14 | Ledger rows are never deleted by the application. |

### 5.2 Batches, expiry, FEFO

| ID | Rule |
|---|---|
| BR-INV-20 | A batch is unique per (tenant, product, batch number, expiry date). |
| BR-INV-21 | A product with `IsBatchTracked = true` requires a batch on every inbound line. |
| BR-INV-22 | A product with `IsExpiryTracked = true` requires an expiry date on every inbound line and a `ShelfLifeDays` on the product. |
| BR-INV-23 | Receiving an already-expired lot is rejected unless the product is quarantined by an administrator with `inventory.stock.adjust` and a reason code. *(exact expired-stock policy: TBD-07)* |
| BR-INV-24 | `ProductionDate` must not be after `ExpiryDate`. |
| BR-INV-25 | Outbound allocation is FEFO: ascending `ExpirySortKey`, then `FirstReceivedAtUtc`, then `Id`. Non-tracked products ignore expiry. |
| BR-INV-26 | Explicit batch selection is allowed only by a caller with `inventory.lots.read` plus the movement permission; FEFO is the default. |
| BR-INV-27 | Quarantined lots cannot be issued by a standard stock-out; a special permission is required. *(TBD-07)* |

### 5.3 Stock counts

| ID | Rule |
|---|---|
| BR-INV-30 | A stock count snapshots the expected quantities at `CutoffAtUtc` (the count is reconciled against that cutoff, not against live stock). |
| BR-INV-31 | Counting scope: full warehouse, category, selected products, or expiry-based. |
| BR-INV-32 | `VarianceQuantity = CountedQuantity − ExpectedQuantity`. The database `CHECK` enforces this. |
| BR-INV-33 | Posting a count generates `ADJUSTMENT` transactions for lines with a non-zero variance and a reason code. |
| BR-INV-34 | Posting a count requires `inventory.stock.count.post`, which is distinct from `inventory.stock.count`. |
| BR-INV-35 | `LastCountedAtUtc` is updated on the balance for each counted product. |
| BR-INV-36 | Submitting a count is idempotent; a repeat submission returns the original result. |

---

## 6. Purchasing rules

| ID | Rule |
|---|---|
| BR-PUR-01 | A purchase order has exactly one supplier and one receiving warehouse. |
| BR-PUR-02 | Receiving must target the PO's warehouse. |
| BR-PUR-03 | Partial receiving is allowed. `ReceivedQuantity` is monotonic and never decreases (a return to supplier is a separate flow, TBD-06). |
| BR-PUR-04 | Over-receipt is rejected unless `AllowOverReceipt = true` and the excess is within the configured tolerance (TBD-08). |
| BR-PUR-05 | An order in `Draft`, `PendingApproval`, `Rejected`, or `Cancelled` cannot be received against. |
| BR-PUR-06 | Receiving posts a `STOCK_IN` stock document in the same transaction. Inventory is never modified outside the inventory pipeline. |
| BR-PUR-07 | The actual unit cost on a receipt line becomes the newest `ProductPriceHistory` entry. |
| BR-PUR-08 | Approval is a server-side state machine with an immutable decision history. Decisions cannot be edited or deleted. |
| BR-PUR-09 | The approver of a step must hold the required permission for that step and must not be the requester, unless the tenant policy allows it (TBD-09). |
| BR-PUR-10 | An approved purchase request can be converted to a purchase order once; the link is permanent. |
| BR-PUR-11 | A purchase order total is derived from its lines; the client cannot set a total. |
| BR-PUR-12 | Supplier deletion is a soft delete; a supplier referenced by any document cannot be deleted. |
| BR-PUR-13 | Price history is append-only. |
| BR-PUR-14 | A purchase order reaches `Received` when all lines are fully received within tolerance, otherwise `PartiallyReceived`. |

---

## 7. Event rules

| ID | Rule |
|---|---|
| BR-EVT-01 | An event belongs to one tenant and has one primary operating warehouse. |
| BR-EVT-02 | `EndAtUtc > StartAtUtc`. `DurationDays ≥ 1`. |
| BR-EVT-03 | An event with posted stock documents cannot be deleted; it is cancelled. |
| BR-EVT-04 | Requirements are derived from attendance × duration × meal types × per-person factors, or entered directly as totals. The basis is recorded per requirement. |
| BR-EVT-05 | `ShortageQuantity = max(0, Required − (OnHand + Incoming))`. |
| BR-EVT-06 | `RecommendedPurchaseQuantity = ceil(ShortageQuantity / packSize)` when a pack size exists, else `ShortageQuantity`. *(pack size field TBD-10)* |
| BR-EVT-07 | A shortage snapshot is immutable; recalculation creates a new snapshot. |
| BR-EVT-08 | Consumption is recorded through a real `STOCK_OUT` document carrying `EventId`. Free-text consumption claims are not permitted. |
| BR-EVT-09 | `ConsumedQuantity` cannot exceed `PlannedQuantity × 2` without an explicit override permission and a note. |
| BR-EVT-10 | Attendance may include guests without a Stockly account (`GuestName`, `MembershipId = NULL`). |
| BR-EVT-11 | Event food cost is a snapshot; it is never recomputed silently for a completed event. |
| BR-EVT-12 | Only the event's warehouse (and warehouses with an explicit transfer plan) can fulfil event requirements. |

---

## 8. Food & costing rules

| ID | Rule |
|---|---|
| BR-FOD-01 | A recipe has a positive yield quantity and a yield unit. |
| BR-FOD-02 | `ServingsPerYield` is optional in V1; cost per serving is `null` when it is not set. |
| BR-FOD-03 | `EffectiveQuantity = QuantityPerYield × (1 + WastagePercent / 100)`. |
| BR-FOD-04 | `TotalCost = Σ (EffectiveQuantity × unitCost)`. Ingredients with no cost source contribute 0 and are flagged as `estimated`. |
| BR-FOD-05 | `CostPerYieldUnit = TotalCost / YieldQuantity`. |
| BR-FOD-06 | Inventory valuation order: moving weighted average → last purchase price → standard cost → 0 (flagged). |
| BR-FOD-07 | A cost calculation reads prices and quantities inside one transaction so the snapshot is internally consistent. |
| BR-FOD-08 | Cost snapshots are immutable. Recalculation inserts a new snapshot. |
| BR-FOD-09 | The moving average update on receipt: `NewAverage = (OldOnHand × OldAverage + ReceivedQty × ReceivedCost) / (OldOnHand + ReceivedQty)`, skipped when `OldOnHand + ReceivedQty = 0`. |
| BR-FOD-10 | Issuing stock does **not** change the moving average (moving-average accounting, not FIFO-valuation). |
| BR-FOD-11 | A recipe cannot be deleted if a completed event references it; it is deactivated. |
| BR-FOD-12 | `WastagePercent` must be between 0 and 100. |

---

## 9. Subscription & entitlement rules

| ID | Rule |
|---|---|
| BR-SUB-01 | A tenant has at most one active subscription. |
| BR-SUB-02 | Effective entitlement = plan feature overridden by `TenantEntitlement`. |
| BR-SUB-03 | Creating a resource that exceeds a `Count`/`Users`/`Warehouses`/`Storage` limit fails with a business error naming the limit. |
| BR-SUB-04 | Usage counters are incremented in the same transaction as resource creation, preventing concurrent over-provisioning. |
| BR-SUB-05 | A tenant with no active subscription, or past its grace period, is read-only: reads succeed, writes fail with a subscription error. Grace period: TBD-12. |
| BR-SUB-06 | A subscription state change is audited and invalidates the permission/entitlement cache. |
| BR-SUB-07 | Entitlement checks are server-side. The frontend's view of entitlements is advisory. |
| BR-SUB-08 | Feature keys are stable strings; removing a feature key requires a decision and a migration. |

---

## 10. Reporting rules

| ID | Rule |
|---|---|
| BR-RPT-01 | Every report is filtered by the caller's tenant and warehouse assignment by default; the filter cannot be widened by the client. |
| BR-RPT-02 | Reports never expose rows from unauthorized warehouses, even when a warehouse filter is omitted. |
| BR-RPT-03 | Cost figures require `master.products.cost.read` (or the relevant report permission). Without it, quantities are shown without values. |
| BR-RPT-04 | Large exports run as background jobs and are delivered as attachments through an authorized download endpoint. |
| BR-RPT-05 | Exports are audited with row counts and filters. |
| BR-RPT-06 | Exports are limited to a maximum row count (TBD-25) and a maximum time window. |
| BR-RPT-07 | CSV export is UTF-8 with a BOM and `;` delimiter for Arabic Excel compatibility (proposed — confirm with the owner). |
| BR-RPT-08 | Date filters are interpreted in the tenant's timezone and converted to UTC at the query boundary. |
| BR-RPT-09 | Report queries use a read-only database path and never take locks that could block operations. |

---

## 11. Notification rules

| ID | Rule |
|---|---|
| BR-NTF-01 | Notifications are generated inside the business transaction as outbox messages, then delivered by the worker. |
| BR-NTF-02 | A notification is visible only to the membership it targets, within the same tenant. |
| BR-NTF-03 | Users can mute categories; muting never suppresses security-critical notifications. |
| BR-NTF-04 | Push subscriptions belong to a membership and a device; they are never shared across tenants. |
| BR-NTF-05 | Expired notifications are pruned after a configurable period (default 90 days). |

---

## 12. Audit rules

| ID | Rule |
|---|---|
| BR-AUD-01 | Audit records are append-only. The application principal has no `UPDATE`/`DELETE` grant. |
| BR-AUD-02 | Security events are written synchronously inside the business transaction, including failures and denials. |
| BR-AUD-03 | Non-security events may be written through the outbox. |
| BR-AUD-04 | Audit payloads are redacted: no passwords, PINs, PIN hashes, tokens, keys, connection strings. |
| BR-AUD-05 | Raw before/after payloads are readable only with `reports.audit`; other roles receive a summary. |
| BR-AUD-06 | Every audit row carries `OccurredAtUtc`, actor, membership, device, session, and `CorrelationId`. |
| BR-AUD-07 | Audit retention is TBD-14. |
| BR-AUD-08 | A tenant admin cannot delete audit records. There is no API for it. |

---

## 13. Validation catalogue (cross-cutting)

| Field | Rule |
|---|---|
| Email | RFC-shaped, ≤320 chars, lowercased, unique globally |
| Username | 3–100 chars, `a-z0-9._-`, lowercased, unique |
| Password | TBD-01 |
| PIN | 6–12 digits (BR-PIN-01..03) |
| Quantity | `> 0`, ≤ 4 decimals, ≤ `decimal(18,4)` |
| Cost/price | `≥ 0`, ≤ 4 decimals; `> 0` where a price is mandatory |
| Dates | ISO-8601 on the wire; UTC in storage; `EndAt > StartAt` |
| Enums | Unknown values rejected |
| Codes | 1–50 chars, `[A-Z0-9_-]` after normalisation, unique per tenant |
| Names | 1–200 chars, trimmed, no control characters |
| Barcode | 4–64 chars, uppercase-normalised, unique per tenant |
| Page size | 1–200, default 50 |
| Page/cursor | Keyset cursor, max depth enforced |
| Request body | ≤ 1 MB (except explicit upload endpoints) |
| String fields in search | ≤ 200 chars; wildcards escaped before use in `LIKE` |
| `Idempotency-Key` | 16–100 chars, `[A-Za-z0-9_-]` |
| `If-Match` ETag | Required for updates/deletes of documents and master data |

---

## 14. Business error codes

Stable machine codes, safe to show to a user, never containing internal detail.

| Code | HTTP | Meaning |
|---|---|---|
| `invalid_credentials` | 401 | Login failed (all causes) |
| `account_locked` | 401 | **Reserved, never emitted.** Account lockout is deliberately indistinguishable from a wrong password at the API surface; the terminal shows a generic message after a documented number of attempts. Kept as a documented code so the internal audit action `auth.login.locked` and the operator UI have a stable name |
| `pin_change_required` | 428 | `PinCredential.MustChangePin` is set (admin reset): the user must set a new PIN before any warehouse operation |
| `precondition_required` | 428 | `If-Match` missing on an endpoint that requires it |
| `session_expired` | 401 | Session idle/absolute timeout |
| `session_revoked` | 401 | Session revoked |
| `token_invalid` | 401 | Signature/claims failure |
| `permission_denied` | 403 | Known resource, missing permission |
| `not_found` | 404 | Missing, or outside tenant/warehouse scope |
| `validation_failed` | 400 | Field validation errors |
| `unknown_field` | 400 | Unrecognised property (mass-assignment defence) |
| `concurrency_conflict` | 412 | `rowversion` / `If-Match` mismatch |
| `insufficient_stock` | 409 | Guarded balance update rejected |
| `document_not_postable` | 409 | Wrong status for posting |
| `lot_expired` | 409 | Receiving/issuing an expired lot |
| `batch_required` | 400 | Batch-tracked product without a batch |
| `over_receipt_exceeded` | 409 | Received quantity beyond tolerance |
| `subscription_required` | 403 | Feature not entitled (plan) |
| `subscription_read_only` | 403 | Tenant is read-only (expired/past due) |
| `usage_limit_reached` | 409 | Count/Users/Warehouses/Storage limit reached |
| `approval_required` | 409 | Document is not approved |
| `approval_forbidden` | 403 | Approver is the requester / lacks permission |
| `last_owner_protected` | 409 | Cannot remove the last tenant owner |
| `idempotency_conflict` | 409 | Same key, different payload |
| `rate_limited` | 429 | Throttled (with `Retry-After`) |
| `payload_too_large` | 413 | Body/limit exceeded |
| `unsupported_media_type` | 415 | File/content type rejected |

**Count: 27 documented codes — 26 potentially emitted, 1 reserved
(`account_locked`).** The count is the contract: adding a code is additive,
removing or repurposing one is a breaking change and needs a decision record.

---

## 15. Open business questions

Every item mirrors a §23 register entry and carries a disposition, so no rule in
this document is silently unresolved. The **rule** is stated here; only the
**policy value** is open.

| ID | Question | Resolved by | Disposition in V1 |
|---|---|---|---|
| BR-TBD-01 | Password policy specifics | TBD-01 | Owner decision; provisional ≥ 12 chars, no composition rules. The **rule** (a policy service MUST exist) is fixed |
| BR-TBD-02 | Return-to-supplier flow | TBD-06 | V1 = return to stock only; return-to-supplier is V1.1 |
| BR-TBD-03 | Expired stock handling policy | TBD-07 | V1 = expired lots cannot be issued; an admin may quarantine/receive with a reason code |
| BR-TBD-04 | Over-receipt tolerance model | TBD-08 | V1 default 0 %; over-receipt only with `AllowOverReceipt` + per-tenant percentage. Enforced by a guarded `UPDATE`, **not** a `CHECK` (ADR-0039) |
| BR-TBD-05 | Approval workflow model and thresholds | TBD-09 | One approval step by permission + optional per-tenant amount bands |
| BR-TBD-06 | Recipe servings semantics and consumption planning basis | TBD-10 | `ServingsPerYield` nullable; cost per serving `null` until set. Attendance = days + meals/day |
| BR-TBD-07 | Subscription grace period behaviour | TBD-12 | Provisional 7 days, then read-only; stored per subscription |
| BR-TBD-08 | Audit retention | TBD-14 | Provisional 24 months online, then cold archive |
| BR-TBD-09 | Report export localisation | TBD-15 | CSV language-neutral (UTF-8 BOM, `;`, code enums); PDF/Excel Arabic-first in V1.1 |
| BR-TBD-10 | Fractional quantities and base units | TBD-16 | Safe assumption: supported from V1, `decimal(18,4)`, 0–4 decimal places |
| BR-TBD-11 | Barcode symbologies | TBD-18 | Design decision (ADR-0043): the six modelled values |
| BR-TBD-12 | Warehouse sub-locations | TBD-19 | V1 has none; additive in V1.1 |
| BR-TBD-13 | Transfer in-transit model | TBD-22 | V1 = instantaneous transfer; no virtual warehouse |
| BR-TBD-14 | CSV delimiter/encoding for Arabic Excel | TBD-15 | **Resolved by BR-RPT-07:** UTF-8 with BOM, `;` delimiter, `.` decimal separator, ISO dates. Listed here only for traceability — it is not open |
