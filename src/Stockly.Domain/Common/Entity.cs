namespace Stockly.Domain.Common;

/// <summary>
/// Marker for a tenant-scoped entity (DATABASE_DESIGN.md §3). Every
/// tenant-scoped table carries a non-null <c>TenantId</c>, every unique index
/// leads with it (ADR-0032), and children reference parents through a composite
/// <c>(Id, TenantId)</c> foreign key so a cross-tenant row is structurally
/// impossible to insert (DATABASE_DESIGN.md §5.1).
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; }
}

/// <summary>
/// Marker for an entity that is deliberately global. The V1 schema has exactly
/// <c>platform.AppUser</c> (ADR-0003), <c>platform.Plan</c>,
/// <c>platform.PlanFeature</c>, <c>identity.Permission</c> and
/// <c>identity.LoginAttempt</c>.
/// </summary>
public interface IGlobalEntity
{
}

/// <summary>
/// Exposes the <c>rowversion</c> concurrency token. Mutable operational tables
/// have one and expose it as an HTTP <c>ETag</c>; append-only tables
/// deliberately do not (ADR-0037).
/// </summary>
public interface IHasRowVersion
{
    byte[] RowVersion { get; }
}

/// <summary>Soft-delete markers. Only master/config tables are soft deleted (DATABASE_DESIGN.md §1.8).</summary>
public interface ISoftDelete
{
    bool IsDeleted { get; }

    DateTime? DeletedAtUtc { get; }

    Guid? DeletedByMembershipId { get; }
}

/// <summary>Standard audit stamps (DATABASE_DESIGN.md §3).</summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; }

    DateTime? UpdatedAtUtc { get; }

    Guid? CreatedByMembershipId { get; }
}

/// <summary>
/// Base for every persisted entity. The primary key is client-generated to
/// avoid key hotspots and to support offline-created records; only
/// <c>audit.AuditLog</c> and <c>identity.LoginAttempt</c> use a
/// database-ordered identity (DATABASE_DESIGN.md §3).
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

/// <summary>Base for a tenant-scoped mutable entity carrying the concurrency token.</summary>
public abstract class TenantScopedEntity : Entity, ITenantScoped, IHasRowVersion
{
    public Guid TenantId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

/// <summary>
/// Base for a soft-deleted entity that is NOT itself tenant-scoped.
/// <para>
/// This exists for exactly one entity: <c>identity.Tenant</c>, the tenant root. The
/// ratified column list for <c>identity.Tenant</c> has no <c>TenantId</c>
/// (DATABASE_DESIGN §4.2), and inheriting a tenant-scoped base would have added
/// one. That matters beyond schema tidiness: the global tenant query filter is
/// applied by <c>ITenantScoped</c>, so a root row carrying an unrelated
/// <c>TenantId</c> would be filtered out of its own tenant's session and the tenant
/// would appear not to exist.
/// </para>
/// </summary>
public abstract class SoftDeleteEntity : Entity, ISoftDelete, IHasRowVersion
{
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public Guid? DeletedByMembershipId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

/// <summary>Tenant-scoped mutable entity that additionally soft-deletes.</summary>
public abstract class TenantScopedSoftDeleteEntity : SoftDeleteEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
}

/// <summary>Tenant-scoped mutable entity carrying the standard audit stamps.</summary>
public abstract class AuditableTenantScopedEntity : TenantScopedEntity, IAuditable
{
    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public Guid? CreatedByMembershipId { get; set; }
}

/// <summary>Tenant-scoped mutable entity that is both audited and soft-deleted.</summary>
public abstract class AuditableSoftDeleteTenantScopedEntity : TenantScopedSoftDeleteEntity, IAuditable
{
    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public Guid? CreatedByMembershipId { get; set; }
}

/// <summary>
/// Base for a tenant-scoped mutable entity that the design deliberately gives
/// <strong>no</strong> <c>RowVersion</c>. The column list of
/// <c>notifications.Notification</c>, <c>notifications.PushSubscription</c>,
/// <c>identity.RefreshToken</c>, <c>identity.PasswordResetToken</c> and
/// <c>identity.RolePermission</c> omits the token deliberately, and inventing one
/// would contradict the ratified column-level design.
/// </summary>
public abstract class TenantRecordEntity : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
}

/// <summary>
/// Base for an append-only tenant-scoped entity (ADR-0037).
/// <para>
/// Append-only means <strong>no <c>RowVersion</c></strong>: a concurrency token
/// on a table that must never be updated is a contradiction and would break the
/// <c>INSERT</c>-only database principal granted on these tables. There is no
/// update or delete path either.
/// </para>
/// </summary>
public abstract class AppendOnlyTenantScopedEntity : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
