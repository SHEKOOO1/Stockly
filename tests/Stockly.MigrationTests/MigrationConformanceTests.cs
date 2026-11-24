using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Stockly.Infrastructure.Persistence;
using Stockly.StocklyDbTestSupport;

namespace Stockly.MigrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class MigrationConformanceTests(SqlServerFixture fixture)
{
    private static readonly string[] ExpectedMigrations =
    [
        "20261001174239_InitialSchema",
        "20261001184551_TenantIsolation"
    ];

    [Fact]
    public async Task Exactly_two_migrations_are_declared_and_applied()
    {
        fixture.RequireServer();
        await fixture.MigrateAsync();

        var options = new DbContextOptionsBuilder<StocklyDbContext>()
            .UseSqlServer(fixture.ConnectionString)
            .Options;
        await using var context = new StocklyDbContext(
            options,
            new FixedTenantContext(null, null, isPlatformScope: true));

        context.Database.GetMigrations().Should().BeEquivalentTo(ExpectedMigrations);
        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();

        await using var connection = await ScopedSql.OpenUnscopedAsync(fixture.ConnectionString);
        var applied = await ScopedSql.QueryAsync(
            connection,
            "SELECT MigrationId FROM [__EFMigrationsHistory] ORDER BY MigrationId;");
        applied.Select(row => (string)row["MigrationId"]!).Should().Equal(ExpectedMigrations);
    }

    [Fact]
    public async Task Initial_schema_creates_the_expected_catalog_shape()
    {
        fixture.RequireServer();
        await fixture.MigrateAsync();

        await using var connection = await ScopedSql.OpenUnscopedAsync(fixture.ConnectionString);

        var tableCount = await ScopedSql.ScalarAsync<int>(
            connection,
            "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0 AND name <> N'__EFMigrationsHistory';");
        tableCount.Should().Be(66);

        var schemaCount = await ScopedSql.ScalarAsync<int>(
            connection,
            "SELECT COUNT(DISTINCT SCHEMA_NAME(schema_id)) FROM sys.tables WHERE is_ms_shipped = 0 AND name <> N'__EFMigrationsHistory' AND SCHEMA_NAME(schema_id) <> N'dbo';");
        schemaCount.Should().Be(12);

        var tenantTableCount = await ScopedSql.ScalarAsync<int>(
            connection,
            "SELECT COUNT(*) FROM sys.tables t WHERE t.is_ms_shipped = 0 AND t.name <> N'__EFMigrationsHistory' AND EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = N'TenantId');");
        tenantTableCount.Should().Be(60);
    }

    [Fact]
    public async Task Tenant_isolation_migration_creates_every_required_artifact()
    {
        fixture.RequireServer();
        await fixture.MigrateAsync();

        await using var connection = await ScopedSql.OpenUnscopedAsync(fixture.ConnectionString);

        (await ScopedSql.ScalarAsync<int>(
            connection,
            "SELECT COUNT(*) FROM sys.security_policies WHERE name = N'spStocklyTenantPolicy' AND is_enabled = 1;"))
            .Should().Be(1);

        (await ScopedSql.ScalarAsync<int>(
            connection,
            "SELECT COUNT(*) FROM sys.security_predicates WHERE predicate_type = 0 AND predicate_definition LIKE N'%fnStocklyTenantVisible%';"))
            .Should().Be(60);

        (await ScopedSql.ScalarAsync<int>(
            connection,
            "SELECT COUNT(*) FROM sys.check_constraints WHERE name LIKE N'CK[_]%[_]TenantScope';"))
            .Should().Be(60);

        (await ScopedSql.ScalarAsync<int>(
            connection,
            "SELECT COUNT(*) FROM sys.objects WHERE type IN (N'FN', N'IF') AND name IN (N'fnStocklyRowTenantAllowed', N'fnStocklyTenantVisible') AND SCHEMA_NAME(schema_id) = N'security';"))
            .Should().Be(2);

        (await ScopedSql.ScalarAsync<int>(
            connection,
            "SELECT COUNT(*) FROM sys.procedures WHERE name = N'uspRevalidateTenantScopeConstraints' AND SCHEMA_NAME(schema_id) = N'security';"))
            .Should().Be(1);
    }

    [Fact]
    public async Task Every_tenant_scoped_table_has_trusted_visibility_and_ownership_guards()
    {
        fixture.RequireServer();
        await fixture.MigrateAsync();

        await using var connection = await ScopedSql.OpenUnscopedAsync(fixture.ConnectionString);

        var missingVisibility = await ScopedSql.ScalarAsync<int>(
            connection,
            "SELECT COUNT(*) FROM sys.tables t WHERE t.is_ms_shipped = 0 AND t.name <> N'__EFMigrationsHistory' AND EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = N'TenantId') AND NOT EXISTS (SELECT 1 FROM sys.security_predicates sp WHERE sp.target_object_id = t.object_id AND sp.predicate_type = 0 AND sp.predicate_definition LIKE N'%fnStocklyTenantVisible%');");
        missingVisibility.Should().Be(0);

        var missingOwnership = await ScopedSql.ScalarAsync<int>(
            connection,
            "SELECT COUNT(*) FROM sys.tables t WHERE t.is_ms_shipped = 0 AND t.name <> N'__EFMigrationsHistory' AND EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = N'TenantId') AND NOT EXISTS (SELECT 1 FROM sys.check_constraints cc WHERE cc.parent_object_id = t.object_id AND cc.name = N'CK_' + t.name + N'_TenantScope');");
        missingOwnership.Should().Be(0);

        var untrusted = await ScopedSql.ScalarAsync<int>(
            connection,
            "SELECT COUNT(*) FROM sys.check_constraints WHERE is_disabled = 1 OR is_not_trusted = 1;");
        untrusted.Should().Be(0);
    }
}
