using System.Data.Common;
using Microsoft.Data.SqlClient;
using Stockly.StocklyDbTestSupport;

namespace Stockly.SecurityTests;

/// <summary>
/// Shared setup for the SQL Server tenant-isolation suites.
/// </summary>
/// <remarks>
/// <para>
/// <strong>One database, cleaned between tests.</strong> Every suite runs against the single
/// canonical <c>Stockly</c> database — the same one the application uses, so a passing suite
/// is evidence about the real schema rather than about a throwaway copy. Isolation between
/// tests therefore comes from the data, not from having a separate database per suite:
/// </para>
/// <list type="bullet">
/// <item>Each test seeds its own rows under its own GUIDs and asserts only about those rows,
/// so no test can pass because of another's data or fail because of it.</item>
/// <item>Each test deletes exactly those rows in <c>DisposeAsync</c>, so the database is left
/// as it was found whether the test passed, failed or threw.</item>
/// <item>Suites that mutate the same table take an exclusive engine-level lease
/// (<c>DatabaseLease</c>), which also serialises against a parallel <c>dotnet test</c>
/// project.</item>
/// </list>
/// <para>
/// Transactions-and-rollback is <em>not</em> used here, and the reason is structural rather
/// than a preference: these tests deliberately act through two different SQL logins — the
/// tenant principal that is the adversary, and the platform principal that verifies ground
/// truth — and a transaction cannot span connections. It could only cover one of them, which
/// would leave exactly the writes it is supposed to guard against outside the rollback.
/// </para>
/// </remarks>
public abstract class TenantIsolationTestBase(SqlServerFixture fixture) : IAsyncLifetime
{
    protected SqlServerFixture Fixture { get; } = fixture;

    /// <summary>Tenant A's id for this test.</summary>
    protected Guid TenantA { get; } = Guid.NewGuid();

    /// <summary>Tenant B's id for this test. The adversary in every cross-tenant attack.</summary>
    protected Guid TenantB { get; } = Guid.NewGuid();

    /// <summary>A unit row belonging to tenant A.</summary>
    protected Guid UnitA { get; } = Guid.NewGuid();

    /// <summary>A unit row belonging to tenant B.</summary>
    protected Guid UnitB { get; } = Guid.NewGuid();

    /// <summary>
    /// A SKU that must never persist. Used by the write attacks to assert that a rejected
    /// write left nothing behind rather than merely returning an error.
    /// </summary>
    /// <remarks>
    /// Unique per test rather than a fixed string: every test in a class shares the one
    /// database, so a fixed code would make the second test collide on
    /// <c>UX_Unit_TenantId_Code</c> and fail during setup for a reason that has nothing to do
    /// with isolation.
    /// </remarks>
    protected string MustNotPersist { get; } = $"MUST-NOT-PERSIST-{Guid.NewGuid():N}"[..32];

    /// <summary>
    /// A short token unique to this test, used to make seeded codes unique.
    /// <para>
    /// The tables carry unique indexes on <c>Code</c>, so a fixed code would collide between
    /// tests sharing the database and the failure would look like a product defect.
    /// </para>
    /// </summary>
    private static readonly string RunToken = Guid.NewGuid().ToString("N")[..8];

    /// <summary>Tenant A's unique code.</summary>
    protected string TenantACode { get; } = $"TENANT-A-{RunToken}";

    /// <summary>Tenant B's unique code.</summary>
    protected string TenantBCode { get; } = $"TENANT-B-{RunToken}";

    /// <summary>Tenant A's unit code.</summary>
    protected string UnitACode { get; } = $"A-BASE-{RunToken}";

    /// <summary>Tenant B's unit code — the adversary's row in every test.</summary>
    protected string UnitBCode { get; } = $"B-BASE-{RunToken}";

    /// <summary>The base unit a tenant inserts into its own tenant.</summary>
    protected string OwnUnitCode { get; } = $"A-OWN-{RunToken}";

    public virtual async Task InitializeAsync()
    {
        Fixture.RequireServer();
        await Fixture.EnsurePrincipalsAsync();
    }

    /// <summary>
    /// Removes exactly the rows this test seeded.
    /// <para>
    /// Deliberately not a truncate. The canonical database is the development database too,
    /// so "clear everything so the test starts clean" would destroy real local work. Deleting
    /// by primary key leaves every other row in the database untouched, and still guarantees
    /// the next test cannot see this one's data.
    /// </para>
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        await using var platform = await ScopedSql.OpenPlatformAsync(PlatformConnectionString);

        // Children before parents: identity.Tenant is referenced by master.Unit, and the
        // FK would reject the tenant delete while its units still exist.
        await ScopedSql.ExecuteAsync(platform, $"""
            DELETE FROM [master].[Unit]
            WHERE Id IN ('{UnitA:D}', '{UnitB:D}') OR Code IN (N'{MustNotPersist}', N'{OwnUnitCode}');
            """);

        await ScopedSql.ExecuteAsync(platform, $"""
            DELETE FROM [identity].[Tenant] WHERE Id IN ('{TenantA:D}', '{TenantB:D}');
            """);
    }

    /// <summary>
    /// Deletes an <c>ops.OutboxMessage</c> row created by a test.
    /// <para>
    /// The outbox table holds no FK to the tenants, so its rows are not removed by the tenant
    /// delete in <see cref="DisposeAsync"/> and have to be deleted explicitly. Leaving one behind
    /// would accumulate rows in the shared canonical database across runs, which is the kind of
    /// residue that makes a later run's row-count assertions fail for an unrelated reason.
    /// </para>
    /// </summary>
    protected async Task CleanupOutboxAsync(Guid outboxId)
    {
        await using var platform = await ScopedSql.OpenPlatformAsync(PlatformConnectionString);
        await ScopedSql.ExecuteAsync(platform, $"DELETE FROM [ops].[OutboxMessage] WHERE Id = '{outboxId:D}';");
    }

    /// <summary>The tenant principal's connection string, which is not an administrator.</summary>
    protected string TenantConnectionString => Fixture.TenantPrincipalConnectionString;

    /// <summary>The platform principal's connection string: a role member, not an admin.</summary>
    protected string PlatformConnectionString => Fixture.PlatformPrincipalConnectionString;

    /// <summary>
    /// Seeds one tenant and one unit for tenant A, and one for tenant B, using the
    /// <em>platform</em> principal.
    /// <para>
    /// Seeding must go through platform authority because a tenant principal cannot write
    /// another tenant's row -- which is the behaviour under test, and would otherwise make the
    /// fixture unable to set up its own adversarial state.
    /// </para>
    /// </summary>
    protected async Task SeedUnitsAsync()
    {
        await using var platform = await ScopedSql.OpenPlatformAsync(PlatformConnectionString);

        // The tenants must exist first: master.Unit.TenantId is a foreign key into
        // identity.Tenant.Id, so a unit row cannot be seeded against a tenant that does not
        // exist. identity.Tenant is one of the six global tables -- it has no TenantId of its
        // own -- which is exactly why it is reachable from platform scope and not from a
        // tenant scope.
        await ScopedSql.ExecuteAsync(platform, $"""
            INSERT INTO [identity].[Tenant] (Id, Code, Name, Status, TimeZoneId, DefaultCulture, DateFormat, IsDeleted)
            VALUES ('{TenantA:D}', N'{TenantACode}', N'Tenant A', 1, N'UTC', N'en-GB', N'dd/MM/yyyy', 0),
                   ('{TenantB:D}', N'{TenantBCode}', N'Tenant B', 1, N'UTC', N'en-GB', N'dd/MM/yyyy', 0);
            """);

        await ScopedSql.ExecuteAsync(platform, $"""
            INSERT INTO [master].[Unit] (Id, TenantId, Code, Name, UnitType, DecimalPlaces, IsBaseUnit, IsActive, IsDeleted)
            VALUES ('{UnitA:D}', '{TenantA:D}', N'{UnitACode}', N'Tenant A base unit', 1, 0, 1, 1, 0),
                   ('{UnitB:D}', '{TenantB:D}', N'{UnitBCode}', N'Tenant B base unit', 1, 0, 1, 1, 0);
            """);
    }

    /// <summary>
    /// The number of rows the platform principal can see with a given code. Used for
    /// ground-truth verification: the tenant principal's view and the true contents of the
    /// table are checked independently, so a test cannot be fooled by both being equally
    /// wrong.
    /// </summary>
    protected async Task<int> PlatformRowCountAsync(string code)
    {
        await using var platform = await ScopedSql.OpenPlatformAsync(PlatformConnectionString);
        return await ScopedSql.ScalarAsync<int>(platform, $"""
            SELECT COUNT(*) FROM [master].[Unit] WHERE Code = N'{code}';
            """);
    }

    /// <summary>Reads tenant B's unit name with platform authority, to prove it was untouched.</summary>
    protected async Task<string?> PlatformNameAsync(Guid unitId)
    {
        await using var platform = await ScopedSql.OpenPlatformAsync(PlatformConnectionString);
        return await ScopedSql.ScalarAsync<string?>(platform, $"""
            SELECT Name FROM [master].[Unit] WHERE Id = '{unitId:D}';
            """);
    }

    /// <summary>
    /// Runs <paramref name="action"/> and returns the SQL Server error number, or
    /// <see langword="null"/> when it succeeded.
    /// </summary>
    protected static Task<int?> ErrorNumberFromAsync(Func<Task> action) => ScopedSql.ErrorNumberFromAsync(action);

    /// <summary>Asserts a statement was refused by the ownership CHECK constraint.</summary>
    protected static void ShouldBeRefusedByOwnershipConstraint(int? errorNumber) =>
        Assert.True(
            errorNumber == ScopedSql.CheckConstraintViolationErrorNumber,
            $"Expected SQL Server error {ScopedSql.CheckConstraintViolationErrorNumber} "
            + "(CHECK constraint violation). Actual: "
            + (errorNumber is null ? "the statement SUCCEEDED, which is an isolation failure" : $"error {errorNumber}"));

    /// <summary>Asserts a statement was refused for lack of permission.</summary>
    protected static void ShouldBeRefusedByPermissions(int? errorNumber) =>
        Assert.True(
            errorNumber is ScopedSql.PermissionDeniedErrorNumber or 1088 or 229,
            $"Expected a permission-denied error (229, 1088). Actual: "
            + (errorNumber is null ? "the statement SUCCEEDED, which is a privilege escalation" : $"error {errorNumber}"));

    /// <summary>Opens a connection and disposes it deterministically.</summary>
    protected static async Task<T> WithConnectionAsync<T>(Task<SqlConnection> open, Func<SqlConnection, Task<T>> action)
    {
        await using var connection = await open;
        return await action(connection);
    }

    /// <summary>Opens a connection and runs a write, returning the affected row count.</summary>
    protected static async Task<int> WithConnectionAsync(Task<SqlConnection> open, Func<DbConnection, Task<int>> action)
    {
        await using var connection = await open;
        return await action(connection);
    }
}