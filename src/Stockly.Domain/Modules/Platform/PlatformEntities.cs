using Stockly.Domain.Common;
using Stockly.Domain.Modules.Identity;

namespace Stockly.Domain.Modules.Platform;

/// <summary>
/// platform.AppUser (DATABASE_DESIGN.md §4.1). Global identity (ADR-0003):
/// deliberately no <c>TenantId</c> — one person has one identity, and access to
/// an organization is granted by <c>TenantMembership</c>.
/// </summary>
public class AppUser : Entity, IGlobalEntity, IHasRowVersion
{
    /// <summary>nvarChar(320). Normalised lowercase. Globally unique.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>nvarChar(100). Normalised lowercase. Filtered-unique when present.</summary>
    public string? UserName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>nvarChar(30). E.164.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>NULL means no password credential (e.g. a PIN-only terminal user). Never returned or logged.</summary>
    public string? PasswordHash { get; set; }

    public DateTime? PasswordSetAtUtc { get; set; }

    public bool MustChangePassword { get; set; }

    /// <summary>Grants the whole <c>platform.*</c> set in V1 (ADR-0034). Never appears in a tenant token.</summary>
    public bool IsPlatformAdmin { get; set; }

    public bool IsActive { get; set; } = true;

    public int FailedLoginCount { get; set; }

    public DateTime? LockoutEndAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>Reserved for V2. Not implemented in V1.</summary>
    public bool TwoFactorEnabled { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<TenantMembership> Memberships { get; set; } = [];
}

/// <summary>platform.Plan (§4.1). Global commercial catalogue.</summary>
public class Plan : Entity, IGlobalEntity, IHasRowVersion
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public string? Description { get; set; }

    public decimal MonthlyPrice { get; set; }

    /// <summary>char(3), ISO-4217.</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    public int BillingPeriodDays { get; set; }

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<PlanFeature> Features { get; set; } = [];
}

/// <summary>platform.PlanFeature (§4.1). UQ: <c>(PlanId, FeatureKey)</c>.</summary>
public class PlanFeature : Entity, IGlobalEntity, IHasRowVersion
{
    public Guid PlanId { get; set; }

    /// <summary>e.g. <c>inventory.batch_tracking</c>, <c>custom_fields</c>.</summary>
    public string FeatureKey { get; set; } = string.Empty;

    public LimitType LimitType { get; set; }

    /// <summary>NULL = unlimited when <see cref="LimitType"/> is not <see cref="Common.LimitType.None"/>.</summary>
    public decimal? LimitValue { get; set; }

    public bool IsEnabled { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Plan Plan { get; set; } = null!;
}

/// <summary>platform.Subscription (§4.1).</summary>
public class Subscription : TenantScopedEntity
{
    public Guid PlanId { get; set; }

    public SubscriptionStatus Status { get; set; }

    public DateTime StartAtUtc { get; set; }

    /// <summary>NULL = open-ended.</summary>
    public DateTime? EndAtUtc { get; set; }

    /// <summary>Provisional policy value (TBD-12), per-subscription.</summary>
    public int GracePeriodDays { get; set; } = 7;

    public DateTime? CancelledAtUtc { get; set; }

    /// <summary>Reserved for a future billing provider (TBD-13).</summary>
    public string? ExternalReference { get; set; }

    public Plan Plan { get; set; } = null!;
}

/// <summary>platform.TenantEntitlement (§4.1). Per-tenant override of a plan feature limit. UQ: <c>(TenantId, FeatureKey)</c>.</summary>
public class TenantEntitlement : AuditableTenantScopedEntity
{
    public string FeatureKey { get; set; } = string.Empty;

    public LimitType LimitType { get; set; }

    public decimal? LimitValue { get; set; }

    public bool IsEnabled { get; set; }

    public string? Reason { get; set; }

    public Guid? GrantedByMembershipId { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}

/// <summary>platform.SubscriptionUsage (§4.1). UQ: <c>(TenantId, PeriodStartUtc, FeatureKey)</c>.</summary>
public class SubscriptionUsage : TenantScopedEntity
{
    public DateTime PeriodStartUtc { get; set; }

    public DateTime PeriodEndUtc { get; set; }

    public string FeatureKey { get; set; } = string.Empty;

    public decimal CurrentValue { get; set; }

    /// <summary>Snapshot of the limit at period start.</summary>
    public decimal? LimitValue { get; set; }
}
