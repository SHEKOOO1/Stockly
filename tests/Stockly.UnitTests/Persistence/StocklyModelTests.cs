using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Stockly.Domain.Common;
using Stockly.Infrastructure.Persistence;

namespace Stockly.UnitTests.Persistence;

/// <summary>
/// Guards the EF Core model itself.
/// <para>
/// These tests need <strong>no database</strong>. EF validates a model completely
/// when it is first built — a bad composite foreign key, a missing alternate key or a
/// duplicated index throws during model construction, before any connection is opened.
/// That is what makes a model defect catchable on a workstation with no SQL Server.
/// Applying a migration is a separate concern and still requires a real server.
/// </para>
/// </summary>
public sealed class StocklyModelTests
{
    private static StocklyDbContext NewContext(bool isPlatformScope = true) =>
        new(
            new DbContextOptionsBuilder<StocklyDbContext>()
                .UseSqlServer("Server=unused;Database=Unused;Trusted_Connection=True;TrustServerCertificate=True")
                .Options,
            new FixedTenantContext(null, null, isPlatformScope));

    private static IModel Model() => NewContext().Model;

    [Fact]
    public void Model_builds_without_validation_errors()
    {
        // Touching Model triggers OnModelCreating and EF's validator.
        var model = NewContext().Model;

        Assert.NotNull(model);
    }

    [Fact]
    public void Every_entity_is_mapped_to_a_schema()
    {
        foreach (var entityType in Model().GetEntityTypes())
        {
            Assert.False(
                string.IsNullOrWhiteSpace(entityType.GetSchema()),
                $"{entityType.ClrType.Name} is not assigned a schema.");
        }
    }

    [Fact]
    public void Tenant_root_has_no_TenantId_column()
    {
        var tenant = Model().FindEntityType(typeof(Domain.Modules.Identity.Tenant))!;

        Assert.Null(tenant.FindProperty(nameof(ITenantScoped.TenantId)));
        Assert.Empty(tenant.GetForeignKeys());
    }

    [Fact]
    public void Every_tenant_scoped_entity_has_a_composite_tenant_alternate_key()
    {
        foreach (var entityType in Model().GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var hasCompositeKey = entityType
                .GetKeys()
                .Any(k => k.Properties.Count == 2
                    && k.Properties[0].Name == nameof(Entity.Id)
                    && k.Properties[1].Name == nameof(ITenantScoped.TenantId));

            Assert.True(
                hasCompositeKey,
                $"{entityType.ClrType.Name} has no UNIQUE (Id, TenantId); composite tenant foreign keys cannot be emitted against it.");
        }
    }

    [Fact]
    public void No_cross_tenant_reference_is_possible()
    {
        foreach (var entityType in Model().GetEntityTypes())
        {
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var principal = foreignKey.PrincipalEntityType;

                if (!typeof(ITenantScoped).IsAssignableFrom(principal.ClrType))
                {
                    continue;
                }

                var carriesTenantId = foreignKey.Properties.Any(p => p.Name == nameof(ITenantScoped.TenantId));

                Assert.True(
                    carriesTenantId,
                    $"{entityType.ClrType.Name} -> {principal.ClrType.Name} does not include TenantId in its foreign key, so a cross-tenant row would be accepted.");
            }
        }
    }

    [Theory]
    [InlineData(typeof(Domain.Modules.Inventory.StockTransaction))]
    [InlineData(typeof(Domain.Modules.Audit.AuditLog))]
    [InlineData(typeof(Domain.Modules.Purchasing.ProductPriceHistory))]
    [InlineData(typeof(Domain.Modules.Events.EventShortageSnapshot))]
    [InlineData(typeof(Domain.Modules.Food.RecipeCostSnapshot))]
    [InlineData(typeof(Domain.Modules.Food.EventFoodCostSnapshot))]
    public void Append_only_tables_have_no_rowversion_and_are_never_soft_deleted(Type entityClrType)
    {
        var entityType = Model().FindEntityType(entityClrType)!;

        Assert.Null(entityType.FindProperty(nameof(IHasRowVersion.RowVersion)));
        Assert.Null(entityType.FindProperty(nameof(ISoftDelete.IsDeleted)));
    }

    [Fact]
    public void Business_data_restricts_deletes_and_only_the_scope_junction_cascades()
    {
        var cascading = Model()
            .GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(f => f.DeleteBehavior == DeleteBehavior.Cascade)
            .ToList();

        foreach (var foreignKey in cascading)
        {
            Assert.Equal(
                typeof(Domain.Modules.Identity.MembershipRoleWarehouse),
                foreignKey.DeclaringEntityType.ClrType);
        }
    }

    [Fact]
    public void Every_tenant_scoped_table_references_the_tenant_root()
    {
        // Without this the TenantId column is a free-floating value: the database
        // would accept a row whose TenantId matches no tenant at all.
        foreach (var entityType in Model().GetEntityTypes())
        {
            if (entityType.ClrType == typeof(Domain.Modules.Identity.Tenant))
            {
                continue;
            }

            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var referencesRoot = entityType
                .GetForeignKeys()
                .Any(f => f.PrincipalEntityType.ClrType == typeof(Domain.Modules.Identity.Tenant));

            Assert.True(
                referencesRoot,
                $"{entityType.ClrType.Name} has no foreign key to identity.Tenant, so its TenantId is not tied to a real tenant.");
        }
    }

    [Fact]
    public void Audit_log_uses_a_database_generated_identity()
    {
        var auditLog = Model().FindEntityType(typeof(Domain.Modules.Audit.AuditLog))!;
        var id = auditLog.FindProperty(nameof(Domain.Modules.Audit.AuditLog.Id))!;

        Assert.Equal(ValueGenerated.OnAdd, id.ValueGenerated);
    }
}