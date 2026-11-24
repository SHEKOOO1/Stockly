using Stockly.Domain.Common;
using Stockly.Domain.Modules.Platform;

namespace Stockly.Domain.Modules.Identity;

/// <summary>
/// identity.Tenant (§4.2). One row is one legal/business entity
/// (TBD-24 / ADR-0048): no <c>ParentTenantId</c>, no <c>LegalEntityId</c>, no
/// subdivision. <c>Code</c> is globally unique because it appears in URLs.
/// </summary>
public class Tenant : SoftDeleteEntity
{
    /// <summary>Globally unique — used in URLs.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    /// <summary>nvarchar(300) NULL.</summary>
    public string? LegalName { get; set; }

    public TenantStatus Status { get; set; }

    /// <summary>Windows time-zone id. Presentation only; all stored timestamps are UTC (ADR-0029).</summary>
    public string TimeZoneId { get; set; } = "UTC";

    public string DefaultCulture { get; set; } = "ar";

    public string DateFormat { get; set; } = "yyyy-MM-dd";
}

/// <summary>
/// identity.TenantSetting (§4.2). Exactly one row per tenant
/// (<c>UNIQUE (TenantId)</c>), which is what makes "one currency per tenant"
/// true by construction rather than by convention (TBD-21 / ADR-0046). There is
/// no FX rate table and no conversion path anywhere in V1.
/// </summary>
public class TenantSetting : TenantScopedEntity
{
    /// <summary>char(3), ISO-4217. The tenant's single and only currency.</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>0–4, mirroring the decimal(18,4) storage precision.</summary>
    public int NumberOfDecimalPlaces { get; set; }

    public Guid? DefaultWarehouseId { get; set; }

    public string LowStockPolicy { get; set; } = string.Empty;

    public int ExpiryWarningDays { get; set; }

    public int DefaultSessionIdleMinutes { get; set; }

    public int DefaultSessionAbsoluteHours { get; set; }

    /// <summary>CHECK &gt;= 6.</summary>
    public int PinLength { get; set; } = 6;

    public bool RequireReasonCodeForAdjustments { get; set; }

    public bool RequireReasonCodeForWaste { get; set; }

    /// <summary>
    /// A kill switch, not a feature. It exists so an operator can prove it is
    /// never enabled; a test asserts it is 0 in every environment.
    /// </summary>
    public bool AllowNegativeStock { get; set; }
}

/// <summary>
/// identity.TenantMembership (§4.2). UQ (filtered, active only):
/// <c>(TenantId, UserId) WHERE Status &lt;&gt; 3</c> — a suspended membership is
/// preserved for audit and can be reactivated, but two active memberships for
/// the same user in the same tenant are impossible.
/// </summary>
public class TenantMembership : TenantScopedEntity
{
    public Guid UserId { get; set; }

    public MembershipStatus Status { get; set; }

    /// <summary>Marks the tenant owner. The last active owner cannot be removed.</summary>
    public bool IsOwner { get; set; }

    public string? EmployeeNumber { get; set; }

    public string? JobTitle { get; set; }

    public string? PreferredCulture { get; set; }

    public DateTime JoinedAtUtc { get; set; }

    public AppUser User { get; set; } = null!;

    public ICollection<MembershipRole> Roles { get; set; } = [];

    public ICollection<MembershipWarehouse> Warehouses { get; set; } = [];
}

/// <summary>
/// identity.Permission (§4.2). Globally seeded and read-only at runtime
/// (ADR-0012). Codes in <c>module.resource.action</c> dot-notation are
/// permanent API contract. <c>IsDangerous = 1</c> for <c>platform.*</c>,
/// <c>tenant.roles.manage</c>, <c>security.*</c> and <c>tenant.audit.*</c>.
/// </summary>
public class Permission : Entity, IGlobalEntity, IHasRowVersion
{
    public string Code { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    /// <summary>English label for the admin UI.</summary>
    public string Name { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public string? Description { get; set; }

    public bool IsDangerous { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

/// <summary>
/// identity.Role (§4.2). UQ (filtered): <c>(TenantId, Code) WHERE IsDeleted = 0</c>.
/// <c>AuthorityLevel</c> is the database-visible privilege-escalation guard
/// (DATABASE_DESIGN.md §6).
/// </summary>
public class Role : TenantScopedSoftDeleteEntity
{
    /// <summary>e.g. <c>store_keeper</c>.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public string? Description { get; set; }

    public RoleType RoleType { get; set; }

    /// <summary>1–100.</summary>
    public short AuthorityLevel { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

/// <summary>
/// identity.RolePermission (§4.2). UQ: <c>(RoleId, PermissionId)</c>.
/// <para>
/// No <c>RowVersion</c>: the design specifies only <c>CreatedAtUtc</c>, and this
/// is a grant row rather than a mutable business record.
/// </para>
/// </summary>
public class RolePermission : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }

    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Permission Permission { get; set; } = null!;

    public Role Role { get; set; } = null!;
}

/// <summary>
/// identity.MembershipRole (§4.2). UQ: <c>(MembershipId, RoleId)</c>.
/// Role and warehouse scope are separate axes; <see cref="ScopeType"/> says
/// which warehouses the grant covers.
/// </summary>
public class MembershipRole : TenantScopedEntity
{
    public Guid MembershipId { get; set; }

    public Guid RoleId { get; set; }

    public MembershipRoleScopeType ScopeType { get; set; }

    public TenantMembership Membership { get; set; } = null!;

    public Role Role { get; set; } = null!;

    public ICollection<MembershipRoleWarehouse> ScopeWarehouses { get; set; } = [];
}

/// <summary>
/// identity.MembershipRoleWarehouse (§4.2). UQ: <c>(MembershipRoleId, WarehouseId)</c>.
/// The junction that makes "role in warehouse X" expressible.
/// </summary>
public class MembershipRoleWarehouse : TenantScopedEntity
{
    public Guid MembershipRoleId { get; set; }

    public Guid WarehouseId { get; set; }

    public MembershipRole MembershipRole { get; set; } = null!;
}

/// <summary>
/// identity.Warehouse (§4.2). UQ (filtered): <c>(TenantId, Code) WHERE IsDeleted = 0</c>.
/// <para>
/// A warehouse is the V1 and V1.1 stock scope and has <strong>no parent key</strong>:
/// no <c>ParentWarehouseId</c>, no <c>LocationId</c>, no zone/bin/aisle column
/// (TBD-19 / ADR-0045). There is no virtual or in-transit warehouse: transfers
/// are instantaneous (TBD-22 / ADR-0047), so stock is never unaccounted for.
/// </para>
/// </summary>
public class Warehouse : TenantScopedSoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public WarehouseType Type { get; set; }

    public string? Address { get; set; }

    public bool IsDefaultReceipt { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<MembershipWarehouse> Assignments { get; set; } = [];

    public ICollection<Device> Devices { get; set; } = [];
}

/// <summary>
/// identity.MembershipWarehouse (§4.2) — the "WHERE" axis of authorization.
/// UQ: <c>(TenantId, MembershipId, WarehouseId)</c>.
/// </summary>
public class MembershipWarehouse : TenantScopedEntity
{
    public Guid MembershipId { get; set; }

    public Guid WarehouseId { get; set; }

    public bool CanReceive { get; set; } = true;

    public bool CanIssue { get; set; } = true;

    /// <summary>Default warehouse for this user.</summary>
    public bool IsDefault { get; set; }

    public DateTime AssignedAtUtc { get; set; }

    public TenantMembership Membership { get; set; } = null!;

    public Warehouse Warehouse { get; set; } = null!;
}

/// <summary>
/// identity.Device (§4.2). A device belongs to <strong>exactly one</strong>
/// warehouse. UQ (filtered) <c>(TenantId, DeviceCode)</c>; UQ
/// <c>UX_Device_EnrollmentCode</c> is <em>global</em>, because an enrollment code
/// must resolve to exactly one device anywhere on the platform (ADR-0032).
/// </summary>
public class Device : TenantScopedSoftDeleteEntity
{
    public Guid WarehouseId { get; set; }

    /// <summary>e.g. <c>KITCHEN-01</c>. A human label, <strong>not</strong> a secret.</summary>
    public string DeviceCode { get; set; } = string.Empty;

    /// <summary>
    /// varchar(64). 128-bit CSPRNG, base32url, rotatable, globally unique
    /// (ADR-0035). Stored in clear because it is a lookup key, not an
    /// authentication secret; HMAC'd in the throttle ledger. Never displayed.
    /// </summary>
    public string EnrollmentCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DeviceType Type { get; set; }

    public DeviceStatus Status { get; set; }

    public bool IsSharedTerminal { get; set; } = true;

    public DateTime? LastSeenAtUtc { get; set; }

    public DateTime RegisteredAtUtc { get; set; }

    public Guid? RegisteredByMembershipId { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? Notes { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
}

/// <summary>
/// identity.AuthSession (§4.2). The server-side session row that every
/// authenticated request re-checks, which is what makes revocation immediate
/// rather than token-expiry-delayed.
/// <para>
/// <c>TenantId</c> and <c>MembershipId</c> are NULL exactly when
/// <see cref="SessionScope"/> is <see cref="Common.SessionScope.Platform"/>. That
/// is an absolute invariant and is enforced by a database CHECK, not only by
/// application code (ADR-0034).
/// </para>
/// </summary>
public class AuthSession : Entity, IHasRowVersion
{
    /// <summary>NULL only for a Platform session.</summary>
    public Guid? TenantId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>NULL only for a Platform session.</summary>
    public Guid? MembershipId { get; set; }

    public SessionScope SessionScope { get; set; }

    /// <summary>NULL for password sessions on personal devices and for platform sessions.</summary>
    public Guid? DeviceId { get; set; }

    public SessionStatus Status { get; set; }

    public AuthenticationMethod AuthenticationMethod { get; set; }

    public Guid? PinCredentialId { get; set; }

    /// <summary>Snapshot; a bump invalidates the session.</summary>
    public int? PinVersion { get; set; }

    /// <summary>Groups every refresh token descended from one login, so reuse can revoke the whole family.</summary>
    public Guid RefreshFamilyId { get; set; }

    public DateTime IssuedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime AbsoluteExpiresAtUtc { get; set; }

    public DateTime LastActivityAtUtc { get; set; }

    /// <summary>Throttled write (≥60s) to avoid a database write per request.</summary>
    public DateTime? LastSeenAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? RevokedReason { get; set; }

    public Guid? RevokedByMembershipId { get; set; }

    /// <summary>nvarChar(45) — IPv6-safe.</summary>
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? CorrelationId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

/// <summary>
/// identity.RefreshToken (§4.2). UQ: <c>UX_RefreshToken_TokenHash</c>.
/// <strong>The token value itself is never stored.</strong>
/// </summary>
public class RefreshToken : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }

    public Guid SessionId { get; set; }

    public Guid FamilyId { get; set; }

    /// <summary>SHA-256 of the opaque token.</summary>
    public byte[] TokenHash { get; set; } = [];

    public RefreshTokenStatus Status { get; set; }

    public DateTime IssuedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }

    public string? CreatedByIpAddress { get; set; }
}

/// <summary>
/// identity.PinCredential (§4.2). UQ <c>(MembershipId)</c> — one PIN credential
/// per membership, unfiltered. Bumping <see cref="Version"/> invalidates every
/// session authenticated by the PIN.
/// </summary>
public class PinCredential : TenantScopedEntity
{
    public Guid MembershipId { get; set; }

    /// <summary>Memory-hard KDF hash. Never returned by any API, never logged, never in an audit payload.</summary>
    public string PinHash { get; set; } = string.Empty;

    public int Version { get; set; } = 1;

    /// <summary>True on administrative reset; the terminal answers 428 until the PIN is changed.</summary>
    public bool MustChangePin { get; set; } = true;

    public int FailedAttemptCount { get; set; }

    public DateTime? LockoutEndAtUtc { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    public DateTime SetAtUtc { get; set; }

    public Guid? SetByMembershipId { get; set; }
}

/// <summary>
/// identity.LoginAttempt (§4.2). The global throttle ledger.
/// <para>
/// It carries no plaintext identifier: the identifier is stored as
/// <c>HMAC-SHA256(identifier, serverPepper)</c> (ADR-0036), because a plain hash of
/// an email address is reversible by dictionary attack and the email list is not
/// secret. The pepper lives in the secret store, never in the database.
/// </para>
/// </summary>
public class LoginAttempt : IGlobalEntity
{
    /// <summary>bigint identity — high volume, append-ordered.</summary>
    public long Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public LoginAttemptMethod Method { get; set; }

    public LoginAttemptOutcome Outcome { get; set; }

    public byte[] IdentifierHash { get; set; } = [];

    /// <summary>NULL when the account does not exist.</summary>
    public Guid? UserId { get; set; }

    public Guid? DeviceId { get; set; }

    /// <summary>HMAC of the enrollment code, so a leaked table cannot enumerate valid terminals.</summary>
    public byte[]? EnrollmentCodeHash { get; set; }

    public string? IpAddress { get; set; }

    public bool IsSuccess { get; set; }
}

/// <summary>
/// identity.PasswordResetToken (§4.2). <c>TokenHash</c> is SHA-256 of a 256-bit
/// random value; a pepper is unnecessary because a random token has no
/// dictionary. On use: revoke all sessions for the user, set
/// <c>MustChangePassword = 1</c>, audit.
/// </summary>
public class PasswordResetToken : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }

    public Guid UserId { get; set; }

    public byte[] TokenHash { get; set; } = [];

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }

    public string? CreatedByIpAddress { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}