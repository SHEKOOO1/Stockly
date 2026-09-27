# STOCKLY — DATABASE DESIGN

| Field | Value |
|---|---|
| Document | `DATABASE_DESIGN.md` |
| Version | 1.0.0 |
| Status | FINAL DRAFT — consistent with `PROJECT_BLUEPRINT.md` v1.0; no open schema question blocks Phase 1 |
| Last updated | 2026-09-27 |
| Engine | SQL Server 2022 |
| ORM | Entity Framework Core |
| Entities | 66 (V1) — see §4 |
| Related | `PROJECT_BLUEPRINT.md`, `BUSINESS_RULES.md`, `ARCHITECTURE.md` |

---

## 1. Modelling principles

1. **Relational by default.** JSON is used only where the shape is unknown at
   design time (custom field values, dropdown options) or for immutable read-only
   point-in-time snapshots (`RecipeCostSnapshot.LineCosts`,
   `EventFoodCostSnapshot.Breakdown`, `ops.OutboxMessage.Payload`,
   redacted `AuditLog.BeforeJson/AfterJson`). Never as a shortcut for a normal
   relation. V1 has exactly three kinds of JSON column, all justified above.
2. **Tenant isolation is structural.** Tenant-scoped tables carry a non-null
   `TenantId`; every unique index leads with `TenantId` (ADR-0032); cross-tenant
   references are impossible because foreign keys are composite on
   `(Id, TenantId)`.
3. **The ledger is the truth.** `StockBalance` and `InventoryLot` are
   transactionally maintained projections. `StockTransaction` is immutable.
   The rebuild is **partial** (`PROJECT_BLUEPRINT.md` §11.1).
4. **No premature denormalisation.** Derived values that are cheap to compute
   are not stored, with two deliberate exceptions: stock balances (needed for
   performance and guarded updates) and cost snapshots (needed because
   historical cost must not change). `StockDocument.TotalQuantity/TotalCost` are
   a third, header-level denormalisation: a movement header's list view is read
   constantly and summing lines per row read would be O(n) per row. They are
   written **in the same transaction** as the lines and are covered by the
   posting tests.
5. **Money is `decimal(18,4)`** (ADR-0024). Quantities are `decimal(18,4)`.
   No `float` anywhere. The one documented exception is
   `master.UnitConversion.Factor decimal(18,8)`: a conversion factor is a ratio
   of two quantities, and rounding it to 4 places would accumulate error across
   chained conversions.
6. **All timestamps are UTC `datetime2(3)`.** Tenant timezone is applied at
   presentation (ADR-0029).
7. **Concurrency:** every mutable operational table has
   `RowVersion rowversion NOT NULL`. **Append-only tables deliberately have
   none** (ADR-0037) — a `rowversion` on a table that must never be updated is
   a contradiction and breaks the `INSERT`-only database principal.
8. **Soft delete** only for master/config data where history matters; ledger
   and audit tables are never deleted.
9. **Enum columns are `smallint`** and documented in the entity table as
   `1=…; 2=…`. There is no separate "lookup table" for an enum in V1; a
   `CHECK` constraint pins the legal values and the API maps them to C# enums.

---

## 2. Schema layout

```text
platform        Global identity + SaaS commercial data
identity        Tenants, memberships, roles, permissions, warehouses, devices, sessions, tokens
master          Categories, units, conversions, products, barcodes, reason codes
inventory       Batches, lots, balances, documents, lines, transactions, counts
purchasing      Suppliers, supplier products, price history, requests, orders, receipts
events          Events, requirements, consumption, attendance, shortage snapshots
food            Meal types, recipes, recipe items, cost snapshots
notifications   Notifications, preferences, push subscriptions
files           Attachments
extensibility   Custom field definitions and values
audit           Audit log (append-only)
ops             Outbox messages, document sequences
```

Table naming: `schema.SingularEntityName` (e.g. `inventory.StockBalance`).
FK naming: `FK_<ChildTable>_<ParentTable>`. Index naming:
`IX_<Table>_<Columns>` / `UX_<Table>_<Columns>`.

---

## 3. Column conventions

| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` | Client-generated (GUID) to avoid key hotspots and to support offline-created records |
| `TenantId` | `uniqueidentifier` | NOT NULL on all tenant-scoped tables; composite key material |
| `RowVersion` | `rowversion` | Concurrency token, exposed as ETag |
| `CreatedAtUtc`, `UpdatedAtUtc` | `datetime2(3)` | NOT NULL; set by the application, not the DB clock |
| `CreatedByMembershipId` | `uniqueidentifier` | Nullable for system-created rows |
| `IsDeleted` | `bit` | NOT NULL default 0; filtered unique indexes use `WHERE IsDeleted = 0` |
| `DeletedAtUtc`, `DeletedByMembershipId` | `datetime2(3)`, `uniqueidentifier` | Set together with `IsDeleted` |
| Enums | `smallint` | Persisted as `smallint` with a `CHECK` pinning the legal values and mapped to a C# enum by the API — never a raw `nvarchar`, never `int` |
| Booleans | `bit` | NOT NULL with a default |
| Names | `nvarchar(200)` | `Name` canonical + `nvarchar(200) NULL` `NameAr` for translatable master data |
| Codes | `nvarchar(50)` | Uppercase-normalised, unique per tenant |
| Notes/text | `nvarchar(1000)` / `nvarchar(max)` | `max` only where genuinely unbounded |
| Money | `decimal(18,4)` | Never `float` |
| Quantity | `decimal(18,4)` | Max 4 decimal places, positive, no negative stored in quantity columns |
| Quantity (signed) | `decimal(18,4)` | Only where a direction is genuinely ambiguous — prefer a direction column |

GUID primary keys require a `NEWSEQUENTIALID()`-ordered default only when the
application cannot supply one (audit log, outbox).

---

## 4. Entity catalogue — column-level design

Legend: PK primary key, FK foreign key, UQ unique, IX index, NN not null,
`rowv` = `rowversion` concurrency token, `sd` = soft delete.

### 4.1 `platform`

#### platform.AppUser (global identity)
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| Email | nvarchar(320) | NN, normalised lowercase, **UQ** |
| UserName | nvarchar(100) | NULL, normalised lowercase, **UQ** (filtered `IS NOT NULL`) |
| DisplayName | nvarchar(200) | NN |
| PhoneNumber | nvarchar(30) | NULL, E.164 |
| PasswordHash | nvarchar(400) | NULL — NULL means no password credential (e.g. PIN-only user) |
| PasswordSetAtUtc | datetime2(3) | NULL |
| MustChangePassword | bit | NN default 0 |
| IsPlatformAdmin | bit | NN default 0 — in V1 this single flag grants the whole `platform.*` permission set to the holder (ADR-0034). It is **not** a tenant grant: it never appears in a tenant token |
| IsActive | bit | NN default 1 |
| FailedLoginCount | int | NN default 0 |
| LockoutEndAtUtc | datetime2(3) | NULL |
| LastLoginAtUtc | datetime2(3) | NULL |
| TwoFactorEnabled | bit | NN default 0 (V2) |
| RowVersion | rowversion | rowv |

Indexes: `UX_AppUser_Email` (unique), `UX_AppUser_UserName` (filtered unique),
`IX_AppUser_LockoutEndAtUtc`.

**No `TenantId`** — deliberate (ADR-0003). A person has one identity; access to
an organization is granted by `TenantMembership`.

#### platform.Plan
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| Code | nvarchar(50) | NN **UQ** |
| Name | nvarchar(200) | NN |
| NameAr | nvarchar(200) | NULL |
| Description | nvarchar(1000) | NULL |
| MonthlyPrice | decimal(18,4) | NN |
| CurrencyCode | char(3) | NN ISO-4217 |
| BillingPeriodDays | int | NN |
| IsActive | bit | NN |
| SortOrder | int | NN |
| RowVersion | rowversion | rowv |

#### platform.PlanFeature
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| PlanId | uniqueidentifier | FK → Plan, NN |
| FeatureKey | nvarchar(100) | NN, e.g. `inventory.batch_tracking`, `custom_fields` |
| LimitType | smallint | NN: 0=None, 1=Boolean, 2=Count, 3=Users, 4=Warehouses, 5=StorageGb |
| LimitValue | decimal(18,4) | NULL (NULL = unlimited when LimitType≠None) |
| IsEnabled | bit | NN |
| RowVersion | rowversion | rowv |

UQ: `(PlanId, FeatureKey)`.

#### platform.Subscription
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK → identity.Tenant, NN |
| PlanId | uniqueidentifier | FK → Plan, NN |
| Status | smallint | NN: 1=Trialing, 2=Active, 3=PastDue, 4=Cancelled, 5=Expired |
| StartAtUtc | datetime2(3) | NN |
| EndAtUtc | datetime2(3) | NULL (NULL = open-ended) |
| GracePeriodDays | int | NN default 7 (§23 TBD-12 — provisional, per-subscription) |
| CancelledAtUtc | datetime2(3) | NULL |
| ExternalReference | nvarchar(200) | NULL (reserved for a future billing provider — §23 TBD-13) |
| RowVersion | rowversion | rowv |

Indexes: `IX_Subscription_Tenant_Status`, `IX_Subscription_EndAtUtc` (for the
expiry evaluator job).

#### platform.TenantEntitlement (per-tenant override)
`Id`, `TenantId` (FK, NN), `FeatureKey` (NN), `LimitType` (NN), `LimitValue` (NULL),
`IsEnabled` (NN), `Reason` (nvarchar(500) NULL), `GrantedByMembershipId` (FK NULL),
`ExpiresAtUtc` (NULL), audit columns, `RowVersion`.
UQ: `(TenantId, FeatureKey)`.

#### platform.SubscriptionUsage
`Id`, `TenantId` (FK NN), `PeriodStartUtc`, `PeriodEndUtc`, `FeatureKey` (NN),
`CurrentValue` decimal(18,4) NN, `LimitValue` decimal(18,4) NULL (snapshot of the
limit at period start), `RowVersion`.
UQ: `(TenantId, PeriodStartUtc, FeatureKey)`.
Updated in the same transaction as resource creation (ADR: over-provisioning).

---

### 4.2 `identity`

#### identity.Tenant
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| Code | nvarchar(50) | NN **UQ** (global unique — used in URLs) |
| Name | nvarchar(200) | NN |
| NameAr | nvarchar(200) | NULL |
| LegalName | nvarchar(300) | NULL |
| Status | smallint | NN: 1=Active, 2=Suspended, 3=Closed |
| TimeZoneId | nvarchar(100) | NN default 'UTC' (Windows tz id) |
| DefaultCulture | nvarchar(10) | NN default 'ar' |
| DateFormat | nvarchar(30) | NN default 'yyyy-MM-dd' |
| IsDeleted | bit | NN sd |
| RowVersion | rowversion | rowv |

#### identity.TenantSetting
`Id`, `TenantId` (FK NN **UQ**), `CurrencyCode char(3) NN`, `NumberOfDecimalPlaces int NN`
(0–4), `DefaultWarehouseId` (FK NULL), `LowStockPolicy` (nvarchar(30) NN),
`ExpiryWarningDays int NN`, `DefaultSessionIdleMinutes int NN`,
`DefaultSessionAbsoluteHours int NN`, `PinLength int NN` (≥6),
`RequireReasonCodeForAdjustments bit NN`, `RequireReasonCodeForWaste bit NN`,
`AllowNegativeStock bit NN default 0` (**always 0** — a kill switch, not a feature),
`RowVersion`.

> `AllowNegativeStock` exists solely so a platform operator can prove it is never
> enabled. A test asserts it is `0` in all environments.

#### identity.TenantMembership
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK → Tenant, NN |
| UserId | uniqueidentifier | FK → platform.AppUser, NN |
| Status | smallint | NN: 1=Active, 2=Suspended, 3=Disabled |
| IsOwner | bit | NN default 0 (marks TenantOwner transfer) |
| EmployeeNumber | nvarchar(50) | NULL |
| JobTitle | nvarchar(100) | NULL |
| PreferredCulture | nvarchar(10) | NULL |
| JoinedAtUtc | datetime2(3) | NN |
| RowVersion | rowversion | rowv |

UQ (filtered, active only): `(TenantId, UserId) WHERE Status <> 3` — a
suspended membership is preserved for audit and can be reactivated, but two
active memberships for the same user in the same tenant are impossible.
`IX_TenantMembership_User` for cross-tenant user lookup.

#### identity.Permission (global catalogue, seeded)
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| Code | nvarchar(100) | NN **UQ**, dot-notation: `inventory.stock.out` |
| Module | nvarchar(50) | NN |
| Name | nvarchar(200) | NN (English label for admin UI) |
| NameAr | nvarchar(200) | NULL |
| Description | nvarchar(1000) | NULL |
| IsDangerous | bit | NN default 0 (granting this is high-impact; UI warns) |
| RowVersion | rowversion | rowv |

Read-only at runtime. Seeded by migration. `IsDangerous = 1` for
`platform.*`, `tenant.roles.manage`, `security.*`, `tenant.audit.*`.

#### identity.Role
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK → Tenant, NN |
| Code | nvarchar(50) | NN (e.g. `store_keeper`) |
| Name | nvarchar(200) | NN |
| NameAr | nvarchar(200) | NULL |
| Description | nvarchar(1000) | NULL |
| RoleType | smallint | NN: 1=System, 2=Custom |
| AuthorityLevel | smallint | NN 1–100 (guards privilege escalation — see §6) |
| IsActive | bit | NN |
| IsDeleted | bit | NN sd |
| RowVersion | rowversion | rowv |

UQ (filtered): `(TenantId, Code) WHERE IsDeleted = 0`.
System role permission sets are **immutable** in V1 (ADR-0012 note).

#### identity.RolePermission
`Id`, `TenantId` (FK NN), `RoleId` (FK composite → Role NN), `PermissionId` (FK NN),
`CreatedAtUtc`.
UQ: `(RoleId, PermissionId)`.
Index: `(TenantId, PermissionId)` for reverse lookup.

#### identity.MembershipRole
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| MembershipId | uniqueidentifier | FK composite → TenantMembership, NN |
| RoleId | uniqueidentifier | FK composite → Role, NN |
| ScopeType | smallint | NN: 1=AllAssignedWarehouses, 2=SpecificWarehouses |
| RowVersion | rowversion | rowv |

UQ: `(MembershipId, RoleId)`.

#### identity.MembershipRoleWarehouse (scope junction)
`Id`, `TenantId` (FK NN), `MembershipRoleId` (FK composite NN),
`WarehouseId` (FK composite NN), `RowVersion`.
UQ: `(MembershipRoleId, WarehouseId)`.

> This is the join that makes "role in warehouse X" expressible. It is the
> formal model of the Stockly rule *"role and warehouse scope are separate"*.

#### identity.Warehouse
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| Code | nvarchar(50) | NN |
| Name | nvarchar(200) | NN |
| NameAr | nvarchar(200) | NULL |
| Type | smallint | NN: 1=Main, 2=Storage, 3=Kitchen, 4=Cold, 5=Vehicle, 6=Quarantine |
| Address | nvarchar(500) | NULL |
| IsDefaultReceipt | bit | NN default 0 |
| IsActive | bit | NN |
| IsDeleted | bit | NN sd |
| RowVersion | rowversion | rowv |

UQ (filtered): `(TenantId, Code) WHERE IsDeleted = 0`.

#### identity.MembershipWarehouse — the "WHERE" axis
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| MembershipId | uniqueidentifier | FK composite NN |
| WarehouseId | uniqueidentifier | FK composite NN |
| CanReceive | bit | NN default 1 |
| CanIssue | bit | NN default 1 |
| IsDefault | bit | NN default 0 (default warehouse for this user) |
| AssignedAtUtc | datetime2(3) | NN |
| RowVersion | rowversion | rowv |

UQ: `(TenantId, MembershipId, WarehouseId)`. Index: `(TenantId, WarehouseId)` for
reverse "who is assigned here" queries.

#### identity.Device
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| WarehouseId | uniqueidentifier | FK composite NN — a device belongs to **exactly one** warehouse |
| DeviceCode | nvarchar(50) | NN (e.g. `KITCHEN-01`) — human label, **not** a secret |
| EnrollmentCode | varchar(64) | NN — 128-bit random, base32url, rotatable, **UQ** (ADR-0035) |
| Name | nvarchar(200) | NN |
| Type | smallint | NN: 1=Kiosk, 2=Tablet, 3=Desktop, 4=Handheld |
| Status | smallint | NN: 1=Active, 2=Suspended, 3=Revoked |
| IsSharedTerminal | bit | NN default 1 |
| LastSeenAtUtc | datetime2(3) | NULL |
| RegisteredAtUtc | datetime2(3) | NN |
| RegisteredByMembershipId | uniqueidentifier | FK NULL |
| RevokedAtUtc | datetime2(3) | NULL |
| Notes | nvarchar(1000) | NULL |
| RowVersion | rowversion | rowv |

UQ (filtered): `(TenantId, DeviceCode)`. UQ: `UX_Device_EnrollmentCode` (global —
an enrollment code must resolve to exactly one device anywhere on the platform,
ADR-0032). Index: `(TenantId, WarehouseId, Status)`.

#### identity.AuthSession
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN **or NULL for a `Platform` session** (ADR-0034) |
| UserId | uniqueidentifier | FK → AppUser NN |
| MembershipId | uniqueidentifier | FK composite NULL — NULL for a `Platform` session |
| SessionScope | smallint | NN: 1=Tenant, 2=Platform. `CHECK ((SessionScope=2 AND TenantId IS NULL AND MembershipId IS NULL) OR (SessionScope=1 AND TenantId IS NOT NULL AND MembershipId IS NOT NULL))` — absolute invariant, so a `CHECK` is correct here |
| DeviceId | uniqueidentifier | FK NULL (NULL for password sessions on personal devices and for platform sessions) |
| Status | smallint | NN: 1=Active, 2=Expired, 3=Revoked, 4=Superseded |
| AuthenticationMethod | smallint | NN: 1=Password, 2=Pin |
| PinCredentialId | uniqueidentifier | FK NULL (for PIN sessions) |
| PinVersion | int | NULL — snapshot; a bump invalidates the session |
| RefreshFamilyId | uniqueidentifier | NN |
| IssuedAtUtc / ExpiresAtUtc / AbsoluteExpiresAtUtc | datetime2(3) | NN |
| LastActivityAtUtc | datetime2(3) | NN |
| LastSeenAtUtc | datetime2(3) | NULL (throttled write ≥60s) |
| RevokedAtUtc | datetime2(3) | NULL |
| RevokedReason | nvarchar(200) | NULL |
| RevokedByMembershipId | uniqueidentifier | FK NULL |
| IpAddress | nvarchar(45) | NULL (IPv6-safe) |
| UserAgent | nvarchar(400) | NULL |
| CorrelationId | nvarchar(100) | NULL |
| RowVersion | rowversion | rowv |

Indexes: `IX_AuthSession_Membership_Status`, `IX_AuthSession_Device_Status`,
`IX_AuthSession_ExpiresAtUtc` (sweeper), `IX_AuthSession_User_Status`,
`IX_AuthSession_Platform_Active (UserId, Status) WHERE SessionScope = 2` (partial
index for the platform-operator session list).

#### identity.RefreshToken
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| SessionId | uniqueidentifier | FK composite NN |
| FamilyId | uniqueidentifier | NN |
| TokenHash | binary(32) | NN — SHA-256 of the token, **UQ** |
| Status | smallint | NN: 1=Active, 2=Used, 3=Revoked, 4=ReplacedByReuse |
| IssuedAtUtc / ExpiresAtUtc | datetime2(3) | NN |
| UsedAtUtc | datetime2(3) | NULL |
| CreatedByIpAddress | nvarchar(45) | NULL |

UQ: `UX_RefreshToken_TokenHash`. Index: `(FamilyId, Status)`.
**The token value itself is never stored.**

#### identity.PinCredential
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| MembershipId | uniqueidentifier | FK composite NN |
| PinHash | nvarchar(400) | NN — KDF hash. Never returned by any API |
| Version | int | NN default 1 — bump invalidates PIN sessions |
| MustChangePin | bit | NN default 1 (true on admin reset) |
| FailedAttemptCount | int | NN default 0 |
| LockoutEndAtUtc | datetime2(3) | NULL |
| LastUsedAtUtc | datetime2(3) | NULL |
| SetAtUtc | datetime2(3) | NN |
| SetByMembershipId | uniqueidentifier | FK NULL |
| RowVersion | rowversion | rowv |

UQ: `(MembershipId)` — one PIN credential per membership, unfiltered. The
previous draft used a filtered index on a `Status` column that does not exist on
this table; the constraint is now exactly what the design means.
Index: `(TenantId, LockoutEndAtUtc)`.

#### identity.LoginAttempt (global throttle ledger)
| Column | Type | Notes |
|---|---|---|
| Id | bigint identity | PK (high volume) |
| OccurredAtUtc | datetime2(3) | NN |
| Method | smallint | NN: 1=Password, 2=Pin, 3=Refresh, 4=Reset |
| Outcome | smallint | NN: 1=Success, 2=Invalid, 3=Locked, 4=RateLimited, 5=UnknownAccount |
| IdentifierHash | binary(32) | NN — **HMAC-SHA256(identifier, server pepper)**, lowercase-trimmed and case-folded first (ADR-0036). Plain SHA-256 was rejected: an attacker with the table can dictionary-attack a known email domain, and the email list is not secret. The pepper lives in the secret store, never in the database |
| UserId | uniqueidentifier | FK NULL (NULL when the account does not exist) |
| DeviceId | uniqueidentifier | FK NULL |
| EnrollmentCodeHash | binary(32) | FK-equivalent NN for terminal attempts — HMAC of the enrollment code, so a leaked table cannot enumerate valid terminals |
| IpAddress | nvarchar(45) | NULL |
| IsSuccess | bit | NN |

Index: `(IdentifierHash, OccurredAtUtc)`, `(IpAddress, OccurredAtUtc)`,
`(EnrollmentCodeHash, OccurredAtUtc)`.
Partitioned by month on `OccurredAtUtc` when volume requires it.

#### identity.PasswordResetToken
`Id`, `TenantId` (FK NN), `UserId` (FK NN), `TokenHash` binary(32) NN **UQ**,
`ExpiresAtUtc` NN, `UsedAtUtc` NULL, `CreatedByIpAddress` NULL, `CreatedAtUtc` NN.
`TokenHash` is a SHA-256 of a 256-bit random token — safe without a pepper,
because the token is already high-entropy (a random value has no dictionary).
On use: revoke all sessions for the user, reset `MustChangePassword = 1`, audit.

---

### 4.3 `ops`

#### ops.DocumentSequence
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| DocumentType | smallint | NN (stock document type / PO / GR / count) |
| PeriodKey | nvarchar(20) | NN (e.g. `2026` for yearly numbering) |
| Prefix | nvarchar(20) | NN |
| NextValue | bigint | NN |
| Padding | smallint | NN default 6 |
| RowVersion | rowversion | rowv |

UQ: `(TenantId, DocumentType, PeriodKey)`.
Number allocation: `UPDATE ops.DocumentSequence SET NextValue = NextValue + 1
WHERE …` with the unique index as the concurrency guard. Never
`MAX(number)+1` in application code.

#### ops.OutboxMessage
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NULL |
| MessageType | nvarchar(100) | NN |
| Payload | nvarchar(max) | NN (JSON — justified: opaque integration payload) |
| Status | smallint | NN: 1=Pending, 2=Processing, 3=Processed, 4=Failed, 5=DeadLetter |
| Attempts | smallint | NN default 0 |
| NextAttemptAtUtc | datetime2(3) | NN |
| LockedUntilUtc | datetime2(3) | NULL |
| CreatedAtUtc / ProcessedAtUtc | datetime2(3) | NULL |
| LastError | nvarchar(2000) | NULL (sanitised — no stack traces to clients) |

Index: `(Status, NextAttemptAtUtc)` for the dispatcher.

---

### 4.4 `master`

#### master.Category
`Id`, `TenantId` (FK NN), `ParentCategoryId` (FK composite self-reference NULL),
`Code` NN, `Name` NN, `NameAr` NULL, `SortOrder` int NN, `IsActive` bit NN,
`IsDeleted` bit NN sd, audit columns, `RowVersion`.
UQ (filtered): `(TenantId, Code) WHERE IsDeleted = 0`.
UX: `(TenantId, ParentCategoryId, Code) WHERE IsDeleted = 0` — sibling codes unique.
Cycle prevention is a domain rule; a depth limit of 3 is enforced in code and
tested.

#### master.Unit
`Id`, `TenantId` (FK NN), `Code` NN, `Name` NN, `NameAr` NULL,
`UnitType` smallint NN (1=Count, 2=Weight, 3=Volume, 4=Length, 5=Time),
`DecimalPlaces` tinyint NN (0–4), `IsBaseUnit` bit NN, `IsActive` bit NN,
`IsDeleted` bit NN sd, `RowVersion`.
UQ (filtered): `(TenantId, Code) WHERE IsDeleted = 0`.

#### master.UnitConversion
`Id`, `TenantId` (FK NN), `ProductId` (FK composite NN) NULL — NULL = tenant-wide default,
`FromUnitId` (FK composite NN), `ToUnitId` (FK composite NN),
`Factor` decimal(18,8) NN (> 0), `RowVersion`.
UQ: `(TenantId, ProductId, FromUnitId, ToUnitId) WHERE ProductId IS NOT NULL` and
`UQ: (TenantId, FromUnitId, ToUnitId) WHERE ProductId IS NULL` — two filtered
indexes, because SQL Server treats NULLs as equal only inside an `IS NULL`
predicate (ADR-0041). Without the second index a tenant could hold two
tenant-wide defaults for the same unit pair.
Constraint: `FromUnitId <> ToUnitId`, `Factor > 0`.
Domain rule: the conversion graph must be acyclic; validated on write.

#### master.Product
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| Sku | nvarchar(50) | NN |
| Name | nvarchar(200) | NN |
| NameAr | nvarchar(200) | NULL |
| Description | nvarchar(1000) | NULL |
| CategoryId | uniqueidentifier | FK composite NULL |
| BaseUnitId | uniqueidentifier | FK composite NN |
| PurchaseUnitId | uniqueidentifier | FK composite NULL |
| IsBatchTracked | bit | NN default 0 |
| IsExpiryTracked | bit | NN default 0 |
| ShelfLifeDays | int | NULL — required when `IsExpiryTracked = 1` |
| DefaultReasonCodeId | uniqueidentifier | FK NULL |
| IsActive | bit | NN |
| IsDeleted | bit | NN sd |
| RowVersion | rowversion | rowv |

UQ (filtered): `(TenantId, Sku) WHERE IsDeleted = 0`.
IX: `(TenantId, CategoryId, IsActive)`, `(TenantId, Name)` (for search).
CHECK: `IsExpiryTracked = 0 OR ShelfLifeDays IS NOT NULL`.
`IsStockTracked` is **not** a flag in V1: all products are stock-tracked
(services/disposables without stock are a V2 consideration).
**There is no `Barcode` column on `Product` (ADR-0033).** The previous draft
carried `Product.Barcode` *and* `ProductBarcode`, so the same code could be
"the primary barcode" and also one of several barcodes, with two independent
unique indexes and no way to say which one wins. `master.ProductBarcode` is now
the **single source of truth** for every barcode; "primary" is the row with
`IsPrimary = 1`.

#### master.ProductBarcode
`Id`, `TenantId` (FK NN), `ProductId` (FK composite NN), `Barcode` nvarchar(64) NN,
`Symbology` smallint NN (1=EAN13, 2=EAN8, 3=UPC-A, 4=Code128, 5=QR, 6=DataMatrix
— the full set the enum models, §23 TBD-18/ADR-0043; an unknown symbology is
stored as `Code128` rather than rejected), `IsPrimary` bit NN, `RowVersion`.
UQ: `(TenantId, Barcode)` — unfiltered, because `Barcode` is `NOT NULL` here, so
a filtered variant would be pointless. UX (filtered): `(ProductId) WHERE
IsPrimary = 1`. Lookup by scan: `(TenantId, Barcode)` covering.

#### master.ProductWarehouseSetting
`Id`, `TenantId` (FK NN), `WarehouseId` (FK composite NN), `ProductId` (FK composite NN),
`MinimumQuantity` decimal(18,4) NULL, `MaximumQuantity` decimal(18,4) NULL,
`ReorderQuantity` decimal(18,4) NULL, `DefaultSupplierId` (FK NULL),
`ShelfLifeDaysOverride` int NULL, `IsPreferred` bit NN default 0, `RowVersion`.
UQ: `(TenantId, WarehouseId, ProductId)`.
CHECK: `MaximumQuantity IS NULL OR MinimumQuantity IS NULL OR MaximumQuantity >= MinimumQuantity`.

#### master.ReasonCode
`Id`, `TenantId` (FK NN), `MovementType` smallint NN, `Code` NN, `Name` NN,
`NameAr` NULL, `IsActive` bit NN, `IsDeleted` bit NN sd, `RowVersion`.
UQ (filtered): `(TenantId, MovementType, Code) WHERE IsDeleted = 0`.

---

### 4.5 `inventory` — the core

#### inventory.Batch (lot identity)
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| ProductId | uniqueidentifier | FK composite NN |
| BatchNumber | nvarchar(50) | NN |
| ExpiryDate | date | NULL — NULL only when the product is not expiry-tracked |
| ProductionDate | date | NULL |
| SupplierId | uniqueidentifier | FK NULL (source) |
| DefaultUnitCost | decimal(18,4) | NULL |
| ReceivedAtUtc | datetime2(3) | NN |
| IsQuarantined | bit | NN default 0 |
| RowVersion | rowversion | rowv |

UQ: `(TenantId, ProductId, BatchNumber, ExpiryDate)` **and** a second UQ
`(TenantId, ProductId, BatchNumber) WHERE ExpiryDate IS NULL`. SQL Server treats
NULLs as equal only inside an `IS NULL`-filtered index, so the two non-expiry
variants need two indexes (ADR-0041).
CHECK: `ExpiryDate IS NULL OR (ProductionDate IS NULL OR ExpiryDate >= ProductionDate)`.

#### inventory.InventoryLot (batch × warehouse) — **concurrency-controlled**
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| WarehouseId | uniqueidentifier | FK composite NN |
| ProductId | uniqueidentifier | FK composite NN |
| BatchId | uniqueidentifier | FK composite NULL — NULL = non-batched product |
| ExpirySortKey | date | NN — copied from `Batch.ExpiryDate`, or `9999-12-31` for non-expiry-tracked lots. Persisted so FEFO is a single index seek with no join to `Batch` (ADR-0041) |
| OnHandQuantity | decimal(18,4) | NN default 0 |
| ReservedQuantity | decimal(18,4) | NN default 0 (always 0 in V1 — no reservation entity, §24) |
| FirstReceivedAtUtc | datetime2(3) | NN |
| LastReceivedAtUtc | datetime2(3) | NULL |
| RowVersion | rowversion | rowv |

- UX filtered `UX_InventoryLot_NonBatched`: `(TenantId, WarehouseId, ProductId)
  WHERE BatchId IS NULL` (one non-batched lot per warehouse+product). `TenantId`
  leads every index here (ADR-0032).
- UX filtered `UX_InventoryLot_Batch`: `(TenantId, WarehouseId, BatchId) WHERE
  BatchId IS NOT NULL`. The two predicates are **mutually exclusive**, so together
  they admit exactly one lot per (tenant, warehouse, product) for untracked
  products and exactly one per (tenant, warehouse, batch) for tracked products.
  Both are required: dropping the first would leave the guarded `UPDATE` in §8.1
  with no single row to hit.
- IX: `(TenantId, ProductId)`, `(TenantId, WarehouseId, OnHandQuantity)`.
- CHECK: `OnHandQuantity >= 0`, `ReservedQuantity >= 0`,
  `ReservedQuantity <= OnHandQuantity`. These are absolute invariants; a
  `CHECK` is correct.
- IX for FEFO allocation: `(TenantId, WarehouseId, ProductId, ExpirySortKey,
  FirstReceivedAtUtc, Id)` INCLUDE `(OnHandQuantity)` — the FEFO ordering is an
  index seek, not a sort.

#### inventory.StockBalance (product × warehouse) — **concurrency-controlled**
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| WarehouseId | uniqueidentifier | FK composite NN |
| ProductId | uniqueidentifier | FK composite NN |
| OnHandQuantity | decimal(18,4) | NN default 0 |
| ReservedQuantity | decimal(18,4) | NN default 0 |
| IncomingQuantity | decimal(18,4) | NN default 0 (on open purchase orders; **not** ledger-rebuildable) |
| AverageUnitCost | decimal(18,4) | NN default 0 (moving weighted average, ADR-0017) |
| LastMovementAtUtc | datetime2(3) | NULL |
| LastCountedAtUtc | datetime2(3) | NULL (**not** ledger-rebuildable) |
| RowVersion | rowversion | rowv |

- UX `UX_StockBalance`: `(TenantId, WarehouseId, ProductId)` — `TenantId` leads
  (ADR-0032).
- IX: `(TenantId, WarehouseId, OnHandQuantity)`, `(TenantId, ProductId)`.
- CHECK: `OnHandQuantity >= 0`, `ReservedQuantity >= 0`,
  `ReservedQuantity <= OnHandQuantity`. Absolute invariants; a `CHECK` is correct.
- **Rebuild scope is partial** and MUST be documented as such
  (`PROJECT_BLUEPRINT.md` §11.1): the rebuild job recomputes `OnHandQuantity`,
  `AverageUnitCost`, and `LastMovementAtUtc` from `StockTransaction`; it restores
  `IncomingQuantity` from open PO lines, `LastCountedAtUtc` from posted counts,
  and leaves `ReservedQuantity` at 0. The rebuild-equivalence test asserts
  equality per column, not per row.

#### inventory.StockDocument (movement header)
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| WarehouseId | uniqueidentifier | FK composite NN |
| CounterWarehouseId | uniqueidentifier | FK composite NULL — target for TRANSFER |
| DocumentNumber | nvarchar(50) | NN (from ops.DocumentSequence) |
| MovementType | smallint | NN: 1=StockIn, 2=StockOut, 3=Return, 4=Transfer, 5=Adjustment, 6=Waste, 7=Count |
| Status | smallint | NN: 1=Draft, 2=Posted, 3=Cancelled, 4=Reversed |
| SourceGoodsReceiptId | uniqueidentifier | FK composite NULL — set when this document was created by a goods receipt |
| SourceStockCountId | uniqueidentifier | FK composite NULL — set when created by posting a count |
| SourceEventConsumptionId | uniqueidentifier | FK composite NULL — set when created by event consumption |
| SourceType | smallint | NN: 0=None, 1=GoodsReceipt, 2=StockCount, 3=EventConsumption — a **tag for the API**, not the referential link |
| EventId | uniqueidentifier | FK → events.Event NULL (consumption linked to an event) |
| ReasonCodeId | uniqueidentifier | FK composite NULL (required for Adjustment/Waste) |
| Reference | nvarchar(100) | NULL (external reference) |
| Notes | nvarchar(1000) | NULL |
| TotalQuantity | decimal(18,4) | NN default 0 (denormalised header total, maintained transactionally) |
| TotalCost | decimal(18,4) | NN default 0 |
| OccurredAtUtc | datetime2(3) | NN (business time) |
| PostedAtUtc | datetime2(3) | NULL |
| PostedByMembershipId | uniqueidentifier | FK NULL |
| DeviceId | uniqueidentifier | FK NULL |
| SessionId | uniqueidentifier | FK NULL |
| CorrelationId | nvarchar(100) | NULL |
| ReversedByDocumentId | uniqueidentifier | FK self NULL |
| ReversesDocumentId | uniqueidentifier | FK self NULL |
| IdempotencyKey | nvarchar(100) | NULL |
| RowVersion | rowversion | rowv |

- UX: `(TenantId, WarehouseId, DocumentNumber)`.
- UX (filtered): `(TenantId, IdempotencyKey) WHERE IdempotencyKey IS NOT NULL` —
  duplicate-post protection.
- IX: `(TenantId, WarehouseId, PostedAtUtc)`, `(TenantId, MovementType, PostedAtUtc)`,
  `(TenantId, EventId)`, `(TenantId, SourceGoodsReceiptId)`,
  `(TenantId, SourceStockCountId)`, `(TenantId, SourceEventConsumptionId)`.
- CHECK: `PostedAtUtc IS NULL OR Status IN (2,4)`.
- CHECK: exactly one source when `SourceType <> 0`:
  `(SourceType=0 AND SourceGoodsReceiptId IS NULL AND SourceStockCountId IS NULL AND SourceEventConsumptionId IS NULL) OR
  (SourceType=1 AND SourceGoodsReceiptId IS NOT NULL AND SourceStockCountId IS NULL AND SourceEventConsumptionId IS NULL) OR
  (SourceType=2 AND SourceGoodsReceiptId IS NULL AND SourceStockCountId IS NOT NULL AND SourceEventConsumptionId IS NULL) OR
  (SourceType=3 AND SourceGoodsReceiptId IS NULL AND SourceStockCountId IS NULL AND SourceEventConsumptionId IS NOT NULL)`.
  This is an absolute invariant, so a `CHECK` is correct.
- **No polymorphic `(SourceType, SourceDocumentId)` pair** (ADR-0038). The previous
  draft used a nullable tag plus a bare `uniqueidentifier` described as "FK …
  filtered". SQL Server cannot express a filtered FK to a table chosen at runtime,
  so the database would have accepted a document pointing at nothing. Typed
  nullable FKs make the reference real and let the database reject a bad
  posting. Polymorphic provenance is retained **only** in the append-only
  history tables (`ProductPriceHistory`, `EventShortageSnapshot`,
  `RecipeCostSnapshot`, `EventFoodCostSnapshot`), where a dangling reference is
  harmless because a row is never rewritten.
- Status transitions are enforced in the domain, not by a trigger.

#### inventory.StockDocumentLine
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| WarehouseId | uniqueidentifier | FK composite NN |
| StockDocumentId | uniqueidentifier | FK composite NN |
| LineNumber | smallint | NN |
| ProductId | uniqueidentifier | FK composite NN |
| BatchId | uniqueidentifier | FK composite NULL |
| Quantity | decimal(18,4) | NN (> 0) |
| UnitId | uniqueidentifier | FK composite NN |
| UnitCost | decimal(18,4) | NULL |
| Notes | nvarchar(500) | NULL |
| RowVersion | rowversion | rowv |

- UX: `(TenantId, StockDocumentId, LineNumber)`.
- IX: `(TenantId, ProductId)`, `(TenantId, BatchId)`.
- CHECK: `Quantity > 0`.
- **There is no `BalanceAfter` column here** (ADR-0037 rationale). A document
  line may split across several lots, so "the balance after this line" has no
  single value. The authoritative running balance lives on
  `inventory.StockTransaction.BalanceAfter` (one row per lot allocation), and
  the resulting `StockBalance` state is reconstructible from the document.

#### inventory.StockTransaction — **immutable ledger**
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| WarehouseId | uniqueidentifier | FK composite NN |
| StockDocumentId | uniqueidentifier | FK composite NN |
| StockDocumentLineId | uniqueidentifier | FK composite NN |
| ProductId | uniqueidentifier | FK composite NN |
| BatchId | uniqueidentifier | FK composite NULL |
| MovementType | smallint | NN |
| Direction | smallint | NN: 1=In, 2=Out |
| Quantity | decimal(18,4) | NN (> 0) |
| UnitId | uniqueidentifier | FK NN |
| UnitCost | decimal(18,4) | NULL |
| BalanceAfter | decimal(18,4) | NN (running balance, mandatory) |
| EventId | uniqueidentifier | FK NULL |
| ReasonCodeId | uniqueidentifier | FK NULL |
| Reference | nvarchar(100) | NULL |
| OccurredAtUtc | datetime2(3) | NN |
| PostedAtUtc | datetime2(3) | NN |
| UserId | uniqueidentifier | FK NN |
| MembershipId | uniqueidentifier | FK NN |
| DeviceId | uniqueidentifier | FK NULL |
| SessionId | uniqueidentifier | FK NULL |
| CorrelationId | nvarchar(100) | NULL |
| IsReversal | bit | NN default 0 |

- **No `RowVersion`, no `IsDeleted`** — the table is append-only by design. The
  application principal has `INSERT` and `SELECT` only on this table.
- IX: `(TenantId, WarehouseId, PostedAtUtc)`, `(TenantId, ProductId, PostedAtUtc)`,
  `(TenantId, EventId)`, `(TenantId, MovementType, PostedAtUtc)`.
- CHECK: `Quantity > 0`.
- Rebuild job: the ledger-derived columns of `StockBalance` and `InventoryLot`
  are recomputed from this table by `RebuildBalancesCommand`, which is
  implemented and tested. See `PROJECT_BLUEPRINT.md` §11.1 for the exact
  rebuild scope — the rebuild is **partial**, not whole-row.

#### inventory.StockCount
`Id`, `TenantId` (FK NN), `WarehouseId` (FK composite NN), `CountNumber` NN,
`Status` smallint NN (1=Draft, 2=Counting, 3=Submitted, 4=Posted, 5=Cancelled),
`ScopeType` smallint NN (1=Full, 2=Category, 3=Selected, 4=ExpiryBased),
`CategoryId` FK NULL, `CutoffAtUtc` NN, `CountedAtUtc` NULL,
`PostedDocumentId` FK → StockDocument NULL, `Notes` NULL,
`PostedByMembershipId` NULL, audit columns, `RowVersion`, `IdempotencyKey` NULL.
UX: `(TenantId, WarehouseId, CountNumber)`.

#### inventory.StockCountLine
`Id`, `TenantId` (FK NN), `StockCountId` FK composite NN, `ProductId` FK composite NN,
`BatchId` FK composite NULL, `UnitId` FK composite NN,
`ExpectedQuantity` decimal(18,4) NN, `CountedQuantity` decimal(18,4) NULL,
`VarianceQuantity` decimal(18,4) NULL, `UnitCost` NULL, `CountedByMembershipId` NULL,
`CountedAtUtc` NULL, `Resolution` smallint NN (1=None,2=Accepted,3=Recount,4=Ignored),
`Notes` NULL, `RowVersion`.
UX: `(TenantId, StockCountId, ProductId, BatchId)` **and** a second UX
`(TenantId, StockCountId, ProductId) WHERE BatchId IS NULL` — without the
filtered variant, two count lines for the same non-batched product would be
allowed (SQL Server treats NULLs as equal only inside an `IS NULL`-filtered
index, ADR-0041).
CHECK: `VarianceQuantity IS NULL OR VarianceQuantity = CountedQuantity - ExpectedQuantity`.

---

### 4.6 `purchasing`

#### purchasing.Supplier
`Id`, `TenantId` (FK NN), `Code` NN, `Name` NN, `NameAr` NULL, `ContactName` NULL,
`Email` NULL, `Phone` NULL, `Address` NULL, `TaxNumber` NULL, `PaymentTermsDays` int NULL,
`CurrencyCode` char(3) NULL, `IsActive` bit NN,
`Notes` NULL, `IsDeleted` sd, `RowVersion`.
UX (filtered): `(TenantId, Code) WHERE IsDeleted = 0`. IX: `(TenantId, Name)`.
**There is no `Rating` column in V1.** The previous draft carried
`Supplier.Rating` while §4.5 declared supplier-performance scoring a V2
feature, so the schema would have shipped a field nothing computes or reads.
`CurrencyCode` is informational only — V1 is single-currency per tenant
(§23 TBD-21), so it never triggers an FX conversion.

#### purchasing.SupplierProduct
`Id`, `TenantId` (FK NN), `SupplierId` FK composite NN, `ProductId` FK composite NN,
`SupplierSku` nvarchar(50) NULL, `UnitId` FK composite NN, `LastUnitPrice` decimal(18,4) NULL,
`LastPurchasedAtUtc` NULL, `MinimumOrderQuantity` decimal(18,4) NULL,
`IsPreferred` bit NN default 0, `IsActive` bit NN, `RowVersion`.
UX: `(TenantId, SupplierId, ProductId)`. IX: `(TenantId, ProductId)`.

#### purchasing.ProductPriceHistory
`Id`, `TenantId` (FK NN), `ProductId` FK composite NN, `SupplierId` FK NULL,
`WarehouseId` FK composite NULL, `UnitId` FK composite NN, `UnitPrice` decimal(18,4) NN (> 0),
`EffectiveFromUtc` datetime2(3) NN, `SourceType` smallint NN (1=PoLine, 2=GoodsReceipt, 3=Manual),
`SourceId` uniqueidentifier NULL (provenance tag only — **deliberately not an
FK**; this table is append-only and immutable, so a dangling reference is
harmless, ADR-0038), `CreatedByMembershipId` FK NULL.
IX: `(TenantId, ProductId, EffectiveFromUtc DESC)`.
**Append-only** (no delete, no update) — therefore **no `RowVersion`**; the
current price is the newest row for `(TenantId, ProductId, SupplierId)`. A
`rowversion` column on a table that must never change is a contradiction and
would also break the `INSERT`-only database principal (ADR-0037).

#### purchasing.PurchaseRequest
`Id`, `TenantId` (FK NN), `RequestNumber` NN, `WarehouseId` FK composite NN,
`Status` smallint NN (1=Draft, 2=PendingApproval, 3=Approved, 4=Rejected, 5=Converted, 6=Cancelled),
`Priority` smallint NN (1=Normal,2=High,3=Urgent), `Justification` nvarchar(1000) NULL,
`RequiredByDate` date NULL, `RequestedByMembershipId` FK NN, `ApprovedAtUtc` NULL,
`ConvertedPurchaseOrderId` FK NULL, `Notes` NULL, `TotalEstimatedCost` decimal(18,4) NULL,
`RowVersion`, `IdempotencyKey` NULL.
UX: `(TenantId, RequestNumber)`. IX: `(TenantId, Status)`.

#### purchasing.PurchaseRequestLine
`Id`, `TenantId` (FK NN), `PurchaseRequestId` FK composite NN, `LineNumber` smallint NN,
`ProductId` FK composite NN, `Quantity` decimal(18,4) NN, `UnitId` FK composite NN,
`EstimatedUnitCost` decimal(18,4) NULL, `PreferredSupplierId` FK NULL, `Notes` NULL,
`RowVersion`. UX: `(PurchaseRequestId, LineNumber)`. CHECK: `Quantity > 0`.

#### purchasing.PurchaseApproval
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| EntityType | smallint | NN: 1=PurchaseRequest, 2=PurchaseOrder — a tag for the API |
| PurchaseRequestId | uniqueidentifier | FK composite NULL |
| PurchaseOrderId | uniqueidentifier | FK composite NULL |
| StepNumber | smallint | NN |
| ApproverMembershipId | FK composite NN | |
| Decision | smallint | NN: 1=Pending, 2=Approved, 3=Rejected |
| DecidedAtUtc | NULL | |
| Comments | nvarchar(1000) | NULL |
| RequiredPermissionCode | nvarchar(100) | NULL — the permission the approver had to hold |
| RowVersion | rowversion | rowv |

UX: `(TenantId, EntityType, StepNumber, ApproverMembershipId)`.
UX (filtered): `(TenantId, EntityType, ApproverMembershipId) WHERE Decision = 1`
(at most one pending decision per approver per entity).
CHECK: exactly one target — `(EntityType=1 AND PurchaseRequestId IS NOT NULL AND
PurchaseOrderId IS NULL) OR (EntityType=2 AND PurchaseRequestId IS NULL AND
PurchaseOrderId IS NOT NULL)`. An approval with an unreferenced `EntityId` would
be invisible to the approval workflow; typed FKs make the reference real
(ADR-0038).

#### purchasing.PurchaseOrder
`Id`, `TenantId` (FK NN), `OrderNumber` NN, `SupplierId` FK composite NN,
`WarehouseId` FK composite NN, `SourceRequestId` FK NULL,
`Status` smallint NN (1=Draft, 2=PendingApproval, 3=Approved, 4=PartiallyReceived, 5=Received, 6=Cancelled, 7=Closed),
`CurrencyCode` char(3) NN, `ExpectedDeliveryDate` date NULL, `OrderedAtUtc` NULL,
`Subtotal` decimal(18,4) NN default 0, `TaxAmount` decimal(18,4) NN default 0,
`ShippingAmount` decimal(18,4) NN default 0, `TotalAmount` decimal(18,4) NN default 0,
`AllowOverReceipt` bit NN default 0, `Notes` NULL, `ApprovedAtUtc` NULL,
`CreatedByMembershipId` FK NN, `RowVersion`, `IdempotencyKey` NULL.
UX: `(TenantId, OrderNumber)`. IX: `(TenantId, SupplierId, Status)`, `(TenantId, Status, ExpectedDeliveryDate)`.

#### purchasing.PurchaseOrderLine
`Id`, `TenantId` (FK NN), `PurchaseOrderId` FK composite NN, `LineNumber` smallint NN,
`ProductId` FK composite NN, `OrderedQuantity` decimal(18,4) NN, `ReceivedQuantity` decimal(18,4) NN default 0,
`UnitId` FK composite NN, `UnitPrice` decimal(18,4) NN,
`LineTotal` decimal(18,4) NN, `ExpectedDate` date NULL, `Notes` NULL, `RowVersion`.
UX: `(TenantId, PurchaseOrderId, LineNumber)`. IX: `(TenantId, ProductId)`.
CHECK: `OrderedQuantity > 0`, `ReceivedQuantity >= 0`.
**There is deliberately no `CHECK (ReceivedQuantity <= OrderedQuantity)`**
(ADR-0039). That constraint contradicts a documented business rule (§23 TBD-08:
over-receipt is allowed when `AllowOverReceipt = true`), and a `CHECK` cannot be
relaxed from application code — the database would reject the very posting the
product allows. The upper bound is enforced instead by a **guarded `UPDATE`**
that joins the parent flag and the tenant tolerance, plus the
`purchase_order_over_receipt` business error, plus an audit record for every
over-receipt. `CHECK` constraints are reserved for absolute invariants with no
legitimate exception.

#### purchasing.GoodsReceipt
`Id`, `TenantId` (FK NN), `ReceiptNumber` NN, `PurchaseOrderId` FK composite NULL,
`WarehouseId` FK composite NN, `SupplierId` FK composite NN, `DeliveryNoteNumber` nvarchar(100) NULL,
`Status` smallint NN (1=Draft, 2=Posted, 3=Cancelled),
`ReceivedAtUtc` datetime2(3) NN, `PostedStockDocumentId` FK → StockDocument NULL,
`Notes` NULL, `PostedByMembershipId` FK NULL, `TotalCost` decimal(18,4) NULL,
`RowVersion`, `IdempotencyKey` NULL.
UX: `(TenantId, ReceiptNumber)`. IX: `(TenantId, WarehouseId, ReceivedAtUtc)`.

#### purchasing.GoodsReceiptLine
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| GoodsReceiptId | FK composite NN | |
| PurchaseOrderLineId | FK composite NULL | NULL for a receipt without a PO |
| LineNumber | smallint | NN |
| ProductId | FK composite NN | |
| Quantity | decimal(18,4) | NN |
| UnitId | FK composite NN | |
| UnitCost | decimal(18,4) | NN |
| BatchId | FK composite NULL | **required when the product is batch-tracked**; the batch is created by this posting when not supplied |
| ExpiryDate | date | NULL — required when the product is expiry-tracked |
| ProductionDate | date | NULL |
| Notes | nvarchar(500) | NULL |
| RowVersion | rowversion | rowv |

UX: `(TenantId, GoodsReceiptId, LineNumber)`.
CHECK: `Quantity > 0`, `UnitCost >= 0`.
The previous draft had no `BatchId` on this table while `InventoryLot` and
`StockDocumentLine` both referenced a batch created "here when `BatchId` is not
supplied" — the column the sentence referred to did not exist. It exists now,
and it is what links the receipt to the lot the stock was posted into.

---

### 4.7 `events`

#### events.Event
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| Code | nvarchar(50) | NN |
| Name | nvarchar(200) | NN |
| NameAr | nvarchar(200) | NULL |
| Description | nvarchar(1000) | NULL |
| Type | smallint | NN: 1=Conference, 2=Residence, 3=Wedding, 4=Training, 5=Other |
| WarehouseId | uniqueidentifier | FK composite NN (primary operating warehouse) |
| StartAtUtc / EndAtUtc | datetime2(3) | NN |
| DurationDays | smallint | NN (> 0, maintained from dates) |
| ExpectedAttendees | int | NN default 0 |
| Status | smallint | NN: 1=Planning, 2=Confirmed, 3=InProgress, 4=Completed, 5=Cancelled |
| MealPlanNotes | nvarchar(1000) | NULL |
| BudgetAmount | decimal(18,4) | NULL |
| IsDeleted | bit | NN sd |
| RowVersion | rowversion | rowv |

UX (filtered): `(TenantId, Code) WHERE IsDeleted = 0`.
IX: `(TenantId, Status, StartAtUtc)`. CHECK: `EndAtUtc > StartAtUtc`.
**There is no `CostSnapshotId` column** (ADR-0037). The previous draft pointed
at `food.EventFoodCostSnapshot` — an append-only table whose row is written
*after* the event is costed, so a foreign key from `Event` would either be
permanently null or force the snapshot row to be written first. The current
snapshot is "the newest `EventFoodCostSnapshot` for this event"; a stored
pointer created a two-way update cycle for no benefit.

#### events.EventRequirement
`Id`, `TenantId` (FK NN), `EventId` FK composite NN, `ProductId` FK composite NN,
`MealTypeId` FK → food.MealType NULL, `BasisType` smallint NN (1=Total, 2=PerPerson, 3=PerPersonPerDay, 4=PerPersonPerMeal),
`QuantityPerBasisUnit` decimal(18,4) NULL, `TotalRequiredQuantity` decimal(18,4) NN,
`UnitId` FK composite NN, `IsCritical` bit NN default 0, `Notes` NULL, `RowVersion`.
UX: `(TenantId, EventId, ProductId, MealTypeId)` **and** a second UX
`(TenantId, EventId, ProductId) WHERE MealTypeId IS NULL` — without the filtered
variant, two event-wide requirements for the same product would both be allowed
(SQL Server treats NULLs as equal only inside an `IS NULL`-filtered index,
ADR-0041).
CHECK: `BasisType = 1 OR QuantityPerBasisUnit IS NOT NULL`.

#### events.EventConsumption
`Id`, `TenantId` (FK NN), `EventId` FK composite NN, `WarehouseId` FK composite NN,
`ProductId` FK composite NN, `MealTypeId` FK NULL, `BatchId` FK NULL,
`PlannedQuantity` decimal(18,4) NN, `ConsumedQuantity` decimal(18,4) NN default 0,
`UnitId` FK composite NN, `ActualUnitCost` decimal(18,4) NULL,
`StockDocumentId` FK → inventory.StockDocument NULL (proof of actual consumption),
`RowVersion`.
UX: `(TenantId, EventId, ProductId, MealTypeId)` **and** a second UX
`(TenantId, EventId, ProductId) WHERE MealTypeId IS NULL` (ADR-0041).
CHECK: `ConsumedQuantity >= 0`.
**No `CHECK (ConsumedQuantity <= PlannedQuantity * 2)`** (ADR-0039). Actual
consumption legitimately exceeds a plan — that is precisely the signal a
manager needs — so the ×2 sanity guard is an **application** rule with a
configurable multiplier, an override permission, and an audit record, not a
`CHECK` the code is expected to bypass.

#### events.EventAttendance
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| TenantId | uniqueidentifier | FK NN |
| EventId | FK composite NN | |
| MembershipId | FK composite NULL | NULL for a walk-in guest — no Stockly account |
| GuestName | nvarchar(200) | NULL |
| IsAttending | bit | NN |
| DaysAttending | smallint | NN default 1 |
| MealsPerDay | decimal(3,1) | NULL — V1 simplification (§23 TBD-10): whole/half meals per day. `NULL` = not planned |
| Notes | nvarchar(1000) | NULL |
| RowVersion | rowversion | rowv |

UX (filtered): `(TenantId, EventId, MembershipId) WHERE MembershipId IS NOT NULL`.
UX (filtered): `(TenantId, EventId, GuestName) WHERE MembershipId IS NULL` — the
walk-in list, deduplicated by name.
CHECK: `DaysAttending > 0`, `MealsPerDay IS NULL OR MealsPerDay >= 0`.

> **V1 simplification (resolved, no child rows).** The previous draft left
> `Meals` as "per-meal flags in child rows (see below) OR simplified counters",
> with the child table never defined — the requirement calculation had two
> possible shapes and no owner had chosen. V1 stores the daily counters
> (`DaysAttending`, `MealsPerDay`) on the attendance row. A per-meal-type
> attendance matrix, which needs a real child table, is V1.1 and stays inside
> TBD-10.

#### events.EventShortageSnapshot
`Id`, `TenantId` (FK NN), `EventId` FK composite NN, `WarehouseId` FK composite NN,
`ProductId` FK composite NN, `CalculatedAtUtc` datetime2(3) NN,
`RequiredQuantity` decimal(18,4) NN, `OnHandQuantity` decimal(18,4) NN,
`IncomingQuantity` decimal(18,4) NN, `ShortageQuantity` decimal(18,4) NN,
`RecommendedPurchaseQuantity` decimal(18,4) NN, `UnitId` FK composite NN,
`UnitCost` decimal(18,4) NULL, `EstimatedCost` decimal(18,4) NULL,
`CreatedByMembershipId` FK NULL.
**Append-only** (ADR-0037): no `RowVersion`, no `IsDeleted`, no update. The
current shortage for an event/product is the newest row.
UX: `(TenantId, EventId, CalculatedAtUtc)`; IX: `(TenantId, EventId, ProductId)`.
Used for trend reporting ("shortage improved over time").

---

### 4.8 `food`

#### food.MealType
`Id`, `TenantId` (FK NN), `Code` NN, `Name` NN, `NameAr` NULL,
`DefaultServingsPerDay` decimal(5,2) NULL, `SortOrder` int NN, `IsActive` bit NN,
`IsDeleted` sd, `RowVersion`. UX (filtered): `(TenantId, Code) WHERE IsDeleted = 0`.

#### food.Recipe
`Id`, `TenantId` (FK NN), `Code` NN, `Name` NN, `NameAr` NULL, `Description` NULL,
`MealTypeId` FK composite NN, `YieldQuantity` decimal(18,4) NN (> 0),
`YieldUnitId` FK composite NN, `ServingsPerYield` decimal(9,2) NULL (§23 TBD-10 — a
nullable owner-supplied value, **not** a design TBD),
`PreparationMinutes` int NULL, `CookingMinutes` int NULL, `Difficulty` smallint NULL,
`Instructions` nvarchar(max) NULL, `ImageAttachmentId` FK → files.Attachment NULL,
`IsActive` bit NN, `IsDeleted` sd, `RowVersion`.
UX (filtered): `(TenantId, Code) WHERE IsDeleted = 0`.
CHECK: `YieldQuantity > 0`.

#### food.RecipeItem
`Id`, `TenantId` (FK NN), `RecipeId` FK composite NN, `LineNumber` smallint NN,
`ProductId` FK composite NN, `QuantityPerYield` decimal(18,4) NN,
`UnitId` FK composite NN, `WastagePercent` decimal(5,2) NN default 0,
`IsOptional` bit NN default 0, `Notes` NULL, `RowVersion`.
UX: `(RecipeId, LineNumber)`. CHECK: `QuantityPerYield > 0`, `WastagePercent BETWEEN 0 AND 100`.

#### food.RecipeCostSnapshot
`Id`, `TenantId` (FK NN), `RecipeId` FK composite NN, `CalculatedAtUtc` datetime2(3) NN,
`TotalCost` decimal(18,4) NN, `YieldQuantity` decimal(18,4) NN, `YieldUnitId` FK NN,
`CostPerYieldUnit` decimal(18,4) NN, `ServingsPerYield` decimal(9,2) NULL,
`CostPerServing` decimal(18,4) NULL, `CostSourceSummary` nvarchar(500) NULL
(human-readable provenance: e.g. `3 estimated, 5 actual`),
`ValuationMethod` smallint NN (1=MovingAverage, 2=LastPurchasePrice, 3=StandardCost),
`LineCosts` nvarchar(max) NULL (JSON per-ingredient breakdown — justified as a
point-in-time read snapshot, not a source of truth),
`CreatedByMembershipId` FK NULL.
**Append-only** (ADR-0037): no `RowVersion`, no update, no delete. The current
recipe cost is the newest row.
IX: `(TenantId, RecipeId, CalculatedAtUtc DESC)`.

#### food.EventFoodCostSnapshot
`Id`, `TenantId` (FK NN), `EventId` FK composite NN, `CalculatedAtUtc` NN,
`MealTypeId` FK NULL, `TotalServings` int NN, `TotalCost` decimal(18,4) NN,
`CostPerServing` decimal(18,4) NULL, `CostPerAttendee` decimal(18,4) NULL,
`CurrencyCode` char(3) NN, `Breakdown` nvarchar(max) NULL (JSON snapshot).
**Append-only** (ADR-0037): no `RowVersion`, no update, no delete. There is
**no `IsCurrent` flag** — "current" is the newest row per
`(TenantId, EventId, MealTypeId)`. A mutable flag on an append-only table means
two writers can disagree about which row is current, and the previous draft's
`RowVersion` + `IsCurrent` pair implied updates that the design forbids.
IX: `(TenantId, EventId, CalculatedAtUtc DESC)`.
UX: `(TenantId, EventId, CalculatedAtUtc, MealTypeId)` plus a filtered variant
`(TenantId, EventId, CalculatedAtUtc) WHERE MealTypeId IS NULL` (ADR-0041), so
one recalculation cannot insert two rows for the same event/meal/time.

---

### 4.9 `notifications`, `files`, `extensibility`, `audit`

#### notifications.Notification
`Id`, `TenantId` (FK NN), `MembershipId` FK composite NN, `Category` smallint NN,
`Severity` smallint NN (1=Info, 2=Warning, 3=Critical),
`Title` nvarchar(200) NN, `Body` nvarchar(1000) NULL, `ActionUrl` nvarchar(300) NULL,
`EntityName` nvarchar(100) NULL, `EntityId` uniqueidentifier NULL,
`IsRead` bit NN default 0, `ReadAtUtc` NULL, `CreatedAtUtc` NN, `ExpiresAtUtc` NULL.
IX: `(TenantId, MembershipId, IsRead, CreatedAtUtc DESC)`.

#### notifications.NotificationPreference
`Id`, `TenantId` (FK NN), `MembershipId` FK composite NN, `Category` smallint NN,
`Channel` smallint NN (1=InApp, 2=Push, 3=Email), `IsEnabled` bit NN, `RowVersion`.
UX: `(MembershipId, Category, Channel)`.

#### notifications.PushSubscription
`Id`, `TenantId` (FK NN), `MembershipId` FK composite NN, `DeviceId` FK NULL,
`Endpoint` nvarchar(500) NN, `P256dh` nvarchar(200) NULL, `Auth` nvarchar(200) NULL,
`UserAgent` nvarchar(400) NULL, `ExpiresAtUtc` NULL, `IsActive` bit NN, `CreatedAtUtc` NN.
UX: `(Endpoint)` — the push endpoint is a globally unique, server-issued URL, not
tenant business data (ADR-0032 exception). Keys are stored because the push
service requires them;
see ADR-0031 (push keys are secrets-adjacent and never exposed to other tenants).

#### files.Attachment
`Id`, `TenantId` (FK NN), `EntityName` nvarchar(100) NN, `EntityId` uniqueidentifier NN,
`FileName` nvarchar(300) NN, `ContentType` nvarchar(150) NN, `SizeBytes` bigint NN,
`Sha256` binary(32) NN, `StorageKey` nvarchar(200) NN **UQ globally** (ADR-0032
exception: a server-generated GUID path must resolve to exactly one blob,
`Append`/`GET`/`DELETE` by key have no tenant context),
`UploadedByMembershipId` FK NN, `UploadedAtUtc` NN, `IsDeleted` bit NN sd,
`OcrStatus` smallint NN default 0 (0=NotApplicable, 1=Pending, 2=Processed, 3=Failed),
`ExtractedText` nvarchar(max) NULL (reserved for V2 OCR),
`RowVersion` rowv.
IX: `(TenantId, EntityName, EntityId)`. CHECK: `SizeBytes >= 0`.
**The original file name is metadata only and is never used to build a path.**

#### extensibility.CustomFieldDefinition
`Id`, `TenantId` (FK NN), `EntityName` nvarchar(100) NN, `FieldKey` nvarchar(100) NN,
`Label` nvarchar(200) NN, `LabelAr` NULL, `FieldType` smallint NN (1=Text, 2=Number, 3=Date, 4=Boolean, 5=Dropdown, 6=MultiSelect),
`IsRequired` bit NN default 0, `IsFilterable` bit NN default 0,
`Options` nvarchar(max) NULL (JSON — justified: dropdown options are user-defined),
`DisplayOrder` int NN, `IsActive` bit NN, `IsDeleted` sd, `RowVersion`.
UX (filtered): `(TenantId, EntityName, FieldKey) WHERE IsDeleted = 0`.

#### extensibility.CustomFieldValue
`Id`, `TenantId` (FK NN), `CustomFieldDefinitionId` FK composite NN,
`EntityName` nvarchar(100) NN, `EntityId` uniqueidentifier NN,
`ValueJson` nvarchar(max) NN (JSON — justified: user-defined field type unknown at design time),
`CreatedAtUtc` NN, `UpdatedAtUtc` NN, `RowVersion`.
UX: `(CustomFieldDefinitionId, EntityName, EntityId)`.

#### audit.AuditLog — append-only
| Column | Type | Notes |
|---|---|---|
| Id | bigint identity | PK, high volume |
| OccurredAtUtc | datetime2(3) | NN — **partition key** (monthly) |
| TenantId | uniqueidentifier | FK NULL (NULL for platform-level events) |
| Category | smallint | NN: 1=Security, 2=Identity, 3=Inventory, 4=Purchasing, 5=Events, 6=Food, 7=Administration, 8=Platform |
| Severity | smallint | NN: 1=Info, 2=Warning, 3=Critical |
| Action | nvarchar(100) | NN, e.g. `stock.out.post` |
| Result | smallint | NN: 1=Success, 2=Failure, 3=Denied |
| EntityName | nvarchar(100) | NULL |
| EntityId | nvarchar(100) | NULL (string, because some entities use non-GUID keys) |
| ActorUserId | uniqueidentifier | FK NULL |
| ActorMembershipId | uniqueidentifier | FK NULL |
| DeviceId | uniqueidentifier | FK NULL |
| SessionId | uniqueidentifier | FK NULL |
| IpAddress | nvarchar(45) | NULL |
| UserAgent | nvarchar(400) | NULL |
| CorrelationId | nvarchar(100) | NULL |
| ReasonCode | nvarchar(200) | NULL |
| ProductId | uniqueidentifier | FK NULL |
| Quantity | decimal(18,4) | NULL |
| UnitId | uniqueidentifier | FK NULL |
| BeforeJson | nvarchar(max) | NULL — redacted |
| AfterJson | nvarchar(max) | NULL — redacted |

- **No `RowVersion`, no update path, no delete path.**
- IX: `(TenantId, OccurredAtUtc DESC)`, `(Category, OccurredAtUtc DESC)`,
  `(ActorUserId, OccurredAtUtc DESC)`, `(EntityName, EntityId, OccurredAtUtc DESC)`,
  `(CorrelationId)`.
- Application principal: `INSERT` + `SELECT` only. `DELETE`/`UPDATE` are not granted.
- Partitioned monthly; retention per §23 TBD-14.

---

## 5. Referential integrity rules

1. **Composite tenant FKs everywhere.** A tenant-scoped child references its
   parent as `FOREIGN KEY (ParentId, TenantId) REFERENCES parent (Id, TenantId)`.
   This makes a cross-tenant row physically impossible to insert. It requires the
   parent to have `UNIQUE (Id, TenantId)`.
2. **Restrict, do not cascade**, for business data. `ON DELETE NO ACTION`
   (default) is used for everything that matters; a `CASCADE` exists only from
   child configuration rows to their definition (e.g. `MembershipRoleWarehouse`).
3. **Soft delete** instead of physical delete for master data, so historical
   transactions keep valid references. Physical purge is a separate,
   audited, retention-driven job (V2).
4. **Ledger tables have no delete path** — `StockTransaction`, `AuditLog`,
   `ProductPriceHistory`, `EventShortageSnapshot`, `RecipeCostSnapshot`,
   `EventFoodCostSnapshot` are append-only, and therefore carry **no
   `RowVersion`** (ADR-0037).
5. Unique indexes on soft-deleted tables are **filtered** (`WHERE IsDeleted = 0`)
   so a code can be reused after deletion while history remains intact.
6. **Every unique index on a tenant-scoped table leads with `TenantId`**
   (ADR-0032). The only exceptions are indexes guarding a server-generated global
   identifier: `files.Attachment.StorageKey`,
   `identity.RefreshToken.TokenHash`, `identity.PasswordResetToken.TokenHash`,
   `identity.Device.EnrollmentCode`, `notifications.PushSubscription.Endpoint`,
   `identity.LoginAttempt.Id`, and `audit.AuditLog.Id`.
7. **A unique index over a nullable column needs an explicit filtered variant**
   (`WHERE <col> IS NULL`) for each `IS NULL` shape that must be unique
   (ADR-0041). SQL Server treats NULLs as equal only inside such a predicate.
   Tables affected: `inventory.Batch`, `inventory.StockCountLine`,
   `events.EventRequirement`, `events.EventConsumption`,
   `food.EventFoodCostSnapshot`, `master.UnitConversion`,
   `events.EventAttendance` (walk-ins).
8. **`CHECK` constraints carry absolute invariants only** (ADR-0039). If the
   product ever allows an exception, the bound is a guarded `UPDATE` in the
   application, never a `CHECK` the code must bypass. Current exceptions:
   `purchasing.PurchaseOrderLine.ReceivedQuantity` (over-receipt) and
   `events.EventConsumption.ConsumedQuantity` (over-consumption).

---

## 6. Row-level integrity against privilege escalation

`Role.AuthorityLevel` (1–100) provides a database-visible guard:

- A membership can only be granted a role whose `AuthorityLevel` is **less than
  or equal to** the highest `AuthorityLevel` the granting membership already
  holds, **unless** the granter holds `platform.*` authority.
- `TenantOwner` is level 100 and cannot be granted by a tenant-level role
  without platform authority.
- The last active `TenantOwner` cannot be demoted or removed (triggered by a
  deferred constraint check at the application level, verified by a database
  test).
- Custom roles created by a membership cannot exceed that membership's own
  authority level.

This is defence in depth: the API enforces it, the audit log records it, and a
test suite attempts escalation through every path.

---

## 7. Indexing strategy

| Access pattern | Index |
|---|---|
| Tenant-scoped list by page | `(TenantId, <filter columns>, Id)` including the filter columns |
| Warehouse list of a tenant | `(TenantId, WarehouseId, IsActive)` |
| Balance lookup for update | `UX_StockBalance (TenantId, WarehouseId, ProductId)` |
| Non-batched lot lookup | `UX_InventoryLot_NonBatched (TenantId, WarehouseId, ProductId) WHERE BatchId IS NULL` — the single lot row the guarded `UPDATE` in §8.1 targets for untracked products |
| Batched lot lookup | `UX_InventoryLot_Batch (TenantId, WarehouseId, BatchId) WHERE BatchId IS NOT NULL` |
| Lot lookup for FEFO | `IX_InventoryLot_FEFO (TenantId, WarehouseId, ProductId, ExpirySortKey, FirstReceivedAtUtc, Id) INCLUDE (OnHandQuantity)` |
| Movement ledger by date | `(TenantId, WarehouseId, PostedAtUtc)` incl. `(ProductId, Quantity)` |
| Ledger by product | `(TenantId, ProductId, PostedAtUtc)` |
| Purchase orders by supplier/status | `(TenantId, SupplierId, Status)` |
| Expiring lots | **No filtered index.** A filtered index predicate must be deterministic, and `WHERE ExpirySortKey < DATEADD(day, 30, SYSUTCDATETIME())` is not — SQL Server would reject it. Implemented instead as a nightly job over `IX_InventoryLot_FEFO`, writing `near-expiry` notifications (§23 TBD-07) |
| Near-expiry report (on demand) | `(TenantId, WarehouseId, ExpirySortKey)` with a **parameterised** `WHERE ExpirySortKey < @windowEnd` query |
| Audit by actor | `(ActorUserId, OccurredAtUtc DESC)` |
| Global search | SQL Server full-text index (`Arabic_CI_AS`) on `master.Product`, `purchasing.Supplier`, `events.Event`, `master.Category` — §23 TBD-17 / ADR-0042 |
| Session checks | `IX_AuthSession_Membership_Status`, `IX_AuthSession_Device_Status`, `IX_AuthSession_Platform_Active` |
| Throttle lookups | `(IdentifierHash, OccurredAtUtc)`, `(EnrollmentCodeHash, OccurredAtUtc)`, `(IpAddress, OccurredAtUtc)` |

Pagination is **keyset-based** (seek to the last seen `Id`/`OccurredAtUtc`),
never `OFFSET` on large tables, so deep pages stay fast.

---

## 8. Transaction patterns

### 8.1 Posting a stock document (canonical example)

```sql
SET XACT_ABORT ON;
BEGIN TRANSACTION;

  -- 1. Lock the document and assert it is postable
  UPDATE inventory.StockDocument
     SET Status = 2, PostedAtUtc = SYSUTCDATETIME(), PostedByMembershipId = @mid
   WHERE Id = @docId AND TenantId = @tid AND WarehouseId = @wid AND Status = 1;
  IF @@ROWCOUNT = 0 THROW 50001, 'document_not_postable', 1;   -- guards double posting

  -- 2. For each line (in LineNumber order to avoid deadlocks):
  --    2a. guarded balance update
  UPDATE inventory.StockBalance
     SET OnHandQuantity = OnHandQuantity - @qty, LastMovementAtUtc = SYSUTCDATETIME()
   WHERE Id = @balanceId AND TenantId = @tid AND WarehouseId = @wid
     AND OnHandQuantity >= @qty;
  IF @@ROWCOUNT = 0 THROW 50002, 'insufficient_stock', 1;

  --    2b. lot update (FEFO allocation, or explicit batch from the line)
  --    2c. insert the immutable ledger row with BalanceAfter
  --    2d. update running weighted average cost

  -- 3. Audit rows (same transaction)
  INSERT INTO audit.AuditLog (...) VALUES (...);

  -- 4. Outbox message for notifications (same transaction)
  INSERT INTO ops.OutboxMessage (...) VALUES (...);

COMMIT;
```

Rules: one transaction per use case; `SET XACT_ABORT ON`; all rows for a given
document locked in a **consistent order** (`LineNumber`, then `ProductId`, then
`WarehouseId`) to prevent deadlocks; deadlock victims are retried by the
middleware (max 3 attempts with jitter) because the operation is idempotent.

### 8.2 Idempotent posting

```text
Idempotency-Key arrives
  → SELECT existing StockDocument WHERE TenantId=@tid AND IdempotencyKey=@key
      found → return the original result, do NOT post again
      not found → attempt insert; unique index race → re-select and return original
```

### 8.3 Document numbering

```sql
UPDATE ops.DocumentSequence
   SET NextValue = NextValue + 1
 WHERE TenantId = @tid AND DocumentType = @type AND PeriodKey = @period;
SELECT NextValue FROM ops.DocumentSequence WHERE ...;
```

`UPDLOCK, HOLDLOCK` is added for the read to make the pair atomic.

---

## 9. Data retention

| Data | Retention | Mechanism |
|---|---|---|
| Stock transactions | Permanent (business record) | Append-only |
| Audit log | §23 TBD-14 (provisional 24 months online) | Monthly partitions, archive to cold storage |
| Login attempts | 6 months (proposed) | Monthly partitions, automated purge job |
| Sessions | 13 months (proposed, for investigation) | Purge job for `Expired`/`Revoked` |
| Refresh tokens | 30 days after expiry (proposed) | Purge job |
| Password reset tokens | 7 days after expiry (proposed) | Purge job |
| Outbox messages | 30 days after processing (proposed) | Purge job |
| Attachments | §23 TBD-14 (provisional 24 months); per-tenant override via `TenantSetting` | Soft delete + purge job |
| Cost snapshots | Permanent | Append-only |

All retention values marked "proposed" require owner confirmation. Each purge job
is audited, and a purge never touches an append-only ledger or audit table.

---

## 10. Migration policy

- EF Core migrations are code-reviewed before merge.
- Migrations are **additive first**: add nullable column → backfill → add
  constraint → later drop the old column in a separate migration.
- Destructive changes require an explicit decision in `DECISIONS.md` and a
  documented rollback/backup plan.
- Production schema changes run as a gated deployment step. The application
  **never** auto-migrates on startup in production.
- Every migration that touches an audit or ledger table must preserve
  append-only semantics.
- Schema drift is detected in CI by comparing the model snapshot with the
  latest migration (`dotnet ef migrations has-pending-model-changes`).

---

## 11. Full-text search

Resolved: §23 TBD-17 / **ADR-0042**. V1 uses a SQL Server `FULLTEXT` catalog
over `master.Product(Name, NameAr, Sku)`, `master.ProductBarcode(Barcode)`,
`purchasing.Supplier(Name, NameAr)`, `events.Event(Name, NameAr, Code)`, and
`master.Category(Name, NameAr)`, with the `Arabic_CI_AS` collation. Barcodes are
matched by the unique index, not by full text.

**Note on search result ranking:** full-text ranking uses `Arabic_CI_AS`, which
does **not** perform Arabic linguistic stemming or diacritic normalisation. This
is an accepted V1 limitation (the engine choice is revisited in V2 only if a
measured requirement appears). A trigram `LIKE` fallback is kept as a secondary
path for partial codes, and the design is benchmarked in Phase 7 before the
reporting/search phase is declared done.

---

## 12. Open database questions

Every item below has a **disposition** and is mirrored from the §23 register, so
none of them is a silent open question.

| ID | Question | Resolved by | Disposition |
|---|---|---|---|
| DB-TBD-01 | Fractional quantities in V1, and which base units? | TBD-16 | **Assumption (safe superset):** `decimal(18,4)` everywhere, `Unit.DecimalPlaces` 0–4. V1 schema already supports it |
| DB-TBD-02 | Warehouse sub-locations in V1 or V1.1? | TBD-19 | **Owner decision:** V1 has none; additive in V1.1 |
| DB-TBD-03 | Stock reservation model for event planning | TBD-10 / V2 | **Deferred to V2.** `ReservedQuantity` exists and is always 0; the shortage formula already accounts for it |
| DB-TBD-04 | Multi-currency and FX rate storage | TBD-21 | **Owner decision:** single currency per tenant. No FX table in V1 |
| DB-TBD-05 | In-transit virtual warehouse for transfers | TBD-22 | **Owner decision:** V1 transfer is instantaneous (one transaction, both warehouses) |
| DB-TBD-06 | Audit retention and archive destination | TBD-14 | **Owner decision:** provisional 24 months online, then cold archive |
| DB-TBD-07 | Barcode symbology set | TBD-18 | **Design decision (ADR-0043):** the six values the enum models |
| DB-TBD-08 | Full-text search engine choice | TBD-17 | **Design decision (ADR-0042):** SQL Server full text + trigram fallback |
| DB-TBD-09 | Over-receipt tolerance representation | TBD-08 | **Owner decision:** V1 default 0 %, `PurchaseOrder.AllowOverReceipt` + a per-tenant percentage. No schema change needed either way |
| DB-TBD-10 | Approval threshold persistence | TBD-09 | **Owner decision:** per-tenant configuration, not code. A dedicated config table is additive |

**No `DB-TBD` item changes the 66-entity V1 schema.** Every deferred option is
additive (a new nullable column, a new table, a new config row), so a late owner
answer never requires rewriting the V1 design.
