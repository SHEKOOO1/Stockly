using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Stockly.Infrastructure.Persistence;
using Stockly.Infrastructure.Persistence.Security;
using Xunit;

namespace Stockly.StocklyDbTestSupport;

/// <summary>
/// A <strong>real Microsoft SQL Server instance</strong> and the one canonical
/// <c>Stockly</c> database, for the database-dependent suites.
/// </summary>
/// <remarks>
/// <para>
/// SQL Server is the authoritative engine for Stockly (PROJECT_BLUEPRINT §25.1,
/// DATABASE_DESIGN §1). The properties these suites verify — composite foreign keys,
/// <c>RESTRICT</c> versus <c>CASCADE</c> delete behaviour, <c>CHECK</c> constraints,
/// transaction rollback, <c>rowversion</c> concurrency — are implemented by
/// SQL Server's engine. Therefore there is deliberately
/// <strong>no in-memory, SQLite or mocked substitute anywhere in this project</strong>.
/// An in-memory provider would either not enforce those properties or enforce
/// different ones, so a suite passing against it would prove nothing about production
/// while appearing green. That is worse than a failing suite, because it removes the
/// signal.
/// </para>
/// <para>
/// <strong>One database, not one per suite.</strong> Every DB-dependent test runs against
/// the single canonical <see cref="CanonicalDatabaseName"/> database — the same one the
/// application uses. Earlier revisions created a <c>Stockly_Phase1_&lt;Suite&gt;</c>
/// database per test class. That produced a dozen permanent near-copies of the schema on
/// every developer machine, and it meant the suites were validating a copy rather than
/// the real thing: a schema defect introduced by a migration would be invisible to every
/// suite but the one whose copy happened to be recreated. Isolation between tests is
/// therefore achieved by data, not by duplication — see
/// <see cref="DatabaseLease"/> for how concurrent assemblies are kept apart, and
/// <c>TestData</c> for how each test cleans up after itself.
/// </para>
/// <para>
/// The server is supplied by the <c>STOCKLY_TEST_SQLSERVER</c> environment variable
/// (see <c>.env.example</c>). The value is a secret and is never committed, never
/// logged, and never included in a failure message — only the variable name is.
/// </para>
/// <para>
/// When the variable is unset or unusable the fixture records
/// <see cref="UnavailableReason"/> and <see cref="RequireServer"/> throws. The tests
/// then <strong>fail</strong>. They are never skipped: in a CI summary a skip is
/// indistinguishable from a pass, and silently skipping a tenant-isolation suite is
/// exactly the failure mode AGENTS.md §4 exists to prevent.
/// </para>
/// </remarks>
public sealed class SqlServerFixture : IAsyncLifetime
{
    /// <summary>Environment variable naming the real SQL Server instance.</summary>
    public const string ConnectionStringVariable = "STOCKLY_TEST_SQLSERVER";

    /// <summary>
    /// The one and only Stockly database name.
    /// <para>
    /// Hard-coded deliberately. A per-suite or per-run database name is what produced a
    /// dozen permanent schema copies on developer machines, and it is also how a test ends
    /// up proving something about a database the application never runs against. The same
    /// name is used in development, in test and in production, which is what makes a green
    /// suite evidence about the real schema.
    /// </para>
    /// </remarks>
    public const string CanonicalDatabaseName = "Stockly";

    private readonly List<StocklyDbContext> _contexts = [];

    private string? _connectionString;
    private SqlPrincipalProvisioner? _principals;
    private DatabaseLease? _lease;
    private SqlPrincipalProvisioner.Principals? _provisioned;

    /// <summary>
    /// Whether a context with no tenant should get the DELIBERATELY UNFILTERED platform
    /// scope of ADR-0034, or the fail-closed anonymous scope.
    /// <para>
    /// These are very different and must never be conflated. <c>null</c> means both
    /// "no session" and "platform operator", and guessing wrong in either direction is
    /// a security bug:
    /// </para>
    /// <list type="bullet">
    /// <item>Anonymous-as-platform: an unauthenticated context sees every tenant's rows.
    /// A filter that is <em>absent</em> is the worst possible outcome.</item>
    /// <item>Platform-as-anonymous: a legitimate platform operator sees nothing and
    /// every cross-tenant admin feature silently returns empty.</item>
    /// </list>
    /// <para>
    /// So the caller must say which it means. Nothing infers it from a null tenant id.
    /// </para>
    /// </summary>
    public bool TreatNullTenantAsPlatformScope { get; init; }

    /// <summary>Whether a real SQL Server was found and accepted the connection.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>
    /// Why no server is available, for the failure message. Never contains a
    /// connection string.
    /// </summary>
    public string? UnavailableReason { get; private set; }

    /// <summary>
    /// The live connection string, pointed at <see cref="CanonicalDatabaseName"/>.
    /// Throws when no server is available.
    /// </summary>
    public string ConnectionString
    {
        get
        {
            _ = _connectionString
                ?? throw new InvalidOperationException(
                    "No SQL Server is available for the database-dependent suites. "
                    + (UnavailableReason ?? string.Empty));

            // The database segment is CanonicalDatabaseName, NOT the configured connection
            // string. Passing the configured string here is a silent, self-inflicted failure:
            // SqlConnectionStringBuilder assigns it to Initial Catalog verbatim, so every
            // suite builds a brand-new database whose NAME is the whole connection string,
            // migrations are applied to that empty copy, and every assertion passes against a
            // schema nothing will ever deploy. It stays green while the canonical database is
            // never touched -- a false pass on the exact guarantee (TESTING_STRATEGY 1.5,
            // AGENTS.md 4) that makes these suites worth running.
            return BuildConnectionString(CanonicalDatabaseName);
        }
    }

    /// <summary>
    /// Creates a context bound to a tenant, or to platform scope when
    /// <paramref name="tenantId"/> is <see langword="null"/>.
    /// </summary>
    public StocklyDbContext CreateContext(Guid? tenantId, Guid? membershipId = null)
    {
        var platformScope = tenantId is null;
        var tenantContext = new FixedTenantContext(tenantId, membershipId, platformScope);

        var options = new DbContextOptionsBuilder<StocklyDbContext>()
            .UseSqlServer(
                ConnectionStringForScope(platformScope),
                sql =>
                {
                    // Retries are correct for production and wrong for a test that is
                    // deliberately provoking a concurrency conflict: a retry would hide
                    // the conflict being asserted. Suites that need to observe a
                    // conflict disable this by building their own context.
                    sql.EnableRetryOnFailure();
                })
            // The same interceptor the API registers, over the same FixedTenantContext the
            // test chose. A fixture that built its own scope mechanism would let the test
            // suite pass while the running application set no scope at all -- which is
            // precisely the defect this line was added to close.
            .AddInterceptors(new TenantScopeConnectionInterceptor(tenantContext))
            // Same reason, one level down: the query filters capture the scope, so the model
            // has to be cached per scope or the first context decides the filter for all.
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantScopeModelCacheKeyFactory>()
            .Options;

        var context = new StocklyDbContext(options, tenantContext);
        _contexts.Add(context);
        return context;
    }

    /// <summary>
    /// The connection string a context of the given scope must use.
    /// <para>
    /// Platform authority is a database <em>role membership</em>, not a flag in the session: the
    /// predicates accept <c>StocklyScope = 2</c> only when
    /// <c>IS_MEMBER('stockly_platform_role') = 1</c>. The fixture's own connection is an
    /// administrator and is deliberately NOT a member of that role, so a platform-scope context
    /// built on it has every write refused by <c>CK_*_TenantScope</c>. Platform-scope contexts
    /// therefore connect as the provisioned platform principal, and every other scope connects
    /// as the provisioned tenant principal -- which is also the least privilege available.
    /// </para>
    /// </summary>
    private string ConnectionStringForScope(bool platformScope) =>
        platformScope ? PlatformPrincipalConnectionString : TenantPrincipalConnectionString;

    /// <summary>
    /// Discards every tracked change and starts a new unit of work, so one test's
    /// writes cannot reach another's assertions.
    /// </summary>
    public async Task<StocklyDbContext> FreshContextAsync(
        Guid? tenantId,
        Guid? membershipId = null)
    {
        await EnsurePrincipalsAsync();

        foreach (var context in _contexts)
        {
            await context.DisposeAsync();
        }

        _contexts.Clear();
        return CreateContext(tenantId, membershipId);
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable(ConnectionStringVariable);

        if (string.IsNullOrWhiteSpace(configured))
        {
            UnavailableReason =
                $"{ConnectionStringVariable} is not set, so no real SQL Server instance is "
                + "available. Set it to a SQL Server connection string (SQL Server Express "
                + "is sufficient and is a genuine Microsoft SQL Server engine). Substituting "
                + "SQLite, PostgreSQL, an in-memory provider or a mock is NOT permitted: "
                + "the constraints under test are enforced by the SQL Server engine, so a "
                + "different engine would not test them.";
            return;
        }

        if (!await IsRealSqlServerAsync(configured))
        {
            UnavailableReason =
                $"{ConnectionStringVariable} is set but the target did not answer as a Microsoft "
                + "SQL Server instance.";
            return;
        }

        _connectionString = configured;
        IsAvailable = true;

        // Serialise every DB-backed suite on this instance before touching any data. The lock
        // is held on a dedicated session for the lifetime of this fixture.
        _lease = await DatabaseLease.AcquireAsync(BuildConnectionString("master"));

        await EnsureDatabaseAsync(CanonicalDatabaseName);
        await MigrateAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            await context.DisposeAsync();
        }

        _contexts.Clear();

        if (_principals is not null)
        {
            await _principals.DisposeAsync();
        }

        // Released last: the principals' cleanup statements are writes, and must finish
        // before another suite is allowed to start mutating this database.
        if (_lease is not null)
        {
            await _lease.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Throws unless a real SQL Server is available. Called at the top of every
    /// database-dependent test so an unrunnable suite reports a failure with a reason
    /// rather than passing quietly.
    /// </summary>
    public void RequireServer()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                "BLOCKED: this test requires a real Microsoft SQL Server instance. "
                + (UnavailableReason ?? string.Empty));
        }
    }

    /// <summary>
    /// Applies every pending migration to <see cref="CanonicalDatabaseName"/> and returns
    /// whether the schema changed.
    /// <para>
    /// Safe to call repeatedly and from several assemblies: Entity Framework takes its own
    /// migration lock, and <see cref="DatabaseLease"/> serialises the callers.
    /// </para>
    /// </summary>
    public async Task<bool> MigrateAsync()
    {
        RequireServer();

        var options = new DbContextOptionsBuilder<StocklyDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var context = new StocklyDbContext(
            options,
            new FixedTenantContext(null, null, isPlatformScope: true));

        var pending = (await context.Database.GetPendingMigrationsAsync()).ToArray();
        if (pending.Length == 0)
        {
            return false;
        }

        await context.Database.MigrateAsync();
        return true;
    }

    /// <summary>
    /// A context bound to <see cref="CanonicalDatabaseName"/>, already migrated.
    /// Use this instead of <see cref="CreateContext"/> when the test needs the schema to
    /// exist.
    /// </summary>
    /// <param name="tenantId">The tenant to scope to, or <see langword="null"/>.</param>
    /// <param name="membershipId">The acting membership, where the scenario needs one.</param>
    /// <param name="isPlatformScope">
    /// Explicit platform authority. Must be stated rather than inferred: see
    /// <see cref="TreatNullTenantAsPlatformScope"/>.
    /// </param>
    public async Task<StocklyDbContext> MigratedContextAsync(
        Guid? tenantId = null,
        Guid? membershipId = null,
        bool? isPlatformScope = null)
    {
        RequireServer();
        await MigrateAsync();
        await EnsurePrincipalsAsync();

        // ADR-0034 platform scope is EXPLICIT. A null tenant id alone does not grant
        // it, because the same null is what an unauthenticated request produces.
        var platform = isPlatformScope
            ?? (tenantId is null && TreatNullTenantAsPlatformScope);

        var options = new DbContextOptionsBuilder<StocklyDbContext>()
            .UseSqlServer(ConnectionStringForScope(platform))
            // Same interceptor the API registers -- see CreateContext. Without it the context
            // opens an unscoped session and the tenant-scope CHECK constraints refuse every
            // write, which reads as a schema bug rather than as a missing scope.
            .AddInterceptors(new TenantScopeConnectionInterceptor(new FixedTenantContext(tenantId, membershipId, platform)))
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantScopeModelCacheKeyFactory>()
            .Options;

        var context = new StocklyDbContext(
            options,
            new FixedTenantContext(tenantId, membershipId, platform));

        _contexts.Add(context);
        return context;
    }

    /// <summary>
    /// A <em>platform-authority</em> context: no tenant, and deliberately unfiltered.
    /// Used for cross-tenant administration and for test setup.
    /// </summary>
    public Task<StocklyDbContext> PlatformContextAsync() =>
        MigratedContextAsync(tenantId: null, membershipId: null, isPlatformScope: true);

    /// <summary>
    /// An <em>unauthenticated</em> context: no tenant and NOT platform scope, so every
    /// tenant-scoped query is pinned to <c>TenantId == null</c> and matches nothing.
    /// </summary>
    public Task<StocklyDbContext> AnonymousContextAsync() =>
        MigratedContextAsync(tenantId: null, membershipId: null, isPlatformScope: false);

    /// <summary>
    /// The tenant principal: a non-administrator SQL login holding DML on the tenant
    /// tables and deliberately <strong>not</strong> a member of
    /// <c>stockly_platform_role</c>.
    /// <para>
    /// This is what ordinary application traffic looks like to the database, and it is the
    /// principal every isolation assertion runs as. A suite that instead ran as
    /// <c>dbo</c> or <c>sysadmin</c> would prove nothing about the boundary, so if it cannot
    /// be provisioned the suites <strong>fail</strong> rather than falling back.
    /// </para>
    /// </summary>
    public async Task<string> EnsureTenantPrincipalAsync()
    {
        RequireServer();
        return (await EnsurePrincipalsAsync()).Tenant;
    }

    /// <summary>
    /// The platform principal: the same DML plus membership of
    /// <c>stockly_platform_role</c>. Used only for seeding, ground-truth verification and
    /// the constraint re-validation procedure.
    /// </summary>
    public async Task<string> EnsurePlatformPrincipalAsync()
    {
        RequireServer();
        return (await EnsurePrincipalsAsync()).Platform;
    }

    /// <summary>
    /// The tenant principal for the canonical database, once
    /// <see cref="EnsurePrincipalsAsync"/> has run.
    /// <para>
    /// Synchronous on purpose: it is a lookup of work initialisation already did, and a test
    /// that skipped initialisation gets an explicit error instead of a connection string
    /// pointing at a database with no user in it.
    /// </para>
    /// </summary>
    public string TenantPrincipalConnectionString => RequireProvisionedPrincipals().Tenant;

    /// <summary>The platform principal for the canonical database.</summary>
    public string PlatformPrincipalConnectionString => RequireProvisionedPrincipals().Platform;

    /// <summary>
    /// Creates the two least-privilege SQL principals in
    /// <see cref="CanonicalDatabaseName"/>, applying migrations first if needed.
    /// </summary>
    public async Task<SqlPrincipalProvisioner.Principals> EnsurePrincipalsAsync()
    {
        RequireServer();
        await MigrateAsync();

        _provisioned ??= await RequireProvisioner().GetAsync(CanonicalDatabaseName);
        return _provisioned;
    }

    private SqlPrincipalProvisioner.Principals RequireProvisionedPrincipals()
    {
        if (_provisioned is not null)
        {
            return _provisioned;
        }

        throw new InvalidOperationException(
            $"No SQL principals have been provisioned for {CanonicalDatabaseName}. "
            + "Call EnsurePrincipalsAsync (or the Ensure*PrincipalAsync helpers) during "
            + "initialisation before using TenantPrincipalConnectionString or "
            + "PlatformPrincipalConnectionString. Substituting an administrator connection "
            + "string is NOT permitted: the isolation tests prove nothing when run as one.");
    }

    private SqlPrincipalProvisioner RequireProvisioner()
    {
        if (_principals is not null)
        {
            return _principals;
        }

        // Operator-supplied principals win, so an environment that cannot create logins
        // can still run the suites.
        var suppliedTenant = Environment.GetEnvironmentVariable(SqlPrincipalProvisioner.TenantPrincipalVariable);
        var suppliedPlatform = Environment.GetEnvironmentVariable(SqlPrincipalProvisioner.PlatformPrincipalVariable);

        _principals = new SqlPrincipalProvisioner(ConnectionString);

        if (!string.IsNullOrWhiteSpace(suppliedTenant) && !string.IsNullOrWhiteSpace(suppliedPlatform))
        {
            _principals.UseSupplied(suppliedTenant, suppliedPlatform);
        }

        return _principals;
    }

    /// <summary>
    /// A connection string pointing at <see cref="CanonicalDatabaseName"/>.
    /// Used by suites that need raw <see cref="SqlConnection"/> access to assert
    /// engine behaviour (cascade rules, transaction rollback, isolation levels) that
    /// EF cannot express.
    /// </summary>
    public string ConnectionStringForDatabase => ConnectionString;

    /// <summary>
    /// Rewrites only the <c>Database</c> segment of the configured connection string,
    /// so the instance, credentials and encryption settings are inherited exactly.
    /// </summary>
    private string BuildConnectionString(string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(_connectionString) { InitialCatalog = databaseName };
        return builder.ToString();
    }

    private async Task EnsureDatabaseAsync(string databaseName)
    {
        var masterBuilder = new SqlConnectionStringBuilder(_connectionString) { InitialCatalog = "master" };

        await using (var connection = new SqlConnection(masterBuilder.ToString()))
        {
            await connection.OpenAsync();

            // CREATE DATABASE cannot be parameterised, cannot run inside a
            // transaction, and cannot be issued with a parameterised EXEC. The name is
            // therefore built in the client and embedded — which is only safe because it is
            // the compile-time constant CanonicalDatabaseName, not a runtime value, and is
            // bracketed here.
            var quoted = "[" + databaseName.Replace("]", "]]", StringComparison.Ordinal) + "]";

            await using var command = connection.CreateCommand();
            command.CommandText =
                $"IF DB_ID(N'{databaseName.Replace("'", "''", StringComparison.Ordinal)}') IS NULL EXEC(N'CREATE DATABASE {quoted.Replace("'", "''", StringComparison.Ordinal)}');";
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task<bool> IsRealSqlServerAsync(string connectionString)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();

            // Confirms this is genuinely Microsoft SQL Server and not a
            // look-alike or a substituted provider.
            command.CommandText = "SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(64))";
            var version = await command.ExecuteScalarAsync() as string;
            return !string.IsNullOrWhiteSpace(version);
        }
        catch (SqlException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            // A malformed connection string is not a server.
            return false;
        }
    }
}

/// <summary>
/// An exclusive, cross-process lease over the single canonical Stockly database.
/// </summary>
/// <remarks>
/// <para>
/// Sharing one database means every DB-backed suite must take turns, and
/// <c>dotnet test</c> runs test <em>projects</em> in parallel — in separate processes. An
/// in-process xUnit collection lock cannot see those, so without something at the database
/// level two assemblies would truncate and reseed each other's rows mid-test and produce
/// failures that look like isolation defects.
/// </para>
/// <para>
/// <c>sp_getapplock</c> is the right tool because it is enforced by the engine: the lock
/// survives across processes, and a session-scoped lock is released automatically if a test
/// host crashes, so a killed run cannot wedge the instance for good.
/// </para>
/// </remarks>
public sealed class DatabaseLease : IAsyncDisposable
{
    /// <summary>
    /// The lock resource name. Instance-wide, so it also serialises two separate clones of
    /// this repository pointed at the same SQL Server.
    /// </summary>
    private const string ResourceName = "Stockly.DatabaseDependentSuites";

    private readonly SqlConnection _connection;

    private DatabaseLease(SqlConnection connection) => _connection = connection;

    /// <summary>
    /// Acquires the lease, waiting for any other suite to release it.
    /// </summary>
    /// <param name="masterConnectionString">
    /// A connection string for the <c>master</c> database. The lock is taken there so it
    /// exists even before the canonical database does.
    /// </param>
    /// <param name="timeoutSeconds">
    /// How long to wait. Long, because a legitimate suite run takes minutes; failing fast
    /// would turn "another suite is running" into a confusing test failure.
    /// </param>
    public static async Task<DatabaseLease> AcquireAsync(
        string masterConnectionString,
        int timeoutSeconds = 900)
    {
        var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandTimeout = timeoutSeconds + 30;

            // sp_getapplock's @LockTimeout is measured in MILLISECONDS, not seconds. Passing
            // timeoutSeconds straight through meant a 900-second intent was a 900-millisecond
            // wait, so two database-dependent suites run by `dotnet test` in parallel --
            // separate processes, so an in-process lock cannot see them -- raced, and the
            // loser failed every test in the suite with "returned -1" about a second in. That
            // looked like an isolation defect and was nothing of the kind: the suites simply
            // never waited for each other.
            var lockTimeoutMilliseconds = checked(timeoutSeconds * 1000);

            // @LockOwner = 'Session' ties the lock to this connection, so closing it --
            // including on a crash -- releases it. @LockMode = 'Exclusive' is what makes two
            // suites mutually exclusive rather than merely both permitted.
            command.CommandText =
                "DECLARE @result int; " +
                "EXEC @result = sys.sp_getapplock " +
                "@Resource = N'Stockly.DatabaseDependentSuites', " +
                "@LockMode = 'Exclusive', @LockOwner = 'Session', " +
                $"@LockTimeout = {lockTimeoutMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}; " +
                "SELECT @result;";

            var result = await command.ExecuteScalarAsync();
            var code = result is int i ? i : Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);

            // 0 granted, 1 after waiting (still granted); -1 timeout, -2 cancelled, -3 deadlock.
            if (code is not (0 or 1))
            {
                throw new InvalidOperationException(
                    $"Could not acquire the Stockly database lease within {timeoutSeconds}s "
                    + $"(sp_getapplock returned {code}). Another database-dependent suite is "
                    + "either still running or has left a session holding the lock. Re-run "
                    + "once it finishes; if it never finishes, the lease's owner session is "
                    + "still open on the server.");
            }

            return new DatabaseLease(connection);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Explicit release, then close. Closing alone would also release it, but doing it
        // explicitly makes the intent visible and releases the lock a moment sooner.
        try
        {
            await using var command = _connection.CreateCommand();
            command.CommandText =
                "EXEC sys.sp_releaseapplock @Resource = N'Stockly.DatabaseDependentSuites', @LockOwner = 'Session';";
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException)
        {
            // The lease's job is done. A failure to release must not fail the test run; the
            // connection closing releases it regardless.
        }

        await _connection.DisposeAsync();
    }
}

/// <summary>
/// The shared collection NAME for the database-dependent suites.
/// <para>
/// One fixture per collection means one server handshake, one lease and one connection
/// pool for the whole suite instead of per test. Sharing a database is also what makes
/// tenant isolation observable: two contexts bound to different tenants must read the
/// same rows and disagree about which of them they may see.
/// </para>
/// <para>
/// Only the name lives here. The <c>[CollectionDefinition]</c> attribute itself must be
/// declared inside each test assembly (xUnit1041), so
/// <c>SqlServerTestCollection.cs</c> is linked into the four database-dependent
/// projects. The fixture has one implementation; the collection has one name.
/// </para>
/// </summary>
public static class SqlServerCollection
{
    /// <summary>The xUnit collection name.</summary>
    public const string Name = "sqlserver";
}