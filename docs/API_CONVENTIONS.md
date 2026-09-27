# STOCKLY — API CONVENTIONS

| Field | Value |
|---|---|
| Document | `API_CONVENTIONS.md` |
| Version | 0.1.0 |
| Status | DRAFT — Phase 0 |
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

| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `/terminal/devices/{deviceCode}/public-info` | anonymous | tenant display name only |
| GET | `/terminal/sessions/{deviceId}/identities` | device-scoped | names only — **not authentication** |
| POST | `/terminal/sessions/{deviceId}/pin-challenge` | anonymous | nonce, short TTL |
| POST | `/terminal/sessions/{deviceId}/pin-auth` | anonymous | PIN verification |
| POST | `/terminal/sessions/{deviceId}/switch-user` | bearer | revoke + re-authenticate |
| POST | `/terminal/devices/{deviceId}/heartbeat` | bearer | throttled last-seen |

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
GET    /api/v1/platform/tenants
POST   /api/v1/platform/tenants
GET    /api/v1/platform/plans
POST   /api/v1/platform/plans
PUT    /api/v1/platform/plans/{id}
POST   /api/v1/platform/tenants/{id}/subscription
PUT    /api/v1/platform/tenants/{id}/entitlements/{featureKey}
GET    /api/v1/platform/tenants/{id}/usage
GET    /api/v1/platform/audit
```

Platform routes **reject a tenant-scoped token** even if the caller holds a role
named similarly.

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
?includeDeleted=true      requires tenant.roles.manage
?sort=-postedAt,name      allowlisted field names only
```

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

| Scope | Limit (proposed — TBD-04) |
|---|---|
| Global per IP | 300 req/min |
| Auth per IP | 10 req/min |
| Auth per identity | 5 failures / 15 min before progressive delay |
| PIN per device | 5 failures / 5 min |
| PIN per membership | 5 failures / 15 min |
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

| ID | Question |
|---|---|
| API-TBD-01 | Cursor vs page/offset for small administrative lists |
| API-TBD-02 | Whether `POST /auth/select-tenant` should be a separate credential-bearing step or part of login (multi-membership users) |
| API-TBD-03 | Whether OIDC/OAuth support enters the API surface in V1 or V2 (TBD-03) |
| API-TBD-04 | Exact rate limit numbers (TBD-04) |
| API-TBD-05 | Whether report filters use a generic `filters` JSON object or explicit typed parameters |
| API-TBD-06 | Whether `If-Match` is required for every update or only for posted documents |
| API-TBD-07 | Localisation of generated PDF/Excel exports (TBD-15) |
