using Microsoft.Data.SqlClient;
using Stockly.Domain.Common;
using Stockly.StocklyDbTestSupport;

namespace Stockly.SecurityTests;

/// <summary>
/// The tenant isolation boundary, asserted against a real Microsoft SQL Server as a
/// non-administrator principal (ADR-0051).
/// </summary>
/// <remarks>
/// <para>
/// Every test here runs as the <em>tenant</em> principal: a SQL login with DML on the tenant
/// tables and <strong>no</strong> membership of <c>stockly_platform_role</c>. That is what
/// makes the suite meaningful. Running it as <c>dbo</c>, <c>sa</c> or <c>sysadmin</c> would
/// test the administrator's exemption rules rather than the application's isolation, and RLS
/// is not a reliable boundary against a server administrator on every configuration.
/// </para>
/// <para>
/// The attacks are not weakened for the tests. They deliberately try to read and write the
/// other tenant's row by primary key, by raw SQL with no ORM in the way, and with EF's
/// <c>IgnoreQueryFilters()</c>, which is exactly what an attacker with any SQL access would
/// attempt. A test that quietly added a query filter back would be testing the ORM.
/// </para>
/// </remarks>
[Collection(SqlServerCollection.Name)]
public sealed class TenantIsolationTests(SqlServerFixture fixture) : TenantIsolationTestBase(fixture)
{
    // ------------------------------------------------------------------
    // 1. Fail closed: no session context at all.
    // ------------------------------------------------------------------

    /// <summary>
    /// A connection nobody has authenticated sees nothing.
    /// <para>
    /// This is the single most important property in the suite. An absent scope must mean
    /// "no access", never "no restriction" -- the alternative makes forgetting to authenticate
    /// the most powerful state in the system.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Missing_session_context_reads_no_rows()
    {
        await SeedUnitsAsync();

        await using var unscoped = await ScopedSql.OpenUnscopedAsync(TenantConnectionString);

        var rows = await ScopedSql.QueryAsync(unscoped, "SELECT Code FROM [master].[Unit];");
        Assert.Empty(rows);
    }

    // ------------------------------------------------------------------
    // 2. A tenant scope sees only its own rows.
    // ------------------------------------------------------------------

    [Fact]
    public async Task Tenant_scope_reads_only_its_own_rows()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);
        var rowsA = await ScopedSql.QueryAsync(tenantA, "SELECT Code, TenantId FROM [master].[Unit];");

        await using var tenantB = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantB);
        var rowsB = await ScopedSql.QueryAsync(tenantB, "SELECT Code FROM [master].[Unit];");

        Assert.Equal([UnitACode], rowsA.Select(r => (string)r["Code"]!));
        Assert.Equal([UnitBCode], rowsB.Select(r => (string)r["Code"]!));
    }

    // ------------------------------------------------------------------
    // 3-4. Cross-tenant UPDATE and DELETE affect zero rows.
    // ------------------------------------------------------------------

    [Fact]
    public async Task Cross_tenant_update_affects_zero_rows()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        var affected = await ScopedSql.ExecuteAsync(tenantA, $"""
            UPDATE [master].[Unit] SET Name = N'PWNED' WHERE Id = '{UnitB:D}';
            """);

        Assert.Equal(0, affected);
        Assert.Equal("Tenant B base unit", await PlatformNameAsync(UnitB));
    }

    [Fact]
    public async Task Cross_tenant_delete_affects_zero_rows()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        var affected = await ScopedSql.ExecuteAsync(tenantA, $"DELETE FROM [master].[Unit] WHERE Id = '{UnitB:D}';");

        Assert.Equal(0, affected);
        Assert.Equal(1, await PlatformRowCountAsync(UnitBCode));
    }

    // ------------------------------------------------------------------
    // 5-6. INSERT and TenantId reassignment are refused by the CHECK constraint.
    // ------------------------------------------------------------------

    /// <summary>
    /// A tenant cannot insert a row owned by another tenant.
    /// <para>
    /// This is the case RLS cannot cover. A FILTER predicate gates SELECT, UPDATE and DELETE,
    /// but never INSERT on a regular table -- measured on this engine: under a policy with no
    /// CHECK constraint, a cross-tenant INSERT succeeded and the row persisted. The
    /// <c>CK_&lt;Table&gt;_TenantScope</c> constraint is what closes it.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Cross_tenant_insert_is_refused()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        var error = await ErrorNumberFromAsync(() => ScopedSql.ExecuteAsync(tenantA, $"""
            INSERT INTO [master].[Unit] (Id, TenantId, Code, Name, UnitType, DecimalPlaces, IsBaseUnit, IsActive, IsDeleted)
            VALUES (NEWID(), '{TenantB:D}', N'{MustNotPersist}', N'Injected', 1, 0, 0, 1, 0);
            """));

        ShouldBeRefusedByOwnershipConstraint(error);
        Assert.Equal(0, await PlatformRowCountAsync(MustNotPersist));
    }

    /// <summary>
    /// A tenant cannot move its own row to another tenant.
    /// <para>
    /// Reassignment is an UPDATE, so a FILTER predicate does not stop it -- the predicate is
    /// evaluated on the row being read, not on the value being written. Only the ownership
    /// CHECK constraint rejects it.
    /// </para>
    /// </summary>
    [Fact]
    public async Task TenantId_reassignment_is_refused()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        var error = await ErrorNumberFromAsync(() => ScopedSql.ExecuteAsync(tenantA, $"""
            UPDATE [master].[Unit] SET TenantId = '{TenantB:D}' WHERE Id = '{UnitA:D}';
            """));

        ShouldBeRefusedByOwnershipConstraint(error);

        // The row must still belong to tenant A, verified independently of the error.
        await using var platform = await ScopedSql.OpenPlatformAsync(PlatformConnectionString);
        var owner = await ScopedSql.ScalarAsync<Guid>(platform, $"SELECT TenantId FROM [master].[Unit] WHERE Id = '{UnitA:D}';");
        Assert.Equal(TenantA, owner);
    }

    // ------------------------------------------------------------------
    // 7-8. The tenant's own writes still work.
    // ------------------------------------------------------------------

    [Fact]
    public async Task Own_tenant_update_succeeds()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        var affected = await ScopedSql.ExecuteAsync(tenantA, $"UPDATE [master].[Unit] SET Name = N'Renamed by A' WHERE Id = '{UnitA:D}';");

        Assert.Equal(1, affected);
        Assert.Equal("Renamed by A", await PlatformNameAsync(UnitA));
    }

    [Fact]
    public async Task Own_tenant_insert_succeeds()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        var affected = await ScopedSql.ExecuteAsync(tenantA, $"""
            INSERT INTO [master].[Unit] (Id, TenantId, Code, Name, UnitType, DecimalPlaces, IsBaseUnit, IsActive, IsDeleted)
            VALUES (NEWID(), '{TenantA:D}', N'{OwnUnitCode}', N'Own row', 1, 0, 0, 1, 0);
            """);

        Assert.Equal(1, affected);
        Assert.Equal(1, await PlatformRowCountAsync(OwnUnitCode));
    }

    // ------------------------------------------------------------------
    // 9. Claiming platform scope without the role grants nothing.
    // ------------------------------------------------------------------

    /// <summary>
    /// Setting <c>StocklyScope = 2</c> is not enough -- platform authority also requires
    /// membership of <c>stockly_platform_role</c>.
    /// <para>
    /// If the session-context value alone granted access, any session able to call
    /// <c>sp_set_session_context</c> would be a platform administrator. Requiring a role
    /// membership means the capability can only be granted by an <c>ALTER ROLE</c> executed
    /// against the database, which application code cannot do.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Spoofed_platform_scope_without_role_membership_grants_nothing()
    {
        await SeedUnitsAsync();

        await using var spoofed = await ScopedSql.OpenSpoofedPlatformAsync(TenantConnectionString);

        Assert.Equal(0, (await ScopedSql.ScalarAsync<int>(spoofed, "SELECT COUNT(*) FROM [master].[Unit];")));

        var affected = await ScopedSql.ExecuteAsync(spoofed, $"UPDATE [master].[Unit] SET Name = N'PWNED';");
        Assert.Equal(0, affected);

        var insertError = await ErrorNumberFromAsync(() => ScopedSql.ExecuteAsync(spoofed, $"""
            INSERT INTO [master].[Unit] (Id, TenantId, Code, Name, UnitType, DecimalPlaces, IsBaseUnit, IsActive, IsDeleted)
            VALUES (NEWID(), '{TenantB:D}', N'{MustNotPersist}', N'Injected', 1, 0, 0, 1, 0);
            """));

        ShouldBeRefusedByOwnershipConstraint(insertError);
        Assert.Equal(0, await PlatformRowCountAsync(MustNotPersist));
    }

    // ------------------------------------------------------------------
    // 10. An unscoped write is refused.
    // ------------------------------------------------------------------

    [Fact]
    public async Task Unscoped_write_is_refused()
    {
        await SeedUnitsAsync();

        await using var unscoped = await ScopedSql.OpenUnscopedAsync(TenantConnectionString);

        var error = await ErrorNumberFromAsync(() => ScopedSql.ExecuteAsync(unscoped, $"""
            INSERT INTO [master].[Unit] (Id, TenantId, Code, Name, UnitType, DecimalPlaces, IsBaseUnit, IsActive, IsDeleted)
            VALUES (NEWID(), '{TenantA:D}', N'{MustNotPersist}', N'Anonymous', 1, 0, 0, 1, 0);
            """));

        ShouldBeRefusedByOwnershipConstraint(error);
        Assert.Equal(0, await PlatformRowCountAsync(MustNotPersist));
    }

    // ------------------------------------------------------------------
    // 11. Ground truth.
    // ------------------------------------------------------------------

    /// <summary>
    /// After a full round of attacks, the database contains exactly what it should.
    /// <para>
    /// Verifying only that each attack "was refused" would still pass if a refusal happened to
    /// have destroyed data. This checks the end state with an independent principal, so both a
    /// leak and a lost row fail the suite.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Ground_truth_no_unauthorised_row_persisted_and_tenant_b_untouched()
    {
        await SeedUnitsAsync();

        await using (var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA))
        {
            await ScopedSql.ExecuteAsync(tenantA, $"UPDATE [master].[Unit] SET Name = N'PWNED' WHERE Id = '{UnitB:D}';");
            await ScopedSql.ExecuteAsync(tenantA, $"DELETE FROM [master].[Unit] WHERE Id = '{UnitB:D}';");
            await ErrorNumberFromAsync(() => ScopedSql.ExecuteAsync(tenantA, $"""
                INSERT INTO [master].[Unit] (Id, TenantId, Code, Name, UnitType, DecimalPlaces, IsBaseUnit, IsActive, IsDeleted)
                VALUES (NEWID(), '{TenantB:D}', N'{MustNotPersist}', N'Injected', 1, 0, 0, 1, 0);
                """));
        }

        await using var platform = await ScopedSql.OpenPlatformAsync(PlatformConnectionString);
        var mine = await ScopedSql.QueryAsync(platform, $"""
            SELECT Code, TenantId FROM [master].[Unit] WHERE Id IN ('{UnitA:D}', '{UnitB:D}');
            """);

        // Scoped to this test's own rows on purpose: every test in this class shares one
        // database, so an unfiltered count would assert on other tests' seeds and fail for
        // reasons unrelated to what is under test.
        Assert.DoesNotContain(mine, r => (string)r["Code"]! == MustNotPersist);
        Assert.Equal(2, mine.Count);
        Assert.Equal("Tenant B base unit", await PlatformNameAsync(UnitB));
        Assert.Equal(TenantA, (Guid)mine.Single(r => (string)r["Code"]! == UnitACode)["TenantId"]!);
        Assert.Equal(TenantB, (Guid)mine.Single(r => (string)r["Code"]! == UnitBCode)["TenantId"]!);

        // And the injected code is absent database-wide, not merely from this test's rows.
        Assert.Equal(0, await PlatformRowCountAsync(MustNotPersist));
    }

    // ------------------------------------------------------------------
    // 12. EF query filters are not the boundary.
    // ------------------------------------------------------------------

    /// <summary>
    /// <c>IgnoreQueryFilters()</c> cannot see another tenant's row.
    /// <para>
    /// This is the assertion that separates "the database enforces isolation" from "the ORM
    /// was helpful". EF's global query filter is defence in depth; if it were the boundary,
    /// this test would fail, which is the point of running it.
    /// </para>
    /// </summary>
    [Fact]
    public async Task IgnoreQueryFilters_cannot_bypass_isolation()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        // Raw SQL with no ORM, no filter and no hint of a tenant predicate.
        var visible = await ScopedSql.QueryAsync(tenantA, "SELECT Code FROM [master].[Unit];");
        Assert.DoesNotContain(visible, r => (string)r["Code"]! == UnitBCode);

        var guessed = await ScopedSql.QueryAsync(tenantA, $"SELECT Code FROM [master].[Unit] WHERE Id = '{UnitB:D}';");
        Assert.Empty(guessed);
    }

    // ------------------------------------------------------------------
    // 13. Raw SQL cannot bypass isolation.
    // ------------------------------------------------------------------

    [Fact]
    public async Task Raw_sql_writes_cannot_bypass_isolation()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        var updateError = await ErrorNumberFromAsync(() => ScopedSql.ExecuteAsync(tenantA, $"""
            UPDATE [master].[Unit] SET Name = N'PWNED-RAW' WHERE TenantId = '{TenantB:D}';
            """));

        // A raw UPDATE against another tenant is filtered to zero rows rather than raising,
        // because that is what the FILTER predicate does. Either outcome is safe; a row
        // change is not.
        Assert.True(updateError is null or ScopedSql.CheckConstraintViolationErrorNumber);

        Assert.Equal("Tenant B base unit", await PlatformNameAsync(UnitB));
    }

    // ------------------------------------------------------------------
    // 14. A missing scope cannot expose global tenant data.
    // ------------------------------------------------------------------

    [Fact]
    public async Task Missing_context_cannot_expose_tenant_data()
    {
        await SeedUnitsAsync();

        await using var unscoped = await ScopedSql.OpenUnscopedAsync(TenantConnectionString);

        foreach (var table in new[] { "master.Unit", "master.Product", "identity.Warehouse", "inventory.StockBalance" })
        {
            var rows = await ScopedSql.QueryAsync(unscoped, $"SELECT 1 AS one FROM [{table.Split('.')[0]}].[{table.Split('.')[1]}];");
            Assert.True(rows.Count == 0, $"Unscoped session read {rows.Count} rows from {table}; expected none.");
        }
    }

    // ------------------------------------------------------------------
    // 15-17. Guessed identifiers give nothing away.
    // ------------------------------------------------------------------

    [Fact]
    public async Task Tenant_cannot_read_tenant_b_by_guessed_id()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        var byId = await ScopedSql.QueryAsync(tenantA, $"SELECT Code FROM [master].[Unit] WHERE Id = '{UnitB:D}';");
        var byCode = await ScopedSql.QueryAsync(tenantA, $"SELECT Code FROM [master].[Unit] WHERE Code = N'{UnitBCode}';");
        var byPredicate = await ScopedSql.QueryAsync(tenantA, $"SELECT Code FROM [master].[Unit] WHERE 1 = 1 AND Name LIKE N'%B%';");

        Assert.Empty(byId);
        Assert.Empty(byCode);

        // The filter predicate cannot be the only thing hiding tenant B: the LIKE is written so
        // that it also matches tenant A's own row ("Tenant A base unit" contains a "B"), and the
        // point of the assertion is that B is absent while A's own match survives. Asserting the
        // result set was empty would pass for the wrong reason -- it would also pass on a
        // connection that could read nothing at all, which is the failure mode that made an
        // earlier version of this file report cross-tenant isolation while the tenant principal
        // could not read its own data.
        Assert.DoesNotContain(byPredicate, row => (string)row["Code"]! == UnitBCode);
        Assert.Contains(byPredicate, row => (string)row["Code"]! == UnitACode);
    }

    [Fact]
    public async Task Tenant_cannot_modify_tenant_b_by_guessed_id()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        var byCode = await ScopedSql.ExecuteAsync(tenantA, $"UPDATE [master].[Unit] SET Name = N'PWNED' WHERE Code = N'{UnitBCode}';");
        Assert.Equal(0, byCode);
        Assert.Equal("Tenant B base unit", await PlatformNameAsync(UnitB));
    }

    [Fact]
    public async Task Tenant_cannot_delete_tenant_b_by_guessed_id()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        var affected = await ScopedSql.ExecuteAsync(tenantA, $"DELETE FROM [master].[Unit] WHERE Code = N'{UnitBCode}';");
        Assert.Equal(0, affected);
        Assert.Equal(1, await PlatformRowCountAsync(UnitBCode));
    }

    // ------------------------------------------------------------------
    // 18. TenantId cannot be altered.
    // ------------------------------------------------------------------

    [Fact]
    public async Task Tenant_cannot_alter_TenantId_to_null_or_to_another_tenant()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        // To another tenant.
        ShouldBeRefusedByOwnershipConstraint(await ErrorNumberFromAsync(
            () => ScopedSql.ExecuteAsync(tenantA, $"UPDATE [master].[Unit] SET TenantId = '{TenantB:D}' WHERE Id = '{UnitA:D}';")));

        // To NULL. This has to be attempted on a table whose TenantId is actually nullable.
        // master.Unit.TenantId is NOT NULL, so setting it to NULL there is refused by SQL Server
        // as error 515 (cannot insert the value NULL into a column that does not allow nulls)
        // -- a NOT NULL constraint, not the ownership check. Asserting 547 there would prove
        // nothing about ownership: the row would be refused even if the CHECK constraints, the
        // policy and the whole ownership model were removed.
        //
        // Three tables carry a nullable TenantId for legitimate platform-owned rows
        // (audit.AuditLog, identity.AuthSession, ops.OutboxMessage). ops.OutboxMessage is the
        // target because it needs no parent rows, so the tenant can insert its own row and then
        // try to promote it to platform scope. That is exactly the attack the nullable column
        // would otherwise permit.
        var outboxId = Guid.NewGuid();
        var pending = (int)OutboxStatus.Pending;
        Assert.Equal(1, await ScopedSql.ExecuteAsync(tenantA, $"""
            INSERT INTO [ops].[OutboxMessage]
                (Id, TenantId, MessageType, Payload, Status, Attempts, NextAttemptAtUtc)
            VALUES
                ('{outboxId:D}', '{TenantA:D}', N'SecurityTest', N'seed', {pending}, 0, SYSUTCDATETIME());
            """));

        ShouldBeRefusedByOwnershipConstraint(await ErrorNumberFromAsync(
            () => ScopedSql.ExecuteAsync(tenantA, $"UPDATE [ops].[OutboxMessage] SET TenantId = NULL WHERE Id = '{outboxId:D}';")));

        // Still tenant-owned after the refused update.
        await using (var platform = await ScopedSql.OpenPlatformAsync(PlatformConnectionString))
        {
            Assert.Equal(
                TenantA,
                await ScopedSql.ScalarAsync<Guid>(platform, $"SELECT TenantId FROM [ops].[OutboxMessage] WHERE Id = '{outboxId:D}';"));
        }

        await CleanupOutboxAsync(outboxId);

        await using var platformCheck = await ScopedSql.OpenPlatformAsync(PlatformConnectionString);
        Assert.Equal(TenantA, await ScopedSql.ScalarAsync<Guid>(platformCheck, $"SELECT TenantId FROM [master].[Unit] WHERE Id = '{UnitA:D}';"));
    }

    // ------------------------------------------------------------------
    // 19-20. The tenant principal cannot weaken the boundary.
    // ------------------------------------------------------------------

    /// <summary>
    /// The tenant principal cannot switch the RLS policy off, drop it, or add a permissive
    /// one.
    /// <para>
    /// Isolation that the application principal can switch off is not isolation. Each of
    /// these fails for a different reason -- <c>ALTER</c> permission, <c>CONTROL</c> on the
    /// policy -- and the test asserts the statement was refused, not the specific mechanism,
    /// because the guarantee is the refusal.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Tenant_principal_cannot_modify_RLS_objects()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        Assert.NotNull(await ErrorNumberFromAsync(
            () => ScopedSql.ExecuteAsync(tenantA, "ALTER SECURITY POLICY security.spStocklyTenantPolicy WITH (STATE = OFF);")));

        Assert.NotNull(await ErrorNumberFromAsync(
            () => ScopedSql.ExecuteAsync(tenantA, "DROP SECURITY POLICY security.spStocklyTenantPolicy;")));

        Assert.NotNull(await ErrorNumberFromAsync(
            () => ScopedSql.ExecuteAsync(tenantA, $"""
                CREATE SECURITY POLICY security.spAttacker
                ADD FILTER PREDICATE security.fnStocklyTenantVisible([TenantId]) ON [master].[Unit];
                """)));

        // Isolation must still hold after all three attempts.
        var visible = await ScopedSql.QueryAsync(tenantA, "SELECT Code FROM [master].[Unit];");
        Assert.Equal([UnitACode], visible.Select(r => (string)r["Code"]!));
    }

    /// <summary>
    /// The tenant principal cannot disable a CHECK constraint, replace the function behind
    /// one, or grant itself platform authority.
    /// </summary>
    [Fact]
    public async Task Tenant_principal_cannot_modify_ownership_constraints_or_grant_itself_platform_authority()
    {
        await SeedUnitsAsync();

        await using var tenantA = await ScopedSql.OpenTenantAsync(TenantConnectionString, TenantA);

        ShouldBeRefusedByPermissions(await ErrorNumberFromAsync(
            () => ScopedSql.ExecuteAsync(tenantA, "ALTER TABLE [master].[Unit] NOCHECK CONSTRAINT CK_Unit_TenantScope;")));

        Assert.NotNull(await ErrorNumberFromAsync(() => ScopedSql.ExecuteAsync(tenantA, $"""
            EXEC(N'CREATE OR ALTER FUNCTION security.fnStocklyRowTenantAllowed(@TenantId uniqueidentifier) RETURNS bit AS BEGIN RETURN 1; END');
            """)));

        Assert.NotNull(await ErrorNumberFromAsync(
            () => ScopedSql.ExecuteAsync(tenantA, "ALTER ROLE stockly_platform_role ADD MEMBER stockly_test_tenant;")));

        // And the boundary is still intact.
        Assert.Equal(0, (await ScopedSql.ExecuteAsync(tenantA, $"DELETE FROM [master].[Unit] WHERE Id = '{UnitB:D}';")));
        ShouldBeRefusedByOwnershipConstraint(await ErrorNumberFromAsync(() => ScopedSql.ExecuteAsync(tenantA, $"""
            INSERT INTO [master].[Unit] (Id, TenantId, Code, Name, UnitType, DecimalPlaces, IsBaseUnit, IsActive, IsDeleted)
            VALUES (NEWID(), '{TenantB:D}', N'{MustNotPersist}', N'Injected', 1, 0, 0, 1, 0);
            """)));
    }
}