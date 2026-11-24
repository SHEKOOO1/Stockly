using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockly.Domain.Modules.Identity;
using Stockly.Domain.Modules.Platform;

namespace Stockly.Infrastructure.Persistence.Configurations;

// ---------------------------------------------------------------------------
// Shared conventions. Applied in every configuration so the invariants of
// DATABASE_DESIGN.md §1, §3 and §5 are stated once.
// ---------------------------------------------------------------------------

internal static class Convention
{
    /// <summary>nvarChar(200) — the canonical `Name` width (§3).</summary>
    public const int NameLength = 200;

    /// <summary>nvarChar(50) — the canonical `Code` width, uppercase-normalised (§3).</summary>
    public const int CodeLength = 50;

    /// <summary>nvarChar(1000) — the canonical notes width (§3).</summary>
    public const int NotesLength = 1000;

    /// <summary>nvarChar(45) — IPv6-safe address length.</summary>
    public const int IpLength = 45;

    /// <summary>decimal(18,4) for money and quantity (ADR-0024, §1.5).</summary>
    public const string Money = "decimal(18,4)";

    /// <summary>decimal(18,8) — the single documented exception: a conversion ratio (§1.5).</summary>
    public const string Ratio = "decimal(18,8)";

    /// <summary>decimal(5,2) for a percentage or a servings ratio.</summary>
    public const string Percent = "decimal(5,2)";

    /// <summary>decimal(3,1) for whole/half meals per day.</summary>
    public const string MealsPerDay = "decimal(3,1)";

    /// <summary>decimal(9,2) for a servings-per-yield ratio.</summary>
    public const string ServingsRatio = "decimal(9,2)";

    public static PropertyBuilder Money_(this PropertyBuilder builder) =>
        builder.HasColumnType(Money);

    public static PropertyBuilder Ratio_(this PropertyBuilder builder) =>
        builder.HasColumnType(Ratio);

    public static PropertyBuilder Percent_(this PropertyBuilder builder) =>
        builder.HasColumnType(Percent);

    /// <summary>Smallint enum column with a CHECK pinning the legal values (§1.9).</summary>
    public static PropertyBuilder Enum_(this PropertyBuilder builder) =>
        builder.HasConversion<int>().HasColumnType("smallint");
}

// ---------------------------------------------------------------------------
// platform
// ---------------------------------------------------------------------------

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("AppUser");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasMaxLength(320).IsRequired();
        b.Property(x => x.UserName).HasMaxLength(100);
        b.Property(x => x.DisplayName).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.PhoneNumber).HasMaxLength(30);
        b.Property(x => x.PasswordHash).HasMaxLength(400);
        b.Property(x => x.MustChangePassword).IsRequired();
        b.Property(x => x.IsPlatformAdmin).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.FailedLoginCount).IsRequired();
        b.Property(x => x.TwoFactorEnabled).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UX_AppUser_Email");
        b.HasIndex(x => x.UserName).IsUnique().HasFilter("[UserName] IS NOT NULL").HasDatabaseName("UX_AppUser_UserName");
        b.HasIndex(x => x.LockoutEndAtUtc).HasDatabaseName("IX_AppUser_LockoutEndAtUtc");
    }
}

internal sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> b)
    {
        b.ToTable("Plan");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.Description).HasMaxLength(Convention.NameLength);
        b.Property(x => x.MonthlyPrice).Money_().IsRequired();
        b.Property(x => x.CurrencyCode).HasColumnType("char(3)").IsFixedLength().IsRequired();
        b.Property(x => x.BillingPeriodDays).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.SortOrder).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_Plan_Code");
    }
}

internal sealed class PlanFeatureConfiguration : IEntityTypeConfiguration<PlanFeature>
{
    public void Configure(EntityTypeBuilder<PlanFeature> b)
    {
        b.ToTable("PlanFeature");
        b.HasKey(x => x.Id);
        b.Property(x => x.FeatureKey).HasMaxLength(100).IsRequired();
        b.Property(x => x.LimitType).Enum_().IsRequired();
        b.Property(x => x.LimitValue).Money_();
        b.Property(x => x.IsEnabled).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.PlanId, x.FeatureKey }).IsUnique().HasDatabaseName("UX_PlanFeature_PlanId_FeatureKey");
    }
}

internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("Subscription");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).Enum_().IsRequired();
        b.Property(x => x.GracePeriodDays).IsRequired();
        b.Property(x => x.ExternalReference).HasMaxLength(200);
        b.Property(x => x.RowVersion).IsRowVersion();

        // Subscription.TenantId is a plain FK to Tenant.Id (a root table has no composite
        // key of its own); every *tenant-scoped parent* reference below is composite
        // on (Id, TenantId) so a cross-tenant row cannot be written at all.
        // TenantId -> identity.Tenant.Id. The only single-column tenant reference:
        // the root has no TenantId of its own, so a composite key is impossible.
        b.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Subscription_Tenant");
        b.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId);

        b.HasIndex(x => new { x.TenantId, x.Status }).HasDatabaseName("IX_Subscription_Tenant_Status");
        b.HasIndex(x => x.EndAtUtc).HasDatabaseName("IX_Subscription_EndAtUtc");
    }
}

internal sealed class TenantEntitlementConfiguration : IEntityTypeConfiguration<TenantEntitlement>
{
    public void Configure(EntityTypeBuilder<TenantEntitlement> b)
    {
        b.ToTable("TenantEntitlement");
        b.HasKey(x => x.Id);
        b.Property(x => x.FeatureKey).HasMaxLength(100).IsRequired();
        b.Property(x => x.LimitType).Enum_().IsRequired();
        b.Property(x => x.LimitValue).Money_();
        b.Property(x => x.IsEnabled).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(500);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.FeatureKey }).IsUnique().HasDatabaseName("UX_TenantEntitlement_TenantId_FeatureKey");
    }
}

internal sealed class SubscriptionUsageConfiguration : IEntityTypeConfiguration<SubscriptionUsage>
{
    public void Configure(EntityTypeBuilder<SubscriptionUsage> b)
    {
        b.ToTable("SubscriptionUsage");
        b.HasKey(x => x.Id);
        b.Property(x => x.FeatureKey).HasMaxLength(100).IsRequired();
        b.Property(x => x.CurrentValue).Money_().IsRequired();
        b.Property(x => x.LimitValue).Money_();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.TenantId, x.PeriodStartUtc, x.FeatureKey })
            .IsUnique()
            .HasDatabaseName("UX_SubscriptionUsage_TenantId_PeriodStartUtc_FeatureKey");
    }
}