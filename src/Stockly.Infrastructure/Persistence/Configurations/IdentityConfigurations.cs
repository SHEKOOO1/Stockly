using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stockly.Domain.Modules.Identity;
using Stockly.Domain.Modules.Platform;

namespace Stockly.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("Tenant");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.LegalName).HasMaxLength(300);
        b.Property(x => x.Status).Enum_().IsRequired();
        b.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
        b.Property(x => x.DefaultCulture).HasMaxLength(10).IsRequired();
        b.Property(x => x.DateFormat).HasMaxLength(30).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        // Globally unique, not per tenant: Tenant is the tenant root and its Code is
        // used in URLs, so two tenants sharing a code would be unroutable.
        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_Tenant_Code");

        b.ToTable("Tenant", t =>
            t.HasCheckConstraint("CK_Tenant_Status", "[Status] IN (1,2,3)"));
    }
}

internal sealed class TenantSettingConfiguration : IEntityTypeConfiguration<TenantSetting>
{
    public void Configure(EntityTypeBuilder<TenantSetting> b)
    {
        b.ToTable("TenantSetting");
        b.HasKey(x => x.Id);
        b.Property(x => x.CurrencyCode).HasColumnType("char(3)").IsFixedLength().IsRequired();
        b.Property(x => x.NumberOfDecimalPlaces).IsRequired();
        b.Property(x => x.LowStockPolicy).HasMaxLength(30).IsRequired();
        b.Property(x => x.ExpiryWarningDays).IsRequired();
        b.Property(x => x.DefaultSessionIdleMinutes).IsRequired();
        b.Property(x => x.DefaultSessionAbsoluteHours).IsRequired();
        b.Property(x => x.PinLength).IsRequired();
        b.Property(x => x.RequireReasonCodeForAdjustments).IsRequired();
        b.Property(x => x.RequireReasonCodeForWaste).IsRequired();
        b.Property(x => x.AllowNegativeStock).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        // UNIQUE (TenantId) is what makes "one settings row, therefore one currency,
        // per tenant" a structural fact rather than a convention (TBD-21 / ADR-0046).
        b.HasIndex(x => x.TenantId).IsUnique().HasDatabaseName("UX_TenantSetting_TenantId");

        b.ToTable("TenantSetting", t =>
        {
            t.HasCheckConstraint("CK_TenantSetting_NumberOfDecimalPlaces", "[NumberOfDecimalPlaces] BETWEEN 0 AND 4");
            t.HasCheckConstraint("CK_TenantSetting_PinLength", "[PinLength] >= 6");
            // A kill switch, not a feature. Pinned to 0 in the database so no
            // deployment mistake can turn negative stock on (DATABASE_DESIGN §4.2).
            t.HasCheckConstraint("CK_TenantSetting_AllowNegativeStock", "[AllowNegativeStock] = 0");
        });
    }
}

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> b)
    {
        b.ToTable("TenantMembership");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).Enum_().IsRequired();
        b.Property(x => x.IsOwner).IsRequired();
        b.Property(x => x.EmployeeNumber).HasMaxLength(50);
        b.Property(x => x.JobTitle).HasMaxLength(100);
        b.Property(x => x.PreferredCulture).HasMaxLength(10);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.User)
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_TenantMembership_AppUser");

        // A suspended membership is preserved for audit and can be reactivated, so the
        // uniqueness is filtered to active rows rather than covering the whole table.
        b.HasIndex(x => new { x.TenantId, x.UserId })
            .IsUnique()
            .HasFilter("[Status] <> 3")
            .HasDatabaseName("UX_TenantMembership_TenantId_UserId_Active");

        b.HasIndex(x => x.UserId).HasDatabaseName("IX_TenantMembership_User");

        b.ToTable("TenantMembership", t =>
            t.HasCheckConstraint("CK_TenantMembership_Status", "[Status] IN (1,2,3)"));
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("Permission");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Module).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.Description).HasMaxLength(Convention.NameLength);
        b.Property(x => x.IsDangerous).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_Permission_Code");
        b.HasIndex(x => x.Module).HasDatabaseName("IX_Permission_Module");
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Role");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.Description).HasMaxLength(Convention.NameLength);
        b.Property(x => x.RoleType).Enum_().IsRequired();
        b.Property(x => x.AuthorityLevel).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Role_TenantId_Code");

        b.ToTable("Role", t =>
        {
            t.HasCheckConstraint("CK_Role_RoleType", "[RoleType] IN (1,2)");
            t.HasCheckConstraint("CK_Role_AuthorityLevel", "[AuthorityLevel] BETWEEN 1 AND 100");
        });
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("RolePermission");
        b.HasKey(x => x.Id);

        b.HasOne(x => x.Permission)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .HasConstraintName("FK_RolePermission_Permission");

        b.HasOne(x => x.Role)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => new { x.RoleId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_RolePermission_Role");

        b.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique().HasDatabaseName("UX_RolePermission_RoleId_PermissionId");
        b.HasIndex(x => new { x.TenantId, x.PermissionId }).HasDatabaseName("IX_RolePermission_TenantId_PermissionId");
    }
}

internal sealed class MembershipRoleConfiguration : IEntityTypeConfiguration<MembershipRole>
{
    public void Configure(EntityTypeBuilder<MembershipRole> b)
    {
        b.ToTable("MembershipRole");
        b.HasKey(x => x.Id);
        b.Property(x => x.ScopeType).Enum_().IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Membership)
            .WithMany(x => x.Roles)
            .HasForeignKey(x => new { x.MembershipId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_MembershipRole_TenantMembership");

        b.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => new { x.RoleId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_MembershipRole_Role");

        b.HasIndex(x => new { x.MembershipId, x.RoleId }).IsUnique().HasDatabaseName("UX_MembershipRole_MembershipId_RoleId");

        b.ToTable("MembershipRole", t =>
            t.HasCheckConstraint("CK_MembershipRole_ScopeType", "[ScopeType] IN (1,2)"));
    }
}

internal sealed class MembershipRoleWarehouseConfiguration : IEntityTypeConfiguration<MembershipRoleWarehouse>
{
    public void Configure(EntityTypeBuilder<MembershipRoleWarehouse> b)
    {
        b.ToTable("MembershipRoleWarehouse");
        b.HasKey(x => x.Id);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.MembershipRole)
            .WithMany(x => x.ScopeWarehouses)
            .HasForeignKey(x => new { x.MembershipRoleId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_MembershipRoleWarehouse_MembershipRole");

        b.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_MembershipRoleWarehouse_Warehouse");

        b.HasIndex(x => new { x.MembershipRoleId, x.WarehouseId })
            .IsUnique()
            .HasDatabaseName("UX_MembershipRoleWarehouse_MembershipRoleId_WarehouseId");
    }
}

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        b.ToTable("Warehouse");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(Convention.CodeLength).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(Convention.NameLength);
        b.Property(x => x.Type).Enum_().IsRequired();
        b.Property(x => x.Address).HasMaxLength(500);
        b.Property(x => x.IsDefaultReceipt).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Warehouse_TenantId_Code");

        b.ToTable("Warehouse", t =>
            t.HasCheckConstraint("CK_Warehouse_Type", "[Type] IN (1,2,3,4,5,6)"));
    }
}

internal sealed class MembershipWarehouseConfiguration : IEntityTypeConfiguration<MembershipWarehouse>
{
    public void Configure(EntityTypeBuilder<MembershipWarehouse> b)
    {
        b.ToTable("MembershipWarehouse");
        b.HasKey(x => x.Id);
        b.Property(x => x.CanReceive).IsRequired();
        b.Property(x => x.CanIssue).IsRequired();
        b.Property(x => x.IsDefault).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.Membership)
            .WithMany(x => x.Warehouses)
            .HasForeignKey(x => new { x.MembershipId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_MembershipWarehouse_TenantMembership");

        b.HasOne(x => x.Warehouse)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_MembershipWarehouse_Warehouse");

        b.HasIndex(x => new { x.TenantId, x.MembershipId, x.WarehouseId })
            .IsUnique()
            .HasDatabaseName("UX_MembershipWarehouse_TenantId_MembershipId_WarehouseId");
        b.HasIndex(x => new { x.TenantId, x.WarehouseId }).HasDatabaseName("IX_MembershipWarehouse_TenantId_WarehouseId");
    }
}

internal sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> b)
    {
        b.ToTable("Device");
        b.HasKey(x => x.Id);
        b.Property(x => x.DeviceCode).HasMaxLength(Convention.CodeLength).IsRequired();

        // DATABASE_DESIGN §6 (identity.Device) declares `varchar(64)`, and both
        // attributes matter:
        //   * ANSI, not Unicode — a base32url enrollment code is ASCII by
        //     construction (ADR-0035), so nvarchar would double the storage of a
        //     value that can never contain a character outside ASCII, and would
        //     invite a caller to "helpfully" store non-ASCII in it.
        //   * variable-length, not fixed-length — a base32url encoding of 128 bits
        //     is 86 characters, not 64. Fixed-length `char(64)` would pad the value
        //     to 64 and silently truncate a legitimate code, which would break
        //     enrollment-code resolution for exactly the terminals that matter. The
        //     design's fixed-length `char(3)` on Tenant.CurrencyCode is the
        //     contrasting case where padding IS intended.
        b.Property(x => x.EnrollmentCode).HasMaxLength(64).IsUnicode(false).IsFixedLength(false).IsRequired();
        b.Property(x => x.Name).HasMaxLength(Convention.NameLength).IsRequired();
        b.Property(x => x.Type).Enum_().IsRequired();
        b.Property(x => x.Status).Enum_().IsRequired();
        b.Property(x => x.IsSharedTerminal).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(Convention.NotesLength);
        b.Property(x => x.RowVersion).IsRowVersion();

        // A device belongs to exactly one warehouse (ADR-0006).
        b.HasOne(x => x.Warehouse)
            .WithMany(x => x.Devices)
            .HasForeignKey(x => new { x.WarehouseId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .HasConstraintName("FK_Device_Warehouse");

        b.HasIndex(x => new { x.TenantId, x.DeviceCode })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Device_TenantId_DeviceCode");

        // Deliberately GLOBAL (ADR-0032): an enrollment code presented at login must
        // resolve to exactly one device anywhere on the platform, because the
        // enrollment code is the only credential the terminal sends before a
        // membership is known.
        b.HasIndex(x => x.EnrollmentCode).IsUnique().HasDatabaseName("UX_Device_EnrollmentCode");

        b.HasIndex(x => new { x.TenantId, x.WarehouseId, x.Status }).HasDatabaseName("IX_Device_TenantId_WarehouseId_Status");

        b.ToTable("Device", t =>
        {
            t.HasCheckConstraint("CK_Device_Type", "[Type] IN (1,2,3,4)");
            t.HasCheckConstraint("CK_Device_Status", "[Status] IN (1,2,3)");
        });
    }
}

internal sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> b)
    {
        b.ToTable("AuthSession");
        b.HasKey(x => x.Id);
        b.Property(x => x.SessionScope).Enum_().IsRequired();
        b.Property(x => x.Status).Enum_().IsRequired();
        b.Property(x => x.AuthenticationMethod).Enum_().IsRequired();
        b.Property(x => x.RevokedReason).HasMaxLength(200);
        b.Property(x => x.IpAddress).HasMaxLength(Convention.IpLength);
        b.Property(x => x.UserAgent).HasMaxLength(400);
        b.Property(x => x.CorrelationId).HasMaxLength(100);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne<AppUser>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .HasConstraintName("FK_AuthSession_AppUser");

        b.HasIndex(x => new { x.MembershipId, x.Status }).HasDatabaseName("IX_AuthSession_Membership_Status");
        b.HasIndex(x => new { x.DeviceId, x.Status }).HasDatabaseName("IX_AuthSession_Device_Status");
        b.HasIndex(x => x.ExpiresAtUtc).HasDatabaseName("IX_AuthSession_ExpiresAtUtc");
        b.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("IX_AuthSession_User_Status");
        b.HasIndex(x => new { x.UserId, x.Status })
            .HasFilter("[SessionScope] = 2")
            .HasDatabaseName("IX_AuthSession_Platform_Active");

        b.ToTable("AuthSession", t =>
        {
            // The absolute invariant of ADR-0034, pinned by the database so no code
            // path can produce a session that claims a tenant without a membership.
            t.HasCheckConstraint(
                "CK_AuthSession_SessionScope",
                "([SessionScope] = 2 AND [TenantId] IS NULL AND [MembershipId] IS NULL) OR " +
                "([SessionScope] = 1 AND [TenantId] IS NOT NULL AND [MembershipId] IS NOT NULL)");
            t.HasCheckConstraint("CK_AuthSession_Status", "[Status] IN (1,2,3,4)");
            t.HasCheckConstraint("CK_AuthSession_AuthenticationMethod", "[AuthenticationMethod] IN (1,2)");
            t.HasCheckConstraint(
                "CK_AuthSession_AbsoluteExpiry",
                "[AbsoluteExpiresAtUtc] >= [IssuedAtUtc] AND [ExpiresAtUtc] <= [AbsoluteExpiresAtUtc]");
        });
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshToken");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).Enum_().IsRequired();
        b.Property(x => x.CreatedByIpAddress).HasMaxLength(Convention.IpLength);

        b.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("UX_RefreshToken_TokenHash");
        b.HasIndex(x => new { x.FamilyId, x.Status }).HasDatabaseName("IX_RefreshToken_FamilyId_Status");

        b.ToTable("RefreshToken", t =>
            t.HasCheckConstraint("CK_RefreshToken_Status", "[Status] IN (1,2,3,4)"));
    }
}

internal sealed class PinCredentialConfiguration : IEntityTypeConfiguration<PinCredential>
{
    public void Configure(EntityTypeBuilder<PinCredential> b)
    {
        b.ToTable("PinCredential");
        b.HasKey(x => x.Id);
        b.Property(x => x.PinHash).HasMaxLength(400).IsRequired();
        b.Property(x => x.Version).IsRequired();
        b.Property(x => x.MustChangePin).IsRequired();
        b.Property(x => x.FailedAttemptCount).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        // Exactly one PIN credential per membership, unfiltered. A previous draft used
        // a filtered index on a Status column this table does not have.
        b.HasIndex(x => x.MembershipId).IsUnique().HasDatabaseName("UX_PinCredential_MembershipId");
        b.HasIndex(x => new { x.TenantId, x.LockoutEndAtUtc }).HasDatabaseName("IX_PinCredential_TenantId_LockoutEndAtUtc");
    }
}

internal sealed class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> b)
    {
        b.ToTable("LoginAttempt");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Method).Enum_().IsRequired();
        b.Property(x => x.Outcome).Enum_().IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(Convention.IpLength);
        b.Property(x => x.IsSuccess).IsRequired();

        b.HasIndex(x => new { x.IdentifierHash, x.OccurredAtUtc }).HasDatabaseName("IX_LoginAttempt_IdentifierHash_OccurredAtUtc");
        b.HasIndex(x => new { x.IpAddress, x.OccurredAtUtc }).HasDatabaseName("IX_LoginAttempt_IpAddress_OccurredAtUtc");
        b.HasIndex(x => new { x.EnrollmentCodeHash, x.OccurredAtUtc }).HasDatabaseName("IX_LoginAttempt_EnrollmentCodeHash_OccurredAtUtc");

        b.ToTable("LoginAttempt", t =>
        {
            t.HasCheckConstraint("CK_LoginAttempt_Method", "[Method] IN (1,2,3,4)");
            t.HasCheckConstraint("CK_LoginAttempt_Outcome", "[Outcome] IN (1,2,3,4,5)");
        });
    }
}

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> b)
    {
        b.ToTable("PasswordResetToken");
        b.HasKey(x => x.Id);
        b.Property(x => x.CreatedByIpAddress).HasMaxLength(Convention.IpLength);

        b.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_PasswordResetToken_AppUser");

        b.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("UX_PasswordResetToken_TokenHash");
    }
}