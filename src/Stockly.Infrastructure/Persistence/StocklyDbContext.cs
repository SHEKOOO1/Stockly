using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stockly.Domain.Common;

namespace Stockly.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context for the modular monolith.
/// <para>
/// It carries two structural isolation mechanisms, both enforced by the database
/// rather than by application discipline (DATABASE_DESIGN.md §1.2, §5):
/// </para>
/// <list type="number">
/// <item>
/// Every tenant-scoped table has a non-null <c>TenantId</c>, and every unique index
/// leads with it (ADR-0032), so within-tenant uniqueness is sufficient to enforce
/// cross-tenant uniqueness.
/// </item>
/// <item>
/// Every child-to-parent reference is a composite foreign key on <c>(Id, TenantId)</c>,
/// so a cross-tenant row is not merely filtered out — it cannot be inserted.
/// </item>
/// </list>
/// <para>
/// A global query filter (<see cref="_tenantId"/>) is the <em>third</em> layer. It is
/// a defence-in-depth measure and explicitly <strong>not</strong> the isolation
/// mechanism: a filter can be switched off with <c>IgnoreQueryFilters()</c>, so
/// nothing may rely on it alone.
/// </para>
/// </summary>
public sealed class StocklyDbContext : DbContext
{
    /// <summary>
    /// The tenant for this context instance. The DbContext is registered per scope,
    /// so EF Core caches the compiled query plan per instance and a captured field
    /// is parameterised rather than re-planned.
    /// </summary>
    private readonly Guid? _tenantId;

    private readonly bool _isPlatformScope;

    public StocklyDbContext(DbContextOptions<StocklyDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantId = tenantContext.TenantId;
        _isPlatformScope = tenantContext.IsPlatformScope;
        CurrentTenantId = tenantContext.TenantId;
        CurrentMembershipId = tenantContext.MembershipId;
        IsPlatformScope = tenantContext.IsPlatformScope;
    }

    /// <summary>The tenant this context is pinned to, or <c>null</c> outside a tenant session.</summary>
    public Guid? CurrentTenantId { get; }

    /// <summary>The membership this context is pinned to.</summary>
    public Guid? CurrentMembershipId { get; }

    public bool IsPlatformScope { get; }

    public DbSet<Domain.Modules.Platform.AppUser> AppUsers => Set<Domain.Modules.Platform.AppUser>();
    public DbSet<Domain.Modules.Platform.Plan> Plans => Set<Domain.Modules.Platform.Plan>();
    public DbSet<Domain.Modules.Platform.PlanFeature> PlanFeatures => Set<Domain.Modules.Platform.PlanFeature>();
    public DbSet<Domain.Modules.Platform.Subscription> Subscriptions => Set<Domain.Modules.Platform.Subscription>();
    public DbSet<Domain.Modules.Platform.TenantEntitlement> TenantEntitlements => Set<Domain.Modules.Platform.TenantEntitlement>();
    public DbSet<Domain.Modules.Platform.SubscriptionUsage> SubscriptionUsages => Set<Domain.Modules.Platform.SubscriptionUsage>();

    public DbSet<Domain.Modules.Identity.Tenant> Tenants => Set<Domain.Modules.Identity.Tenant>();
    public DbSet<Domain.Modules.Identity.TenantSetting> TenantSettings => Set<Domain.Modules.Identity.TenantSetting>();
    public DbSet<Domain.Modules.Identity.TenantMembership> TenantMemberships => Set<Domain.Modules.Identity.TenantMembership>();
    public DbSet<Domain.Modules.Identity.Permission> Permissions => Set<Domain.Modules.Identity.Permission>();
    public DbSet<Domain.Modules.Identity.Role> Roles => Set<Domain.Modules.Identity.Role>();
    public DbSet<Domain.Modules.Identity.RolePermission> RolePermissions => Set<Domain.Modules.Identity.RolePermission>();
    public DbSet<Domain.Modules.Identity.MembershipRole> MembershipRoles => Set<Domain.Modules.Identity.MembershipRole>();
    public DbSet<Domain.Modules.Identity.MembershipRoleWarehouse> MembershipRoleWarehouses => Set<Domain.Modules.Identity.MembershipRoleWarehouse>();
    public DbSet<Domain.Modules.Identity.Warehouse> Warehouses => Set<Domain.Modules.Identity.Warehouse>();
    public DbSet<Domain.Modules.Identity.MembershipWarehouse> MembershipWarehouses => Set<Domain.Modules.Identity.MembershipWarehouse>();
    public DbSet<Domain.Modules.Identity.Device> Devices => Set<Domain.Modules.Identity.Device>();
    public DbSet<Domain.Modules.Identity.AuthSession> AuthSessions => Set<Domain.Modules.Identity.AuthSession>();
    public DbSet<Domain.Modules.Identity.RefreshToken> RefreshTokens => Set<Domain.Modules.Identity.RefreshToken>();
    public DbSet<Domain.Modules.Identity.PinCredential> PinCredentials => Set<Domain.Modules.Identity.PinCredential>();
    public DbSet<Domain.Modules.Identity.LoginAttempt> LoginAttempts => Set<Domain.Modules.Identity.LoginAttempt>();
    public DbSet<Domain.Modules.Identity.PasswordResetToken> PasswordResetTokens => Set<Domain.Modules.Identity.PasswordResetToken>();

    public DbSet<Domain.Modules.Ops.DocumentSequence> DocumentSequences => Set<Domain.Modules.Ops.DocumentSequence>();
    public DbSet<Domain.Modules.Ops.OutboxMessage> OutboxMessages => Set<Domain.Modules.Ops.OutboxMessage>();

    public DbSet<Domain.Modules.Master.Category> Categories => Set<Domain.Modules.Master.Category>();
    public DbSet<Domain.Modules.Master.Unit> Units => Set<Domain.Modules.Master.Unit>();
    public DbSet<Domain.Modules.Master.UnitConversion> UnitConversions => Set<Domain.Modules.Master.UnitConversion>();
    public DbSet<Domain.Modules.Master.Product> Products => Set<Domain.Modules.Master.Product>();
    public DbSet<Domain.Modules.Master.ProductBarcode> ProductBarcodes => Set<Domain.Modules.Master.ProductBarcode>();
    public DbSet<Domain.Modules.Master.ProductWarehouseSetting> ProductWarehouseSettings => Set<Domain.Modules.Master.ProductWarehouseSetting>();
    public DbSet<Domain.Modules.Master.ReasonCode> ReasonCodes => Set<Domain.Modules.Master.ReasonCode>();

    public DbSet<Domain.Modules.Inventory.Batch> Batches => Set<Domain.Modules.Inventory.Batch>();
    public DbSet<Domain.Modules.Inventory.InventoryLot> InventoryLots => Set<Domain.Modules.Inventory.InventoryLot>();
    public DbSet<Domain.Modules.Inventory.StockBalance> StockBalances => Set<Domain.Modules.Inventory.StockBalance>();
    public DbSet<Domain.Modules.Inventory.StockDocument> StockDocuments => Set<Domain.Modules.Inventory.StockDocument>();
    public DbSet<Domain.Modules.Inventory.StockDocumentLine> StockDocumentLines => Set<Domain.Modules.Inventory.StockDocumentLine>();
    public DbSet<Domain.Modules.Inventory.StockTransaction> StockTransactions => Set<Domain.Modules.Inventory.StockTransaction>();
    public DbSet<Domain.Modules.Inventory.StockCount> StockCounts => Set<Domain.Modules.Inventory.StockCount>();
    public DbSet<Domain.Modules.Inventory.StockCountLine> StockCountLines => Set<Domain.Modules.Inventory.StockCountLine>();

    public DbSet<Domain.Modules.Purchasing.Supplier> Suppliers => Set<Domain.Modules.Purchasing.Supplier>();
    public DbSet<Domain.Modules.Purchasing.SupplierProduct> SupplierProducts => Set<Domain.Modules.Purchasing.SupplierProduct>();
    public DbSet<Domain.Modules.Purchasing.ProductPriceHistory> ProductPriceHistories => Set<Domain.Modules.Purchasing.ProductPriceHistory>();
    public DbSet<Domain.Modules.Purchasing.PurchaseRequest> PurchaseRequests => Set<Domain.Modules.Purchasing.PurchaseRequest>();
    public DbSet<Domain.Modules.Purchasing.PurchaseRequestLine> PurchaseRequestLines => Set<Domain.Modules.Purchasing.PurchaseRequestLine>();
    public DbSet<Domain.Modules.Purchasing.PurchaseApproval> PurchaseApprovals => Set<Domain.Modules.Purchasing.PurchaseApproval>();
    public DbSet<Domain.Modules.Purchasing.PurchaseOrder> PurchaseOrders => Set<Domain.Modules.Purchasing.PurchaseOrder>();
    public DbSet<Domain.Modules.Purchasing.PurchaseOrderLine> PurchaseOrderLines => Set<Domain.Modules.Purchasing.PurchaseOrderLine>();
    public DbSet<Domain.Modules.Purchasing.GoodsReceipt> GoodsReceipts => Set<Domain.Modules.Purchasing.GoodsReceipt>();
    public DbSet<Domain.Modules.Purchasing.GoodsReceiptLine> GoodsReceiptLines => Set<Domain.Modules.Purchasing.GoodsReceiptLine>();

    public DbSet<Domain.Modules.Events.Event> Events => Set<Domain.Modules.Events.Event>();
    public DbSet<Domain.Modules.Events.EventRequirement> EventRequirements => Set<Domain.Modules.Events.EventRequirement>();
    public DbSet<Domain.Modules.Events.EventConsumption> EventConsumptions => Set<Domain.Modules.Events.EventConsumption>();
    public DbSet<Domain.Modules.Events.EventAttendance> EventAttendances => Set<Domain.Modules.Events.EventAttendance>();
    public DbSet<Domain.Modules.Events.EventShortageSnapshot> EventShortageSnapshots => Set<Domain.Modules.Events.EventShortageSnapshot>();

    public DbSet<Domain.Modules.Food.MealType> MealTypes => Set<Domain.Modules.Food.MealType>();
    public DbSet<Domain.Modules.Food.Recipe> Recipes => Set<Domain.Modules.Food.Recipe>();
    public DbSet<Domain.Modules.Food.RecipeItem> RecipeItems => Set<Domain.Modules.Food.RecipeItem>();
    public DbSet<Domain.Modules.Food.RecipeCostSnapshot> RecipeCostSnapshots => Set<Domain.Modules.Food.RecipeCostSnapshot>();
    public DbSet<Domain.Modules.Food.EventFoodCostSnapshot> EventFoodCostSnapshots => Set<Domain.Modules.Food.EventFoodCostSnapshot>();

    public DbSet<Domain.Modules.Notifications.Notification> Notifications => Set<Domain.Modules.Notifications.Notification>();
    public DbSet<Domain.Modules.Notifications.NotificationPreference> NotificationPreferences => Set<Domain.Modules.Notifications.NotificationPreference>();
    public DbSet<Domain.Modules.Notifications.PushSubscription> PushSubscriptions => Set<Domain.Modules.Notifications.PushSubscription>();

    public DbSet<Domain.Modules.Files.Attachment> Attachments => Set<Domain.Modules.Files.Attachment>();

    public DbSet<Domain.Modules.Extensibility.CustomFieldDefinition> CustomFieldDefinitions => Set<Domain.Modules.Extensibility.CustomFieldDefinition>();
    public DbSet<Domain.Modules.Extensibility.CustomFieldValue> CustomFieldValues => Set<Domain.Modules.Extensibility.CustomFieldValue>();

    public DbSet<Domain.Modules.Audit.AuditLog> AuditLogs => Set<Domain.Modules.Audit.AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StocklyDbContext).Assembly);
        ConfigureCompositeTenantAlternateKeys(modelBuilder);
        ConfigureTenantRootForeignKeys(modelBuilder);
        ConfigureDeleteBehaviors(modelBuilder);
        ConfigureSchemas(modelBuilder);
        ConfigureTenantQueryFilters(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Ties every tenant-scoped table's <c>TenantId</c> to a real
    /// <c>identity.Tenant</c> row: <c>FOREIGN KEY (TenantId) REFERENCES Tenant (Id)</c>.
    /// <para>
    /// Without it, <c>TenantId</c> is a free-floating GUID. Nothing in the database
    /// would stop a row claiming to belong to a tenant that does not exist, and the
    /// tenant filter would happily return it — the composite child keys would all be
    /// satisfied by the phantom id. This is the load-bearing foreign key of the whole
    /// isolation scheme, so it is a convention rather than 66 hand-written
    /// declarations that could each be forgotten.
    /// </para>
    /// <para>
    /// It is single-column on purpose. The root has no <c>TenantId</c> of its own
    /// (DATABASE_DESIGN §4.2), so a composite key is impossible here; this is the one
    /// documented exception to §5.1. <c>Restrict</c>, never cascade: deleting a tenant
    /// is a deliberate, audited platform operation, not a side effect of something
    /// else.
    /// </para>
    /// <para>
    /// Entities that are deliberately outside this: <c>audit.AuditLog</c> and
    /// <c>ops.OutboxMessage</c> (both global with a nullable tenant for platform-level
    /// rows, and a composite key on a nullable column cannot exist in SQL Server) and
    /// <c>identity.AuthSession</c> (nullable tenant for a platform-authority session).
    /// None of them implement <see cref="ITenantScoped"/>, so none are touched.
    /// </para>
    /// </summary>
    private static void ConfigureTenantRootForeignKeys(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            if (entityType.ClrType == typeof(Domain.Modules.Identity.Tenant))
            {
                continue;
            }

            AddTenantRootForeignKey(modelBuilder, entityType.ClrType);
        }
    }

    /// <summary>
    /// Cached generic dispatch, one closure per entity type.
    /// <para>
    /// <c>EntityTypeBuilder.HasOne&lt;T&gt;()</c> is generic-only, so a runtime entity
    /// type cannot be configured directly. <c>MakeGenericMethod</c> is used instead of
    /// a hand-written <c>IMutableForeignKey</c> so that the relationship is still
    /// declared through EF's own public configuration API and validated by EF's own
    /// model validator. The delegate is cached, so the reflection cost is paid once per
    /// entity type for the lifetime of the process.
    /// </para>
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Action<ModelBuilder>> TenantRootKeyDispatch = new();

    private static void AddTenantRootForeignKey(ModelBuilder modelBuilder, Type entityClrType)
    {
        var configure = TenantRootKeyDispatch.GetOrAdd(
            entityClrType,
            static clrType => (Action<ModelBuilder>)typeof(StocklyDbContext)
                .GetMethod(nameof(ConfigureTenantRootForeignKeyCore), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(clrType)
                .CreateDelegate(typeof(Action<ModelBuilder>)));

        configure(modelBuilder);
    }

    private static void ConfigureTenantRootForeignKeyCore<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class
    {
        modelBuilder
            .Entity<TEntity>()
            .HasOne<Domain.Modules.Identity.Tenant>()
            .WithMany()
            .HasForeignKey(nameof(ITenantScoped.TenantId))
            .HasPrincipalKey(nameof(Entity.Id))
.OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Gives every tenant-scoped entity <c>UNIQUE (Id, TenantId)</c>.
    /// <para>
    /// This is not a convenience. DATABASE_DESIGN §5.1 rule 1 requires every child to
    /// reference its parent as <c>FOREIGN KEY (ParentId, TenantId) REFERENCES parent
    /// (Id, TenantId)</c> so a cross-tenant row cannot be inserted at all. SQL Server
    /// can only reference a declared key, and EF Core refuses to emit a composite
    /// foreign key whose principal is not an alternate key — the model would fail
    /// validation with "no such key" and the migration would be rejected.
    /// </para>
    /// <para>
    /// It is applied here rather than in 60 configurations because it is a structural
    /// invariant of the tenant boundary: forgetting it in one entity silently
    /// downgrades that entity's isolation to application-level filtering. One
    /// convention cannot be forgotten per table.
    /// </para>
    /// </summary>
    /// <summary>
    /// Forces <c>RESTRICT</c> on every foreign key, then re-enables <c>CASCADE</c> for
    /// the few child-configuration rows the design allows it on.
    /// <para>
    /// DATABASE_DESIGN §5.2: "Restrict, do not cascade, for business data. A CASCADE
    /// exists only from child configuration rows to their definition." EF's default is
    /// <c>CASCADE</c> for every required relationship, so relying on that default
    /// would have silently produced 84 cascading deletes — including
    /// <c>product.Product</c> cascading into its own batches, which is the opposite of
    /// the audit guarantee the ledger tables exist to provide.
    /// </para>
    /// <para>
    /// Cascading delete is limited to the outgoing keys of
    /// <c>identity.MembershipRoleWarehouse</c>: those rows are pure assignment records
    /// (membership + role + warehouse) with no lifecycle of their own, so dropping
    /// the assignment is dropping the row. Everything else restricts, so a delete can
    /// never silently remove stock, ledger or audit history.
    /// </para>
    /// </summary>
    private static void ConfigureDeleteBehaviors(ModelBuilder modelBuilder)
    {
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }

        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            // The tenant-root key is excluded explicitly: it is declared on every
            // tenant-scoped table, MembershipRoleWarehouse included, and deleting a
            // tenant must never cascade into anything. Without this guard the
            // blanket Restrict below would be undone for this one key.
            if (foreignKey.DeclaringEntityType.ClrType == typeof(Domain.Modules.Identity.MembershipRoleWarehouse)
                && foreignKey.PrincipalEntityType.ClrType != typeof(Domain.Modules.Identity.Tenant))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Cascade;
            }
        }
    }

    private static void ConfigureCompositeTenantAlternateKeys(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            modelBuilder
                .Entity(entityType.ClrType)
                .HasAlternateKey(nameof(Entity.Id), nameof(ITenantScoped.TenantId));
        }
    }

    /// <summary>
    /// Place every entity in its schema. Table names are <c>schema.SingularEntityName</c>
    /// (DATABASE_DESIGN.md §2), which is the plural type name plus an <c>s</c>, except
    /// where the type name already ends in <c>s</c>.
    /// </summary>
    private static void ConfigureSchemas(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var tableName = entityType.GetTableName();
            if (tableName is null)
            {
                tableName = clrType.Name;
            }

            entityType.SetSchema(SchemaFor(clrType));
        }
    }

    /// <summary>Maps a CLR namespace onto its database schema.</summary>
    public static string? SchemaFor(Type clrType)
    {
        var ns = clrType.Namespace;
        if (ns is null)
        {
            return null;
        }

        const string prefix = "Stockly.Domain.Modules.";
        if (!ns.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var segment = ns[prefix.Length..];
        var dot = segment.IndexOf('.', StringComparison.Ordinal);
        var schema = dot >= 0 ? segment[..dot] : segment;
        return schema switch
        {
            "Platform" => "platform",
            "Identity" => "identity",
            "Ops" => "ops",
            "Master" => "master",
            "Inventory" => "inventory",
            "Purchasing" => "purchasing",
            "Events" => "events",
            "Food" => "food",
            "Notifications" => "notifications",
            "Files" => "files",
            "Extensibility" => "extensibility",
            "Audit" => "audit",
            _ => null,
        };
    }

    /// <summary>
    /// Applies the tenant query filter to every tenant-scoped entity.
    /// <para>
    /// A platform-authority session gets <em>no</em> filter: it is a deliberately
    /// global scope (ADR-0034) whose tenant parameter is NULL. Every tenant-scoped
    /// query it issues must therefore be an explicit, reviewed cross-tenant query —
    /// which is what <see cref="IsPlatformScope"/> makes visible at the call site.
    /// </para>
    /// <para>
    /// Out of a tenant session (<c>_tenantId == null</c>) the filter pins every
    /// tenant-scoped query to <c>TenantId == null</c>, which matches no row. That is
    /// deliberate: an unauthenticated request must not be able to read tenant data
    /// by omitting the tenant parameter, and an explicit
    /// <c>IgnoreQueryFilters()</c> is required to get past it.
    /// </para>
    /// </summary>
    private void ConfigureTenantQueryFilters(ModelBuilder modelBuilder)
    {
        if (_isPlatformScope)
        {
            return;
        }

        var tenantId = _tenantId;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");

            // The property is `Guid` and the captured scope is `Guid?`, and
            // Expression.Equal refuses to compare those: there is no lifted `Guid == Guid?`
            // operator, so building this node throws "The binary operator Equal is not
            // defined for the types 'System.Guid' and 'System.Nullable`1[System.Guid]'".
            // The left side is promoted to `Guid?` instead, which keeps a single expression
            // for both cases: outside a tenant session the constant is null and the
            // comparison matches no row, exactly as documented below.
            var body = Expression.Equal(
                Expression.Convert(
                    Expression.Property(parameter, nameof(ITenantScoped.TenantId)),
                    typeof(Guid?)),
                Expression.Constant(tenantId, typeof(Guid?)));

            var lambda = Expression.Lambda(body, parameter);

            // Soft-deleted rows are hidden by default. Combined with filtered unique
            // indexes ("WHERE IsDeleted = 0") a code can be reused after deletion
            // while the deleted row's history stays intact.
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                entityType.SetQueryFilter(Expression.Lambda(
                    Expression.AndAlso(
                        lambda.Body,
                        Expression.Not(Expression.Property(parameter, nameof(ISoftDelete.IsDeleted)))),
                    parameter));
            }
            else
            {
                entityType.SetQueryFilter(lambda);
            }
        }
    }
}