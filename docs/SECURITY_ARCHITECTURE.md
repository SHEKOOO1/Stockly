# STOCKLY — SECURITY ARCHITECTURE

| Field | Value |
|---|---|
| Document | `SECURITY_ARCHITECTURE.md` |
| Version | 0.1.0 |
| Status | DRAFT — Phase 0 |
| Last updated | 2026-09-27 |
| Related | `ROLES_PERMISSIONS.md`, `ARCHITECTURE.md`, `BUSINESS_RULES.md`, `TESTING_STRATEGY.md` |

> **Security is a first-class architectural requirement of Stockly, not a feature
> added later.** The backend is authoritative. The frontend is never trusted.

---

## 1. Security model summary

```text
   Untrusted input
          │
          ▼
  ┌───────────────────────┐
  │ Edge (Nginx)          │  TLS, HSTS, security headers, body-size limit, IP rate limit
  └───────────┬───────────┘
              ▼
  ┌───────────────────────┐
  │ API pipeline          │  correlation id → body size → auth → session →
  │                       │  membership → entitlement → rate limit → permission →
  │                       │  warehouse scope → resource scope → use case → audit
  └───────────┬───────────┘
              ▼
  ┌───────────────────────┐
  │ Data layer            │  tenant composite keys, unique indexes, CHECK constraints,
  │                       │  rowversion, atomic guarded UPDATE, no DDL for app principal
  └───────────────────────┘
```

Every request traverses all layers. A failure in the frontend, or in any single
layer, MUST NOT be sufficient to breach isolation.

---

## 2. Threat model (STRIDE-derived, Stockly-specific)

| Threat | Stockly scenario | Control |
|---|---|---|
| **Spoofing** | Attacker calls the API claiming to be a warehouse manager | JWT signature + issuer/audience validation, server-side session state check, device claim is not proof |
| **Spoofing** | Shared terminal: attacker impersonates a user by selecting their name | PIN is the authenticator; name selection grants nothing (§4.2) |
| **Spoofing** | Stolen refresh token replay | Rotation + family reuse detection → whole family revoked (ADR-0007) |
| **Spoofing** | Tenant impersonation via `X-Tenant-Id` header | Header is ignored; tenant comes from validated token/session (ADR-0004) |
| **Tampering** | Client sends `warehouseId` of an unassigned warehouse | Server verifies assignment; resource loaded with tenant+warehouse predicate |
| **Tampering** | Client edits price/approval/role fields in a body | Unknown fields rejected; such fields are server-owned; mass-assignment tests |
| **Tampering** | Client posts a free-form quantity to "set" stock | There is no set-quantity endpoint; only ledger-producing movement endpoints exist |
| **Repudiation** | User claims they did not issue goods | Append-only audit with user, device, session, correlation id, before/after |
| **Repudiation** | Admin changes a role's permissions | Audited with before/after diff, cannot be deleted by the admin |
| **Information disclosure** | IDOR probing product IDs across tenants | Tenant predicate in the same query; `404` for cross-tenant existence |
| **Information disclosure** | Error messages reveal SQL, stack traces, tenant names | Safe problem details + redaction + test suite |
| **Information disclosure** | User enumeration via login response differences | Uniform status/message/timing; generic credentials error |
| **Denial of service** | Brute-force PINs on a terminal | Per-device + per-membership + per-IP rate limits + progressive delay + lockout |
| **Denial of service** | Oversized request body / huge export | Body size limits, page size caps, query timeouts, export job queue |
| **Elevation of privilege** | User self-assigns `TenantAdmin` | Role assignment requires `tenant.roles.manage`/`tenant.users.manage`; target membership checked; audited |
| **Elevation of privilege** | Member of Tenant A uses Tenant B's warehouse | Tenant-scoped composite FKs + assignment check |
| **Elevation of privilege** | Revoked membership keeps working until token expiry | Session + membership state checked per request (ADR-0013) |
| **Tampering (race)** | Two concurrent stock-outs drive stock negative | Atomic guarded `UPDATE` + `CHECK` constraint (ADR-0010) |
| **Tampering (replay)** | Retried POST creates a duplicate receipt | `Idempotency-Key` + unique index (ADR-0014) |
| **Unauthorized file** | Malicious upload / path traversal | GUID storage keys, allowlist + magic-byte validation, authorized download endpoint, `nosniff` (ADR-0025) |
| **CSRF** | Cookie-authenticated state-changing request from a malicious site | Refresh cookie is `SameSite=Strict`; access token is memory-only and requires an explicit header; anti-CSRF token for any cookie-authenticated mutation (ADR-0030) |
| **XSS** | Stored product name renders script in terminal UI | Output encoding by the framework; strict CSP; no `dangerouslySetInnerHTML`; sanitization for any rich text; stored-XSS test |

---

## 3. Trust boundaries

| Boundary | Trust level |
|---|---|
| Browser / PWA / shared terminal | **Untrusted.** All input is hostile until validated. |
| Nginx | Trusted infrastructure, but it is not a security decision point for authorization. |
| ASP.NET Core process | Trusted application code — it *is* the security boundary. |
| SQL Server | Trusted for integrity; the app principal is least-privilege. |
| Object storage / file store | Untrusted content. Nothing is served without re-authorization. |
| Platform operator (human) | Trusted but audited; no self-service bypass. |

---

## 4. Authentication

### 4.1 Password authentication

| Control | Requirement |
|---|---|
| Storage | Argon2id (preferred) or BCrypt with a per-user salt. Parameters recorded in the hash. *(TBD-02)* |
| Comparison | Constant-time comparison library. |
| Enumeration | Identical response body, status code, and approximate timing whether the account exists, the password is wrong, or the account is locked. |
| Failure handling | Generic message: `invalid_credentials`. The reason is only in the audit log (secured category). |
| Lockout | Progressive delay, then temporary lock, then require administrative unlock. Per identity and per IP. *(TBD-04)* |
| Forced change | `MustChangePassword` flag forces a change before any other operation. |
| Reset | Single-use, short-lived, hashed token delivered out-of-band. Reset revokes all sessions of the user. |
| Password policy | Length/complexity/breach-list rules. *(TBD-01)* |
| Reuse | Current password required to change it. |

### 4.2 PIN authentication (shared terminals)

| Control | Requirement |
|---|---|
| Minimum | 6 digits, maximum 12, digits only. |
| Storage | Memory-hard KDF hash. Never plaintext, never returned, never logged. |
| Strength | Must not equal the password, tenant code, current year, or trivial sequences. |
| Comparison | Constant-time hash comparison. |
| Identity list endpoint | Returns `{ membershipId, displayName, avatarRef }` only. Never roles, permissions, PIN state, or account status. |
| Verification order | Device active → membership active → membership assigned to the device's warehouse → PIN credential present → PIN hash verify → throttling. |
| Uniform failure | Wrong PIN, unknown membership, locked PIN, and disabled account are indistinguishable to the client. |
| Throttling | Per device + per membership + per IP, combined (any one reaching the limit blocks). |
| Progressive delay | Delay increases with consecutive failures; then temporary lock; then admin unlock. |
| Rotation | `PinVersion` bump invalidates all sessions authenticated by that PIN. |
| Forced change | `MustChangePin` blocks stock permissions until the PIN is changed. |
| Self-registration | **Impossible.** No terminal endpoint creates users. |
| Session limits | One active session per device by default. *(TBD-11)* |

### 4.3 Token model

| Property | Access token | Refresh token |
|---|---|---|
| Type | JWT (asymmetric: RSA-256) | Opaque random 256-bit |
| Lifetime | 15 minutes | 7 days, rotating |
| Storage (server) | Not stored | **Hashed** in `RefreshToken` with family id, status, expiry |
| Storage (client) | Memory only | `HttpOnly; Secure; SameSite=Strict` cookie |
| Claims | `sub`, `mid`, `tid`, `sid`, `did`, `jti`, `iat`, `exp`, `iss`, `aud` | None (opaque) |
| Revocation | Per-request session check | Family revocation |

Rules:

- Signature algorithm is pinned; `alg: none` and algorithm confusion are rejected.
- `iss` and `aud` are validated on every request.
- Clock skew tolerance: 60 seconds.
- Refresh token reuse ⇒ revoke family + session + `auth.token.reuse_detected` audit event + security notification.
- Logout revokes the session and family.
- Tokens MUST NOT be placed in URLs, query strings, localStorage, or logs.

---

## 5. Authorization

### 5.1 The mandatory chain

```text
Authenticated User
      ↓
Tenant (from token/session, never from input)
      ↓
TenantMembership active
      ↓
Entitlement (subscription allows the operation)
      ↓
Permission (role bundle)
      ↓
Warehouse Assignment (membership ↔ warehouse)
      ↓
Resource scope (row belongs to tenant AND an authorized warehouse)
```

Skipping any step is a security defect, not a style issue.

### 5.2 Rules

- Role ≠ warehouse scope. `stock.out` permission in the kitchen does not grant
  the cleaning warehouse.
- Resource checks happen **in the query** that loads the resource
  (`WHERE Id = @id AND TenantId = @tid AND WarehouseId IN (...)`).
  Load-then-check is prohibited for tenant/warehouse-scoped data.
- `404` instead of `403` when the caller must not learn the resource's existence.
- Bulk endpoints (e.g. bulk post, bulk export) authorize **every** item; a
  single unauthorized item rejects the whole request (no partial success that
  could be used as an oracle).
- Server-owned fields in request bodies are ignored/stripped, and unknown
  properties cause `400`. This defeats mass assignment.
- `platform.*` routes require a platform-scoped token; a tenant token is rejected.

### 5.3 Privilege escalation defences

| Vector | Defence |
|---|---|
| Self-role-assignment | Only `tenant.roles.manage` may grant roles; granting a role you do not hold requires a higher authority level; every grant audited with before/after |
| Last-admin protection | The system prevents removing the last active `TenantOwner` |
| Role self-escalation | System roles' permission sets are immutable in V1; custom roles cannot exceed the creator's effective permissions |
| Permission tampering | Permissions are seeded and read-only; only role↔permission mappings are editable, and only by `tenant.roles.manage` |
| Membership self-reactivation | A suspended membership cannot be reactivated by itself; reactivation is an admin action and is audited |
| Warehouse auto-assignment | Assigning a warehouse requires a separate, explicit permission from granting roles |

---

## 6. Rate limiting and abuse prevention

Layered limits (all configurable, all enforced server-side):

| Layer | Scope | Purpose |
|---|---|---|
| Edge (Nginx) | Per IP | Volumetric flood protection |
| Global | Per IP | Baseline protection for all endpoints |
| Endpoint | Per IP per route group | Protect expensive endpoints (reports, exports, costing) |
| Auth | Per IP **and** per identity | Password and PIN brute force |
| Device | Per device | Terminal PIN brute force across users |
| Membership | Per membership | PIN brute force across devices |
| Business | Per tenant | Export/report job storms |

Responses include `Retry-After` and `X-RateLimit-*` headers. Limits are applied
**before** expensive work, and throttling decisions are audited for the auth
endpoints.

Bypass resistance:

- Keyed on the *verified* identity where available, and on IP otherwise — not on
  a client-supplied header. `X-Forwarded-For` is trusted **only** from configured
  proxy hops.
- Distributed counters: in-memory per instance is acceptable for V1 single
  instance; the interface is designed so a Redis/DB-backed limiter can replace
  it without touching callers.
- Locks are not bypassable by changing the request body, casing, or parameter
  order.

---

## 7. Cryptography and secrets

| Item | Approach |
|---|---|
| Password/PIN hashing | Argon2id (preferred) or BCrypt, per-user salt. *(TBD-02)* |
| Token signing | RSA-256 (asymmetric) so verification can be delegated without sharing the signing key. |
| Refresh tokens | 256-bit CSPRNG opaque; SHA-256 stored (lookup key) + the token is never recoverable |
| Reset tokens | 256-bit opaque, hashed at rest, single-use, short TTL |
| Idempotency keys | Client-supplied opaque string, stored hashed, scoped to endpoint+actor |
| At rest | TLS to SQL Server; transparent data encryption (TDE) in production is a deployment decision, not code |
| In transit | TLS 1.2+ everywhere, HSTS, no plaintext internal traffic in production |
| Secrets delivery | Environment/Docker secrets from a secret store. **Never** in the repository, images, logs, or `appsettings.json` committed to Git. |

Secret handling rules:

- `appsettings.json` contains **no** secrets, only non-sensitive defaults.
- Environment-specific secrets come from environment variables or mounted
  secrets; development uses `.env` (git-ignored) with `.env.example` as the
  committed template.
- A pre-commit secret scan runs in CI.
- Signing keys are rotated with a documented procedure that supports key
  overlap so existing tokens remain verifiable.
- `IDesensitizedString`/redaction helpers prevent secrets reaching logs; a test
  asserts redaction for known patterns.

---

## 8. Input validation and output encoding

| Control | Requirement |
|---|---|
| Model validation | FluentValidation on every request DTO; validation runs before any business logic or DB access |
| Unknown properties | Rejected (`400`) — prevents mass assignment and contract drift |
| Length/format limits | Enforced per field; strings bounded; numbers bounded to `decimal(18,4)` with ≤4 decimals; no scientific notation |
| Enums | Unknown values rejected, never coerced to a default |
| SQL injection | EF Core parameterisation everywhere; raw SQL always parameterised; identifiers from an allowlist, never concatenated user input. Injection tests included. |
| XSS | Output encoding by the framework, strict CSP (`default-src 'self'`, no `unsafe-inline` except where justified and reviewed), no `innerHTML` for user data, sanitisation for any rich text, `X-Content-Type-Options: nosniff` |
| CSRF | Refresh cookie `SameSite=Strict`; access token memory-only; explicit anti-CSRF token on any cookie-authenticated mutation; CORS restricted to configured origins, never `*` with credentials |
| Open redirect | Redirect targets validated against an allowlist |
| Deserialization | No `TypeNameHandling` other than `None`; no polymorphic deserialization of client payloads |
| Payload size | Global body limit; per-endpoint overrides; `MultipartReader` limits for uploads |
| Header injection | Header values echoed back (e.g. correlation id) are sanitised to a safe character set |
| Path traversal | Storage keys are server-generated GUIDs; any file name from the client is metadata only and sanitised for display |

---

## 9. Inventory integrity and concurrency

Detailed rules live in `PROJECT_BLUEPRINT.md` §11 and `BUSINESS_RULES.md` §5.
Security-relevant summary:

1. There is **no** endpoint that sets a stock quantity directly. All changes
   originate from documents that produce immutable ledger transactions.
2. Balance mutation is a single atomic guarded `UPDATE` inside an explicit
   transaction. A guarded predicate is part of the SQL, not a C# `if`.
3. `CHECK (OnHandQuantity >= 0)` is enforced by the database as a final barrier.
4. Documents post all-or-nothing.
5. `Idempotency-Key` prevents duplicate posting on retry.
6. Concurrency tokens (`rowversion` → ETag → `If-Match`) prevent lost updates on
   master data and document headers.
7. Reversal is the only way to undo a posted document; it creates compensating
   transactions and never deletes history.
8. A stock-out request that would exceed available stock is rejected with a
   business code; the client is never allowed to "force" a negative balance.

---

## 10. Audit and non-repudiation

See `PROJECT_BLUEPRINT.md` §16 for the field list. Security-specific rules:

- Audit is written **inside the business transaction** so a change and its
  audit trail commit or roll back together.
- Security-category events are always written synchronously, including on
  failure and on denial.
- The application database principal has **no** `DELETE` and **no** `UPDATE`
  grant on `audit.AuditLog`. It may only `INSERT` and `SELECT` (with `SELECT`
  granted only to the security-audit reporting path).
- Ordinary tenant roles cannot read raw audit JSON. Exports are produced by the
  backend with redaction, and the raw before/after payload is admin-only.
- The audit table is partitioned monthly; retention is TBD-14.
- Redaction is applied to every before/after payload: passwords, PINs, tokens,
  keys, connection strings, and any field whose name matches a secret pattern.
  A dedicated test asserts a synthetic document cannot leak a secret.

---

## 11. Session and device security

| Control | Requirement |
|---|---|
| Idle timeout | Server-enforced; the terminal UI timeout is a convenience only |
| Absolute timeout | Maximum session lifetime regardless of activity |
| Auto-lock | UI lock plus server-side invalidation |
| Switch user | Current session revoked before the new one is created |
| Device revocation | Revokes all sessions on the device immediately |
| Membership suspension | Invalidates all memberships' sessions immediately |
| Role change | Permission cache invalidated immediately |
| Last-seen | Throttled write (≥60s) to avoid a DB write per request |
| Device claim | The client asserts `deviceId`; the server validates that the device belongs to the tenant and warehouse. **Known limitation:** physical device possession is not cryptographically proven in V1 — recorded as a known security issue. |
| Terminal hardening | Documented: kiosk/fullscreen, no browser devtools, no shared OS accounts, autoplay disabled, screen lock. Documented as an operational requirement, not a code control. |

---

## 12. Security headers (edge + app)

| Header | Value |
|---|---|
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains; preload` |
| `Content-Security-Policy` | `default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data: blob:; connect-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'; form-action 'self'` |
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `no-referrer` |
| `Permissions-Policy` | restrict camera to the scanning origin, deny microphone/geolocation |
| `Cross-Origin-Opener-Policy` | `same-origin` |
| `Cross-Origin-Resource-Policy` | `same-origin` |
| `Cache-Control` (API) | `no-store` on all authenticated responses |

---

## 13. Secrets and PII handling in code

Never committed, logged, seeded, or returned: passwords, PINs, PIN hashes, JWTs,
refresh tokens, reset tokens, encryption/signing keys, connection strings,
certificates, PII beyond operational need.

Personal data minimisation: stock operations need to know *who*, *where*, and
*when* — not a user's address, national ID, or medical data. User profile fields
are limited to name, business email, and optional phone.

---

## 14. Security test obligations

Every item below MUST have automated coverage before the owning module is
considered complete. Full plan in `TESTING_STRATEGY.md`.

Tenant isolation · warehouse isolation · IDOR/BOLA · privilege escalation ·
role manipulation · permission manipulation · mass assignment · unauthorized
object access · disabled user · suspended membership · revoked session ·
expired token · token replay · refresh token reuse · PIN brute force ·
password brute force · rate-limit bypass · account enumeration · SQL injection ·
XSS · CSRF · unsafe file upload · path traversal · malformed input ·
oversized payload · concurrency/race · duplicate requests · replayed
transactions · insecure direct object references · subscription-limit bypass ·
audit tampering.

**A security regression is a build failure.**

---

## 15. Known security limitations (Phase 0 register)

These are recorded honestly. None of them may be removed from the register
without a decision, a test, and a status update.

| ID | Limitation | Severity | Mitigation | Plan |
|---|---|---|---|---|
| SEC-KNW-01 | Device identity is a client claim; physical device possession is not cryptographically proven | Medium | PIN + per-device rate limits + short sessions + device revocation | V2 device attestation / per-device client certificate |
| SEC-KNW-02 | Shared terminals are usable by anyone with physical access to the name list | Medium (inherent to the PIN model) | PIN secrecy, per-device lockout, forced PIN change, audit of every action | Document operational controls; V2 optional device biometrics |
| SEC-KNW-03 | Access token cannot be revoked before its 15-minute expiry; mitigated by per-request session state checks | Low | Session state check on every authorized request | Shorter access tokens (5 min) if friction is acceptable |
| SEC-KNW-04 | No MFA in V1 | Medium | Strong password policy, lockout, rate limits, audit | V2 TOTP/WebAuthn |
| SEC-KNW-05 | Rate limiting is per-instance in V1 (single instance deployment) | Low | Deployment pinned to one instance; limiter interface designed for a shared store | Move to a shared store before horizontal scaling |
| SEC-KNW-06 | Antivirus scanning of attachments not implemented in V1 | Low | Allowlist + magic-byte validation + no inline serving | V1.1 ClamAV hook |
| SEC-KNW-07 | No WAF/DDoS service in front of Nginx in V1 | Low | Edge rate limits + provider-level protection | Production infrastructure phase |

---

## 16. Security review checklist (per phase)

```text
[ ] Authentication paths reviewed (password, PIN, refresh, reset)
[ ] Authorization checked per endpoint, including new ones
[ ] Tenant predicate present in every new query
[ ] Warehouse assignment checked on every new warehouse-scoped path
[ ] No new raw SQL without parameterisation
[ ] No new endpoint returns entity data without ownership filter
[ ] No secrets introduced (config, logs, tests, fixtures)
[ ] No new JSON blob used where a relation is appropriate
[ ] Rate limits appropriate for the new endpoint's cost
[ ] Audit events added for the new sensitive actions
[ ] Security tests added alongside the feature (not afterwards)
[ ] Full security suite still green
[ ] Known issues register updated
```
