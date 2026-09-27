# STOCKLY — API CONVENTIONS

| Field | Value |
|---|---|
| Document | `API_CONVENTIONS.md` |
| Version | 1.0.0 |
| Status | FINAL DRAFT — consistent with `PROJECT_BLUEPRINT.md` v1.0 |
| Last updated | 2026-09-27 |
| Related | `WORKFLOWS.md`, `SECURITY_ARCHITECTURE.md`, `ROLES_PERMISSIONS.md` |

---

## 1. Base and versioning

```text
Base path:   /api/v1
OpenAPI:     /api-docs/openapi.json      (always, sanitised)
Swagger UI:  /swagger                    (Development/Staging only)
```

- Versioning is in the path. A breaking change ⇒ `/api/v2`. Non-breaking
  additions may ship within `v1`.
- No endpoint silently changes the meaning of an existing field.

---

## 2. The one rule that outranks everything else

**The API never trusts the client for identity, tenant, warehouse, permission,
role, price, quantity, cost, or approval state.**

- `TenantId` is **never** accepted from a request body, query string, route, or
  header. It comes from the validated token/session. If a client sends one, the
  field is rejected as an unknown field.
- `WarehouseId` **may** be sent (the user picks a warehouse), but it is always
  validated against `MembershipWarehouse` for the current membership.
- Prices, costs, totals, approval decisions, role and permission sets, and
  quantities that the server derives are never accepted from the client. Where a
  field is server-owned, sending it results in `400 unknown_field`.

---

## 3. Authentication

```http
Authorization: Bearer <access-token>
```

Refresh is a cookie, not a body field:

```http
POST /api/v1/auth/refresh
Cookie: stk_refresh=<opaque>

Set-Cookie: stk_refresh=<opaque>; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth; Max-Age=...
```

- Access token: JWT, 15 minutes, in-memory only in the PWA.
- Refresh token: opaque, rotating, `HttpOnly` cookie scoped to the auth path.
- Tokens MUST NOT appear in URLs, logs, `localStorage`, or error messages.
- `GET /api/v1/auth/me` returns the current identity, roles, permissions,
  warehouses, and entitlements (advisory for the UI; the server still
  authorises every call).
- **Token scope is explicit.** Every access token carries `scp` = `tenant` or
  `platform` (ADR-0034). A `platform` token is rejected by every route outside
  `/api/v1/platform/**`; a `tenant` token is rejected by every route inside it.
  The scope is checked by middleware against the live `AuthSession` — never
  trusted from the token alone.
- **Warehouse scope is explicit.** A route that takes `warehouseId` returns
  **`404`** when that warehouse is not in the caller's `MembershipWarehouse`.
  `400` is reserved for a malformed value in a warehouse the caller *is*
  assigned to. Rationale in `ROLES_PERMISSIONS.md` §4.1.

---

## 4. Endpoints

### 4.1 Auth

| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `/auth/login` | anonymous | password login |
| POST | `/auth/refresh` | cookie | rotate refresh token |
| POST | `/auth/logout` | bearer | revoke session + family |
| POST | `/auth/select-tenant` | anonymous (post-credential) | choose membership when multi-tenant |
| GET | `/auth/me` | bearer | identity + effective capabilities |
| POST | `/auth/change-password` | bearer | requires current password |
| POST | `/auth/forgot-password` | anonymous | always responds identically |
| POST | `/auth/reset-password` | anonymous | single-use token |

### 4.2 Terminal (shared device)

The terminal is addressed by its **enrollment code** (128-bit random, rotatable —
ADR-0035), never by `deviceId` or the human `DeviceCode`. A terminal URL is
delivered out of band, e.g. `https://terminal.stockly/…/k7m2qp9xr4td`.

| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `/terminal/sessions/{enrollmentCode}/public-info` | anonymous | tenant display name + branding only |
| GET | `/terminal/sessions/{enrollmentCode}/identities` | anonymous, device-scoped | `{ membershipId, displayName, avatarRef }` **only** — never an authentication grant, never roles/PIN status. Rate limited per IP and per code; identical 404/200 timing for an unknown code |
| POST | `/terminal/sessions/{enrollmentCode}/pin-challenge` | anonymous | nonce, short TTL |
| POST | `/terminal/sessions/{enrollmentCode}/pin-auth` | anonymous | PIN verification. Responds `428 pin_change_required` when `PinCredential.MustChangePin` is set — the user must set a new PIN before any work |
| POST | `/terminal/sessions/{enrollmentCode}/switch-user` | bearer | revoke + re-authenticate |
| POST | `/terminal/sessions/{enrollmentCode}/heartbeat` | bearer | throttled last-seen + idle-window extension. The code is re-resolved to a device, so a rotated or revoked code fails with the same generic `404` |

### 4.3 Tenant administration

```text
GET    /api/v1/tenant/memberships
POST   /api/v1/tenant/memberships
PATCH  /api/v1/tenant/memberships/{id}
POST   /api/v1/tenant/memberships/{id}/suspend
POST   /api/v1/tenant/memberships/{id}/reactivate
POST   /api/v1/tenant/memberships/{id}/roles
DELETE /api/v1/tenant/memberships/{id}/roles/{roleId}
POST   /api/v1/tenant/memberships/{id}/warehouses
DELETE /api/v1/tenant/memberships/{id}/warehouses/{warehouseId}
GET    /api/v1/tenant/roles
POST   /api/v1/tenant/roles
PUT    /api/v1/tenant/roles/{id}/permissions
GET    /api/v1/tenant/warehouses
POST   /api/v1/tenant/warehouses
GET    /api/v1/tenant/devices
POST   /api/v1/tenant/devices
POST   /api/v1/tenant/devices/{id}/suspend|revoke
GET    /api/v1/tenant/audit
GET    /api/v1/tenant/settings
PUT    /api/v1/tenant/settings
```

### 4.4 Platform

```text
POST   /api/v1/platform/auth/login        (anonymous, pre-token)
POST   /api/v1/platform/auth/refresh      (cookie)
POST   /api/v1/platform/auth/logout       (bearer, platform scope)
GET    /api/v1/platform/tenants
POST   /api/v1/platform/tenants
GET    /api/v1/platform/plans
POST   /api/v1/platform/plans
PUT    /api/v1/platform/plans/{id}
POST   /api/v1/platform/tenants/{id}/subscription
PUT    /api/v1/platform/tenants/{id}/entitlements/{featureKey}
GET    /api/v1/platform/tenants/{id}/usage
GET    /api/v1/platform/audit
GET    /api/v1/platform/sessions
```

Every route in this block requires a token with `scp = platform`, which is issued
only by `POST /platform/auth/login` for a user with
`AppUser.IsPlatformAdmin = true` (ADR-0034). Rules:

- A tenant-scoped token is rejected here even if the caller holds a role named
  similarly.
- A platform-scoped token is rejected on every non-`/platform/**` route, **even
  if the same user also has an active tenant membership**. There is no "switch to
  tenant" from a platform token; the operator signs in again on the tenant path.
- `TenantId` is taken from the request path, never from a body field or a
  client-supplied header; a platform route may act on any tenant precisely
  because the caller's authority is platform-wide, and every such call is audited
  with the target tenant id.

### 4.5 Master data

```text
GET|POST        /api/v1/master/categories
GET|PATCH|DELETE /api/v1/master/categories/{id}
GET|POST        /api/v1/master/units
POST            /api/v1/master/unit-conversions
GET|POST        /api/v1/master/products
GET|PATCH|DELETE /api/v1/master/products/{id}
POST            /api/v1/master/products/{id}/barcodes
GET|POST        /api/v1/master/reason-codes
GET             /api/v1/master/search?q=            (products, suppliers, events)
```

### 4.6 Inventory

```text
GET  /api/v1/inventory/balances?warehouseId&productId&lowStockOnly
GET  /api/v1/inventory/lots?warehouseId&productId&expiringWithinDays
GET  /api/v1/inventory/documents?warehouseId&type&from&to
GET  /api/v1/inventory/documents/{id}
POST /api/v1/inventory/stock-in
POST /api/v1/inventory/stock-out
POST /api/v1/inventory/returns
POST /api/v1/inventory/transfers
POST /api/v1/inventory/adjustments
POST /api/v1/inventory/waste
POST /api/v1/inventory/documents/{id}/post
POST /api/v1/inventory/documents/{id}/reverse
GET  /api/v1/inventory/ledger?productId&warehouseId&from&to
GET|POST        /api/v1/inventory/stock-counts
GET  /api/v1/inventory/stock-counts/{id}
PATCH /api/v1/inventory/stock-counts/{id}/lines/{lineId}
POST /api/v1/inventory/stock-counts/{id}/submit
POST /api/v1/inventory/stock-counts/{id}/post
GET  /api/v1/inventory/movements/meta        (enums, reason codes, units)
```

**There is no `PUT /inventory/balances` or any endpoint that sets a quantity.**

### 4.7 Purchasing

```text
GET|POST            /api/v1/purchasing/suppliers
GET|PATCH|DELETE    /api/v1/purchasing/suppliers/{id}
GET|POST            /api/v1/purchasing/supplier-products
GET                 /api/v1/purchasing/prices?productId
GET|POST            /api/v1/purchasing/requests
GET|PATCH|DELETE    /api/v1/purchasing/requests/{id}
POST                /api/v1/purchasing/requests/{id}/submit
POST                /api/v1/purchasing/requests/{id}/approve
POST                /api/v1/purchasing/requests/{id}/convert
GET|POST            /api/v1/purchasing/orders
GET|PATCH           /api/v1/purchasing/orders/{id}
POST                /api/v1/purchasing/orders/{id}/submit
POST                /api/v1/purchasing/orders/{id}/approve
POST                /api/v1/purchasing/orders/{id}/cancel
GET|POST            /api/v1/purchasing/receipts
GET                 /api/v1/purchasing/receipts/{id}
```

### 4.8 Events, Food, Reports, Notifications

```text
GET|POST        /api/v1/events
GET|PATCH       /api/v1/events/{id}
POST            /api/v1/events/{id}/cancel
GET|POST        /api/v1/events/{id}/attendance
GET|POST        /api/v1/events/{id}/requirements
POST            /api/v1/events/{id}/requirements/derive
POST            /api/v1/events/{id}/shortage/calculate
GET             /api/v1/events/{id}/shortage
POST            /api/v1/events/{id}/shortage/{snapshotId}/create-requests
POST            /api/v1/events/{id}/food-cost

GET|POST        /api/v1/food/meal-types
GET|POST        /api/v1/food/recipes
GET|PATCH|DELETE /api/v1/food/recipes/{id}
GET|POST        /api/v1/food/recipes/{id}/items
POST            /api/v1/food/recipes/{id}/cost
GET             /api/v1/food/recipes/{id}/cost

GET             /api/v1/reports/{reportName}
POST            /api/v1/reports/{reportName}/exports
GET             /api/v1/reports/exports
GET             /api/v1/reports/exports/{id}/download

GET             /api/v1/notifications
POST            /api/v1/notifications/{id}/read
POST            /api/v1/notifications/read-all
GET|PUT         /api/v1/notifications/preferences
POST            /api/v1/notifications/push-subscriptions
DELETE          /api/v1/notifications/push-subscriptions/{id}
```

`{reportName}` is an enum: `dashboard`, `inventory`, `movement`, `consumption`,
`waste`, `purchasing`, `suppliers`, `price-history`, `events`, `user-activity`,
`audit`.

---

## 5. Request conventions

### 5.1 Headers

| Header | Direction | Purpose |
|---|---|---|
| `Authorization: Bearer …` | request | Access token |
| `Accept-Language: ar` | request | Response language for `NameAr`-backed fields |
| `X-Correlation-Id: <uuid>` | both | Echoed; generated when absent |
| `Idempotency-Key: <16–100 chars>` | request | Required on posting operations |
| `If-Match: "<etag>"` | request | Optimistic concurrency on updates/deletes |
| `Retry-After`, `X-RateLimit-*` | response | Throttling information |
| `ETag: "<rowversion>"` | response | Concurrency token |
| `Cache-Control: no-store` | response | On all authenticated responses |

### 5.2 Pagination

```http
GET /api/v1/inventory/balances?pageSize=50&cursor=eyJpZCI6Ii4uIn0
```

```json
{
  "items": [ … ],
  "pageSize": 50,
  "nextCursor": "eyJpZCI6Ii4uIn0",
  "hasMore": true,
  "estimatedTotal": 1234
}
```

Keyset (cursor) pagination, not `OFFSET`. `pageSize` 1–200, default 50.
`estimatedTotal` is explicitly an estimate, never a blocking `COUNT(*)`.

### 5.3 Filtering and sorting

```text
?warehouseId=…            single
?productIds=a,b,c         multiple (max 100)
?from=2026-01-01&to=2026-01-31   dates, interpreted in the tenant timezone
?includeDeleted=true      requires the *resource's own* `*.manage` permission
                          (e.g. `master.products.manage` for products,
                          `tenant.users.manage` for users) — never a
                          single global permission
?sort=-postedAt,name      allowlisted field names only
```

> **Corrected:** the previous value was `requires tenant.roles.manage`. That made
> role administration the gate for reading deleted products, which is neither
> necessary (too narrow: a store keeper legitimately reconciles deleted items)
> nor sufficient (too broad in spirit: unrelated privilege standing in for
> data access). The rule is uniform instead: seeing soft-deleted rows of a
> resource requires managing that resource.

- Unknown query parameters are **rejected** (`400`), preventing parameter
  pollution and contract drift.
- Sort fields are allowlisted; arbitrary field names are rejected (they would
  otherwise be an injection vector into `ORDER BY`).
- Search text has `LIKE` wildcards escaped server-side.

### 5.4 Bodies

- `POST`/`PATCH` bodies are JSON.
- Unknown properties → `400 unknown_field` (mass-assignment defence).
- Server-owned fields (`TenantId`, `Status` where the server drives it, prices
  on receiving, `PostedAtUtc`, audit fields, `RowVersion` as a body value) are
  rejected.
- `DELETE` may carry a body only for a documented reason (soft delete + reason).
- Maximum body 1 MB (except uploads).

---

## 6. Response conventions

### 6.1 Success

- A single resource returns the resource object directly (no wrapper).
- `201 Created` with a `Location` header for creation.
- `204 No Content` for deletes and command endpoints with no payload.
- `200 OK` with a payload for commands that return a result (e.g. posting a
  document returns the posted document + new balances).

### 6.2 Errors — RFC 9457 Problem Details

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://stockly.dev/problems/insufficient-stock",
  "title": "Insufficient stock",
  "status": 409,
  "detail": "Product 'RICE-25KG' has 3 available but 5 were requested.",
  "instance": "/api/v1/inventory/stock-out",
  "traceId": "0HN7…",
  "code": "insufficient_stock",
  "errors": {
    "lines[0].quantity": ["insufficient_stock"]
  }
}
```

Rules:

- `detail` is a **safe** message: no SQL, no stack trace, no internal type names,
  no other tenant's data, no connection details.
- `code` is a stable machine string from `BUSINESS_RULES.md` §14.
- `errors` is field-path keyed; values are codes, not sentences.
- `traceId` always present so a user can quote it in a support request.
- Validation details reveal the field path but never the whole model.

### 6.3 Status codes

| Code | When |
|---|---|
| `200` | Successful read or command with a result |
| `201` | Resource created |
| `204` | Successful delete or void command |
| `400` | Malformed, unknown field, failed validation, bad parameter |
| `401` | Missing/invalid/expired token, failed credentials |
| `403` | Authenticated but not permitted (known resource) or entitlement blocked |
| `404` | Not found **or** outside the caller's tenant/warehouse scope |
| `405` | Method not allowed on an existing path |
| `409` | Business rule violation, concurrency, duplicate, insufficient stock |
| `412` | `If-Match` / `rowversion` mismatch |
| `413` | Payload too large |
| `415` | Unsupported content type / file type |
| `422` | Reserved (unused in V1 to keep the contract small) |
| `428` | `If-Match` missing on an endpoint that requires it (§8) |
| `429` | Rate limited |
| `500` | Unexpected failure — safe message + `traceId` |
| `503` | Dependency unavailable (DB) |

**Never** return `500` with an exception message. Never return `200` with an
error embedded in the body.

---

## 7. Idempotency

```http
POST /api/v1/inventory/stock-out
Idempotency-Key: 5f3c1b7e-9d2a-4f6b-8c1d-2e3f4a5b6c7d
```

| Situation | Behaviour |
|---|---|
| First request | Processed; result stored with the key |
| Repeat, same key, same payload | Original result returned, with `Idempotent-Replay: true` |
| Repeat, same key, **different** payload | `409 idempotency_conflict` |
| Concurrent repeat | The unique index decides the winner; the loser re-reads and returns the winner's result |

Applied to: stock in/out/return/transfer/adjust/waste posting, goods receipts,
stock-count submit/post, purchase order/approval actions.

---

## 8. Concurrency

```http
GET  /api/v1/inventory/documents/{id}
→ 200, ETag: "AQAAAAIAAY="

PATCH /api/v1/inventory/documents/{id}
If-Match: "AQAAAAIAAY="
→ 412 Precondition Failed when the rowversion changed
```

- `ETag` is the base64 of the `rowversion`.
- `If-Match` is **required** for updates and deletes of documents, master data,
  and recipes.
- Missing `If-Match` on those endpoints ⇒ `428 Precondition Required`.
- Inventory posting does not need `If-Match` — the guarded `UPDATE` is the
  concurrency control, and `Idempotency-Key` handles retries.

---

## 9. Localization

- Request: `Accept-Language: ar` or `?culture=ar` (culture wins if both present
  and valid).
- Response: master-data objects expose `name` and `nameAr`; the frontend picks.
  The server does not translate fields silently.
- Enums are returned as stable lowercase codes: `"stock_out"`, `"pending_approval"`.
- Numbers are raw decimals + ISO currency code; formatting is client-side.
- Dates are UTC ISO-8601 (`2026-09-27T10:15:30Z`).

---

## 10. Rate limits

| Scope | Limit (provisional — §23 TBD-04, config-driven) |
|---|---|
| Global per IP | 300 req/min |
| Auth per IP | 10 req/min |
| Auth per identity (HMAC key) | 5 failures / 15 min before progressive delay |
| PIN per device | 5 failures / 5 min |
| PIN per membership | 5 failures / 15 min |
| PIN per enrollment code | 5 failures / 5 min — protects the code itself, not just the resolved device |
| Terminal identities per IP and per code | 30 req/min (the endpoint leaks a staffing list; see SEC-KNW-08) |
| Reports per user | 30 req/min |
| Exports per tenant | 10 jobs/hour |
| Body | 1 MB |

Responses include `Retry-After` (seconds) and:
`X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`.

**Throttling decisions are server-side and audited for auth endpoints.**

---

## 11. Pagination, bulk and long operations

- Max page size 200.
- Bulk operations are limited to 500 items and are all-or-nothing.
- Anything over the synchronous limit becomes a job: `202 Accepted` with a job
  id, polled via `GET /api/v1/jobs/{id}` and delivered as an attachment.
- Client cancellation must not leave partial data (transaction scope).

---

## 12. OpenAPI and documentation

- Generated from code with XML comments; `/api-docs/openapi.json` always
  available and free of secrets, connection strings, and internal host names.
- Swagger UI in Development/Staging only; disabled in Production.
- Every operation documents: required permission, warehouse scope, idempotency
  requirement, `If-Match` requirement, rate limit, and known error codes.
- The OpenAPI document is a security surface: descriptions must not leak
  internal schema details.

---

## 13. Open API questions

None of these block implementation: each one has a recorded V1 disposition. A
disposition may change only through a new ADR.

| ID | Question | V1 disposition |
|---|---|---|
| API-TBD-01 | Cursor vs page/offset for small administrative lists | **Keyset (cursor) pagination for every list that can grow without bound** (ledger, documents, events, users) because `OFFSET` degrades on exactly the tables the report pages read. Small, bounded administrative lists (roles, permissions, reason codes, units) may use page/offset. Sorting is restricted to an allowlist of indexed columns |
| API-TBD-02 | Whether `POST /auth/select-tenant` is a separate credential-bearing step or part of login | **Separate step that re-presents credentials.** Login returns no token when the user has >1 active membership; the client calls `select-tenant` with the email/username + password + membership id. A partial token would leak the ambiguity and could be replayed |
| API-TBD-03 | Whether OIDC/OAuth enters the API surface in V1 or V2 | **V2.** §23 TBD-03: local passwords + PIN only. `POST /auth/external/{provider}` is not in V1; the identity abstraction is the seam |
| API-TBD-04 | Exact rate limit numbers | **Owner decision (§23 TBD-04).** The table in §10 is the provisional value set; all numbers are config-driven (`RateLimit__*`), so a change is configuration, not a contract change |
| API-TBD-05 | Generic `filters` JSON object vs explicit typed parameters | **Explicit typed parameters, plus a `search` term where a full-text index backs it.** A generic JSON filter bag is a mass-assignment and query-shape risk and cannot be indexed or documented per field |
| API-TBD-06 | Whether `If-Match` is required for every update or only for posted documents | **Every `PUT`/`PATCH` that mutates a row carrying a concurrency token requires `If-Match`**, which is every mutable master-data row and every document header. Append-only tables and new-row `POST`s do not. A missing `If-Match` is `428 precondition_required` |
| API-TBD-07 | Localisation of generated PDF/Excel exports | **Owner decision (§23 TBD-15), provisional:** CSV is language-neutral (UTF-8 BOM, `;`); PDF/Excel Arabic-first with an English fallback |
