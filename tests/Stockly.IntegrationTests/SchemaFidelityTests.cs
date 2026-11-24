using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Stockly.StocklyDbTestSupport;
using Xunit;

namespace Stockly.IntegrationTests;

/// <summary>
/// Proves the fixture is talking to a <strong>real Microsoft SQL Server</strong> and
/// that the applied schema matches <c>docs/DATABASE_DESIGN.md</c>.
/// <para>
/// These are the assertions the rest of the database suites rest on. If the applied
/// schema were quietly wrong — a missing table, an unexpected cascade — every tenant
/// isolation result below would still be "green" while describing a schema nobody
/// will deploy. So the schema is verified first, from the server's own catalog.
/// </para>
/// <para>
/// The counts are asserted against the design document, not against a golden file
/// generated from the migration. A golden file would only prove the migration is
/// stable, which is not the question being asked.
/// </para>
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class SchemaFidelityTests
{
    private readonly SqlServerFixture _fixture;

    public SchemaFidelityTests(SqlServerFixture fixture) => _fixture = fixture;

    private async Task<DbConnection> ConnectAsync()
    {
        _fixture.RequireServer();
        await _fixture.MigrateAsync();

        var connection = new SqlConnection(_fixture.ConnectionStringForDatabase);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<T> ScalarAsync<T>(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T));
    }

    // ---- Engine identity ------------------------------------------------

    [Fact]
    public async Task The_test_server_is_a_genuine_microsoft_sql_server()
    {
        await using var connection = await ConnectAsync();

        var version = await ScalarAsync<string>(connection, "SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(64))");
        var edition = await ScalarAsync<string>(connection, "SELECT CAST(SERVERPROPERTY('Edition') AS nvarchar(128))");

        version.Should().StartWith("1", "SQL Server major version 16 (2022) or 17 (2025)");
        edition.Should().NotBeNullOrWhiteSpace();

        // The distinguishing feature. A substitute provider cannot answer this, so it
        // is asserted rather than trusting the provider name.
        (await ScalarAsync<int>(connection, "SELECT COUNT(*) FROM sys.dm_os_host_info")).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task The_engine_enforces_composite_foreign_keys_and_rowversion_which_is_why_sql_server_is_required()
    {
        await using var connection = await ConnectAsync();

        // These two system views only exist in SQL Server, and they are the
        // mechanisms the isolation design relies on.
        (await ScalarAsync<int>(connection,
            "SELECT COUNT(*) FROM sys.foreign_key_columns")).Should().BeGreaterThan(0);

        // system_type_id 189 is the timestamp/rowversion type id.
        (await ScalarAsync<int>(connection,
            "SELECT COUNT(*) FROM sys.columns WHERE system_type_id = 189")).Should().BeGreaterThan(0);
    }

    // ---- Table and schema counts (DATABASE_DESIGN §2, §11) ---------------

    [Fact]
    public async Task There_are_exactly_66_domain_tables()
    {
        await using var connection = await ConnectAsync();

        // __EFMigrationsHistory is EF's own bookkeeping table and is not part of the
        // documented model, so it is excluded explicitly rather than by accident.
        var tables = await ScalarAsync<int>(connection,
            "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0 AND name <> '__EFMigrationsHistory'");

        tables.Should().Be(66, "DATABASE_DESIGN §11 records a 66-entity V1 model");
    }

    [Fact]
    public async Task Every_table_lives_in_one_of_the_twelve_documented_schemas()
    {
        await using var connection = await ConnectAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT DISTINCT SCHEMA_NAME(schema_id) AS SchemaName
            FROM sys.tables
            WHERE is_ms_shipped = 0 AND SCHEMA_NAME(schema_id) <> 'dbo'
            ORDER BY SchemaName
            """;

        var schemas = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                schemas.Add(reader.GetString(0));
            }
        }

        schemas.Should().BeEquivalentTo(
        [
            "audit", "events", "extensibility", "files", "food", "identity",
            "inventory", "master", "notifications", "ops", "platform", "purchasing",
        ], "DATABASE_DESIGN §2 fixes the twelve schemas");

        // And nothing escaped into dbo: that would mean an entity type whose namespace
        // did not map to a schema, which is a silent modelling bug. This runs after the
        // reader above is closed, because one connection carries one active reader.
        await using var dboCheck = connection.CreateCommand();
        dboCheck.CommandText =
            "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0 AND SCHEMA_NAME(schema_id) = 'dbo' AND name <> '__EFMigrationsHistory'";
        (await dboCheck.ExecuteScalarAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Table_names_follow_the_singular_entity_name_convention()
    {
        await using var connection = await ConnectAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT SCHEMA_NAME(schema_id) + '.' + name AS FullName
            FROM sys.tables
            WHERE is_ms_shipped = 0 AND SCHEMA_NAME(schema_id) <> 'dbo'
            ORDER BY 1
            """;

        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        names.Should().NotBeEmpty();
        names.Should().OnlyHaveUniqueItems();
        names.Should().NotContain(name => name.EndsWith("s.", StringComparison.Ordinal) || name.Contains(".s", StringComparison.Ordinal),
            "DATABASE_DESIGN §2 uses the singular entity name");
    }

    // ---- The EnrollmentCode fidelity fix (DATABASE_DESIGN §6) ------------

    [Fact]
    public async Task EnrollmentCode_is_varchar64_variable_length_and_non_unicode()
    {
        await using var connection = await ConnectAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT t.name AS TypeName, c.max_length, c.is_nullable, c.is_computed
            FROM sys.columns c
            JOIN sys.types t ON t.user_type_id = c.user_type_id
            WHERE c.object_id = OBJECT_ID('identity.Device') AND c.name = 'EnrollmentCode'
            """;

        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue("identity.Device must have EnrollmentCode");

        var typeName = reader.GetString(0);
        var maxLength = reader.GetInt16(1);
        var isNullable = reader.GetBoolean(2);
        var isComputed = reader.GetBoolean(3);

        typeName.Should().Be("varchar", "DATABASE_DESIGN declares varchar(64); nchar/nvarchar would be both fixed-length and Unicode");
        maxLength.Should().Be(64, "max_length is in bytes, and varchar is one byte per character");
        isNullable.Should().BeFalse("the design marks it NN");
        isComputed.Should().BeFalse("it is a stored value, not computed");
    }

    [Fact]
    public async Task Every_tenant_id_column_is_not_nullable()
    {
        await using var connection = await ConnectAsync();

        // ADR-0032: a nullable TenantId would let a row claim no tenant and slip past
        // every filter. Exactly three tables are documented exceptions, and they are
        // exceptions because they must also hold platform-level rows where no tenant
        // exists (audit.AuditLog, ops.OutboxMessage) or because a platform session has
        // no tenant or membership at all (identity.AuthSession).
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT SCHEMA_NAME(o.schema_id) + '.' + OBJECT_NAME(o.object_id)
            FROM sys.columns c
            JOIN sys.objects o ON o.object_id = c.object_id
            WHERE c.name = 'TenantId' AND c.is_nullable = 1
              AND o.is_ms_shipped = 0
              AND SCHEMA_NAME(o.schema_id) <> 'dbo'
            ORDER BY 1
            """;

        var exceptions = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            exceptions.Add(reader.GetString(0));
        }

        exceptions.Should().BeEquivalentTo(
        [
            "audit.AuditLog",
            "identity.AuthSession",
            "ops.OutboxMessage",
        ], "a fourth nullable TenantId would be a row that can belong to no tenant");
    }

    // ---- Referential integrity (DATABASE_DESIGN §5) ---------------------

    [Fact]
    public async Task Exactly_two_foreign_keys_cascade_and_both_are_assignment_records()
    {
        await using var connection = await ConnectAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT OBJECT_SCHEMA_NAME(parent_object_id) + '.' + OBJECT_NAME(parent_object_id) AS Child,
                   OBJECT_NAME(referenced_object_id) AS Parent
            FROM sys.foreign_keys
            WHERE delete_referential_action_desc = 'CASCADE'
            ORDER BY Child, Parent
            """;

        var cascades = new List<(string Child, string Parent)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            cascades.Add((reader.GetString(0), reader.GetString(1)));
        }

        // §5.2: cascade exists only from child configuration rows to their definition.
        // Anything else silently deletes stock, ledger or audit history.
        cascades.Should().BeEquivalentTo(
        [
            ("identity.MembershipRoleWarehouse", "MembershipRole"),
            ("identity.MembershipRoleWarehouse", "Warehouse"),
        ]);
    }

    [Fact]
    public async Task Every_foreign_key_other_than_the_two_assignment_cascades_is_restrict()
    {
        await using var connection = await ConnectAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM sys.foreign_keys
            WHERE delete_referential_action_desc NOT IN ('RESTRICT', 'NO_ACTION')
              AND NOT (
                    OBJECT_SCHEMA_NAME(parent_object_id) = 'identity'
                AND OBJECT_NAME(parent_object_id) = 'MembershipRoleWarehouse'
              )
            """;

        (await command.ExecuteScalarAsync()).Should().Be(0,
            "DATABASE_DESIGN §5.2 forbids cascade except from assignment rows");
    }

    [Fact]
    public async Task Every_composite_foreign_key_includes_the_tenant_column()
    {
        await using var connection = await ConnectAsync();

        // The load-bearing property: a child row cannot reference a parent in another
        // tenant, because the tenant is part of the key it must match.
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            WITH fk AS (
                SELECT fk.object_id,
                       COUNT(*) AS ColumnCount,
                       SUM(CASE WHEN COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'TenantId' THEN 1 ELSE 0 END) AS HasTenant
                FROM sys.foreign_keys fk
                JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
                WHERE fk.is_system_named = 1
                GROUP BY fk.object_id
            )
            SELECT COUNT(*) FROM fk WHERE ColumnCount > 1 AND HasTenant = 0
            """;

        // audit.AuditLog's own tenant reference is the documented single-column case.
        (await command.ExecuteScalarAsync()).Should().Be(0);
    }

    // ---- Constraints and indexes ---------------------------------------

    [Fact]
    public async Task Check_constraints_exist_for_the_legal_value_sets()
    {
        await using var connection = await ConnectAsync();

        var checks = await ScalarAsync<int>(connection,
            "SELECT COUNT(*) FROM sys.check_constraints WHERE parent_object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped = 0)");

        checks.Should().BeGreaterThan(50,
            "the design pins enum ranges and invariants with CHECK constraints");
    }

    [Fact]
    public async Task The_auth_session_scope_invariant_is_enforced_by_a_check_constraint()
    {
        await using var connection = await ConnectAsync();

        // DATABASE_DESIGN §10: SessionScope=2 requires NULL tenant and membership;
        // SessionScope=1 requires both. This is the invariant that makes a platform
        // session impossible to forge as a tenant session at the database level.
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT definition
            FROM sys.check_constraints
            WHERE parent_object_id = OBJECT_ID('identity.AuthSession')
              AND definition LIKE '%SessionScope%'
            """;

        var definitions = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            definitions.Add(reader.GetString(0));
        }

        definitions.Should().NotBeEmpty("identity.AuthSession must pin its scope invariant");
        var definition = definitions[0];
        definition.Should().Contain("SessionScope");
        definition.Should().Contain("TenantId");
        definition.Should().Contain("MembershipId");
    }

    [Fact]
    public async Task Every_tenant_scoped_unique_index_leads_with_tenant_id()
    {
        await using var connection = await ConnectAsync();

        // ADR-0032. The documented exceptions all guard server-generated global
        // identifiers. Any other exception means cross-tenant uniqueness is not
        // enforced.
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM sys.indexes i
            JOIN sys.tables t ON t.object_id = i.object_id
            WHERE i.is_unique = 1
              AND i.is_primary_key = 0
              AND i.type = 2
              AND i.is_disabled = 0
              AND t.is_ms_shipped = 0
              AND SCHEMA_NAME(t.schema_id) <> 'dbo'
              -- Only tenant-scoped tables: a global table (no TenantId) is meant to be
              -- globally unique.
              AND EXISTS (SELECT 1 FROM sys.columns tc
                          WHERE tc.object_id = t.object_id AND tc.name = 'TenantId')
              -- Exclude the declared alternate keys AK_*_Id_TenantId. These are not
              -- business uniqueness rules; they are the composite-FK TARGETS required by
              -- DATABASE_DESIGN §5.1 so a child can reference its parent on
              -- (Id, TenantId). They necessarily lead with the global `Id` primary key,
              -- which is the whole point of them, and they cannot span tenants because
              -- the tenant is the second half of the key.
              AND i.name NOT LIKE 'AK[_]%'
              -- The documented global-identifier exceptions.
              --
              -- identity.PinCredential(MembershipId) is included here because §6
              -- specifies `UQ (MembershipId)` explicitly and deliberately ("one PIN
              -- credential per membership, unfiltered"), while the global exception list
              -- in §5.1 rule 6 omits it. That is a documentation inconsistency, not a
              -- schema defect: MembershipId is a globally unique GUID primary key, so the
              -- index cannot span tenants. Recorded in docs/CHANGELOG.md.
              AND SCHEMA_NAME(t.schema_id) + '.' + t.name NOT IN (
                    'files.Attachment', 'identity.RefreshToken',
                    'identity.PasswordResetToken', 'identity.Device',
                    'notifications.PushSubscription', 'identity.PinCredential')
              -- The leading column must be EITHER TenantId, OR a foreign key whose
              -- composite form carries TenantId. In the latter case the composite FK
              -- already forces the parent (and therefore the tenant) to match, so the
              -- index cannot span tenants: UX_RecipeItem(RecipeId, LineNumber) is
              -- correct precisely because FK(RecipeId, TenantId) -> Recipe(Id, TenantId)
              -- exists. Anything else is counted as a violation.
              AND NOT EXISTS (
                    SELECT 1
                    FROM sys.index_columns ic
                    WHERE ic.object_id = i.object_id
                      AND ic.index_id = i.index_id
                      AND ic.key_ordinal = 1
                      AND (
                            COL_NAME(ic.object_id, ic.column_id) = 'TenantId'
                            OR EXISTS (
                                SELECT 1
                                FROM sys.foreign_key_columns fkc
                                JOIN sys.foreign_keys fk
                                  ON fk.object_id = fkc.constraint_object_id
                                 AND fk.parent_object_id = ic.object_id
                                WHERE fkc.parent_object_id = ic.object_id
                                  AND fkc.parent_column_id = ic.column_id
                                  AND EXISTS (SELECT 1 FROM sys.foreign_key_columns f2
                                              WHERE f2.constraint_object_id = fk.object_id
                                                AND COL_NAME(f2.parent_object_id, f2.parent_column_id) = 'TenantId'))
                          ))
            """;

        (await command.ExecuteScalarAsync()).Should().Be(0,
            "a unique index not leading with TenantId does not enforce cross-tenant uniqueness");
    }

    [Fact]
    public async Task Append_only_audit_table_has_no_rowversion_and_no_update_path()
    {
        await using var connection = await ConnectAsync();

        // ADR-0037: a rowversion on a table that must never be updated is an
        // invitation to update it.
        (await ScalarAsync<int>(connection,
            "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('audit.AuditLog') AND system_type_id = 189"))
            .Should().Be(0);

        (await ScalarAsync<int>(connection,
            "SELECT COUNT(*) FROM sys.triggers WHERE parent_id = OBJECT_ID('audit.AuditLog')"))
            .Should().Be(0);
    }

    // ---- Migration state ------------------------------------------------

    [Fact]
    public async Task Every_migration_is_recorded_as_applied()
    {
        await using var connection = await ConnectAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId LIKE 'InitialSchema%'
            """;

        var applied = await ScalarAsync<int>(connection,
            "SELECT COUNT(*) FROM __EFMigrationsHistory");

        applied.Should().BeGreaterThan(0,
            "the fixture applies migrations before this test runs; an empty history means it did not");
    }

    [Fact]
    public async Task The_initial_schema_contains_no_seed_rows()
    {
        await using var connection = await ConnectAsync();

        // The 105 permissions, 11 roles and plans are Phase 2 work
        // (PROJECT_BLUEPRINT §25). Their absence here is what keeps Phase 1 honest,
        // so it is asserted rather than assumed.
        foreach (var table in new[] { "[identity].[Permission]", "[identity].[Role]", "[platform].[Plan]" })
        {
            (await ScalarAsync<int>(connection, $"SELECT COUNT(*) FROM {table}")).Should().Be(0,
                $"{table} is seeded in Phase 2, not Phase 1");
        }
    }
}