using System.Data.Common;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace Stockly.StocklyDbTestSupport;

/// <summary>
/// Creates and removes the two <em>separate</em> SQL principals the isolation tests run as.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why two principals and not one.</strong> Platform authority in Stockly is a
/// membership of the <c>stockly_platform_role</c> database role. A single principal cannot
/// be both the ordinary tenant application principal and a platform principal, because
/// proving that a tenant <em>cannot</em> reach platform data requires a principal that
/// genuinely lacks the role. If the tests ran as a role member they would prove nothing: the
/// spoofing test would pass only because the tester remembered to omit the role, and a bug
/// in the session-context handling would go unnoticed behind a membership that grants
/// everything anyway.
/// </para>
/// <para>
/// So the tests run as:
/// </para>
/// <list type="bullet">
/// <item><strong>tenant principal</strong> — DML on the tenant tables, and
/// <em>not</em> a member of <c>stockly_platform_role</c>. This is what ordinary
/// application traffic looks like.</item>
/// <item><strong>platform principal</strong> — the same DML <em>plus</em> role membership.
/// Used only for seeding, ground-truth verification, and the re-validation procedure.</item>
/// </list>
/// <para>
/// Neither is <c>sysadmin</c>, neither is <c>dbo</c>, and neither is <c>sa</c>. That is not
/// only hygiene: RLS is not a reliable boundary against a server administrator on every
/// engine configuration, and a test that runs as <c>sysadmin</c> could pass or fail for
/// reasons that have nothing to do with the isolation rules under test.
/// </para>
/// <para>
/// <strong>Principals are keyed per database.</strong> Each suite database gets its own pair
/// of logins, named with a hash of that database's name. A single mutable pair would mean the
/// second suite to ask for principals silently received the first suite's, pointed at the
/// wrong database — and tests that seed through one database while asserting through another
/// would fail for reasons that look like isolation defects.
/// </para>
/// <para>
/// <strong>Passwords are generated per run</strong> with a cryptographic RNG and are never
/// logged, never returned by a public member and never committed. If the operator supplies
/// their own principals through <see cref="TenantPrincipalVariable"/> /
/// <see cref="PlatformPrincipalVariable"/>, those are used as-is and nothing is created.
/// </para>
/// </remarks>
public sealed class SqlPrincipalProvisioner : IAsyncDisposable
{
    /// <summary>
    /// Optional. A pre-provisioned least-privilege <em>tenant</em> principal connection
    /// string. When set, the fixture uses it instead of creating one.
    /// </summary>
    public const string TenantPrincipalVariable = "STOCKLY_TEST_SQLSERVER_APP";

    /// <summary>
    /// Optional. A pre-provisioned <em>platform</em> principal connection string. It must be
    /// a member of <c>stockly_platform_role</c> in each test database, or the platform tests
    /// will fail — deliberately, because that is the property under test.
    /// </summary>
    public const string PlatformPrincipalVariable = "STOCKLY_TEST_SQLSERVER_PLATFORM";

    private const string TenantLoginPrefix = "stockly_test_tenant";
    private const string PlatformLoginPrefix = "stockly_test_platform";
    private const string TenantUserPrefix = "stockly_test_tenant";
    private const string PlatformUserPrefix = "stockly_test_platform";

    /// <summary>
    /// The two connection strings for one database.
    /// <para>
    /// A record rather than two loose properties so that a tenant/platform pair can never be
    /// mixed across databases — the compiler rejects the mistake that most easily produces a
    /// false pass here.
    /// </para>
    /// </summary>
    /// <param name="Tenant">Connection string for the non-platform principal.</param>
    /// <param name="Platform">Connection string for the platform-authority principal.</param>
    /// <param name="TenantLogin">Server-wide login name of the tenant principal.</param>
    /// <param name="PlatformLogin">Server-wide login name of the platform principal.</param>
    public sealed record Principals(
        string Tenant,
        string Platform,
        string TenantLogin,
        string PlatformLogin);

    private readonly string _adminConnectionString;
    private readonly Dictionary<string, Principals> _byDatabase = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _createdLogins = new(StringComparer.OrdinalIgnoreCase);

    private string? _suppliedTenant;
    private string? _suppliedPlatform;

    public SqlPrincipalProvisioner(string adminConnectionString) =>
        _adminConnectionString = adminConnectionString;

    /// <summary>Why a principal is unavailable. Never contains a credential.</summary>
    public string? UnavailableReason { get; private set; }

    /// <summary>
    /// Adopts operator-supplied principals instead of creating any, for an environment whose
    /// admin credential may not have <c>CREATE LOGIN</c>.
    /// </summary>
    public void UseSupplied(string tenantConnectionString, string platformConnectionString)
    {
        _suppliedTenant = tenantConnectionString;
        _suppliedPlatform = platformConnectionString;
    }

    /// <summary>
    /// Creates (or returns) the principal pair for <paramref name="databaseName"/>, mapping
    /// both users into that database and granting least privilege.
    /// </summary>
    /// <param name="databaseName">The per-suite database to grant access to.</param>
    /// <exception cref="InvalidOperationException">
    /// The principals could not be created and the reason was not a permissions failure, or no
    /// principal is available for the database.
    /// </exception>
    public async Task<Principals> GetAsync(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        if (_byDatabase.TryGetValue(databaseName, out var existing))
        {
            return existing;
        }

        // Operator-supplied principals are used verbatim. Note they are NOT rewritten to point
        // at databaseName: silently repointing a credential the operator chose would be worse
        // than failing, and a mismatched database surfaces immediately as an empty tenant.
        if (_suppliedTenant is not null && _suppliedPlatform is not null)
        {
            var supplied = new Principals(_suppliedTenant, _suppliedPlatform, string.Empty, string.Empty);
            _byDatabase[databaseName] = supplied;
            return supplied;
        }

        // Names are derived from the database, so concurrent suites never collide and a crashed
        // run leaves a predictable name for the next run to replace.
        var suffix = "_" + StableSuffix(databaseName);
        var tenantLogin = TenantLoginPrefix + suffix;
        var platformLogin = PlatformLoginPrefix + suffix;
        var tenantUser = TenantUserPrefix + suffix;
        var platformUser = PlatformUserPrefix + suffix;

        await using var admin = new SqlConnection(AdminBuilderFor("master").ToString());
        await admin.OpenAsync();

        string tenantPassword;
        string platformPassword;

        try
        {
            tenantPassword = await CreateLoginAsync(admin, tenantLogin);
            platformPassword = await CreateLoginAsync(admin, platformLogin);
        }
        catch (SqlException ex) when (IsPermissionFailure(ex.Number))
        {
            UnavailableReason =
                $"Could not create the SQL login principals the isolation tests require "
                + $"(SQL Server error {ex.Number}). The {tenantLogin} / {platformLogin} "
                + "accounts need CREATE LOGIN rights on the instance. If this account cannot "
                + "create logins, supply pre-provisioned principals via "
                + $"{TenantPrincipalVariable} and {PlatformPrincipalVariable} instead. "
                + "Substituting sysadmin or dbo is NOT permitted.";

            throw new InvalidOperationException(UnavailableReason, ex);
        }
        catch (SqlException ex)
        {
            // Not a permissions failure, so not the operator's problem: this is a defect in this
            // file. Reporting it as "you need CREATE LOGIN" would send the reader to grant a
            // privilege that was never the issue.
            throw new InvalidOperationException(
                $"Provisioning the isolation test principals failed with SQL Server error "
                + $"{ex.Number}, which is a fault in this provisioning code rather than a "
                + $"permissions problem: {ex.Message}",
                ex);
        }

        await using var database = new SqlConnection(AdminBuilderFor(databaseName).ToString());
        await database.OpenAsync();

        await MapUserAsync(database, tenantUser, tenantLogin);
        await MapUserAsync(database, platformUser, platformLogin);

        // Platform authority: role membership only. This is the whole mechanism, so granting it
        // to the tenant principal would silently void every spoofing test.
        //
        // The statement is assembled into a variable rather than written inline. Both EXEC and
        // sp_executesql reject a concatenated argument -- EXEC(N'...' + QUOTENAME(@n) + N'')
        // and EXEC sp_executesql N'...' + @n are both syntax errors on this engine -- so the
        // only form that works is a single variable holding the finished text.
        await ExecuteAsync(database, $"""
DECLARE @id nvarchar(max) = QUOTENAME({Quote(platformUser)});
DECLARE @sql nvarchar(max) = N'ALTER ROLE stockly_platform_role ADD MEMBER ' + @id + N';';
IF IS_MEMBER(N'stockly_platform_role') IS NULL OR IS_MEMBER(N'stockly_platform_role') <> 1
    EXEC sp_executesql @sql;
""");

        // Least-privilege data access.
        //
        // Note what is NOT granted: no EXECUTE on the security schema, no CONTROL, no ALTER,
        // no IMPERSONATE, nothing at server level. Measured: RLS predicates and CHECK
        // constraints invoke security.fnStocklyTenantVisible / fnStocklyRowTenantAllowed in
        // the engine's own context, so the tenant principal needs no EXECUTE grant to be
        // correctly isolated. Withholding it is strictly better -- it also makes a direct call
        // to those functions fail with "EXECUTE permission denied" (error 229).
        await ExecuteAsync(database, $"""
DECLARE @sql nvarchar(max) = N'';
SELECT @sql = @sql + N'GRANT SELECT, INSERT, UPDATE, DELETE ON '
    + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
    + N' TO ' + QUOTENAME({Quote(tenantUser)}) + N', ' + QUOTENAME({Quote(platformUser)}) + N';'
    + CHAR(13) + CHAR(10)
FROM sys.tables t
WHERE t.is_ms_shipped = 0 AND t.name <> N'__EFMigrationsHistory';
EXEC sp_executesql @sql;
""");

        var principals = new Principals(
            BuildPrincipalConnectionString(databaseName, tenantLogin, tenantPassword),
            BuildPrincipalConnectionString(databaseName, platformLogin, platformPassword),
            tenantLogin,
            platformLogin);

        _byDatabase[databaseName] = principals;
        return principals;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Users first, then logins: a login cannot be dropped while a database user maps it.
        foreach (var (databaseName, principals) in _byDatabase)
        {
            if (principals.TenantLogin.Length == 0)
            {
                // Operator-supplied: not ours to drop.
                continue;
            }

            try
            {
                await using var target = new SqlConnection(AdminBuilderFor(databaseName).ToString());
                await target.OpenAsync();

                // Role membership goes first: a user still in stockly_platform_role cannot be
                // dropped.
                await ExecuteAsync(target, $"""
DECLARE @p nvarchar(max) = QUOTENAME({Quote(principals.PlatformLogin)});
DECLARE @t nvarchar(max) = QUOTENAME({Quote(principals.TenantLogin)});
DECLARE @dropRole nvarchar(max) = N'ALTER ROLE stockly_platform_role DROP MEMBER ' + @p + N';';
DECLARE @dropP nvarchar(max) = N'DROP USER ' + @p + N';';
DECLARE @dropT nvarchar(max) = N'DROP USER ' + @t + N';';

-- IS_MEMBER(...) with no principal_name argument answers for the CURRENT user, which here is
-- the administrator running the cleanup -- not for the platform principal being dropped. That
-- user is not a role member, so the call returns NULL, the DROP MEMBER is skipped, and the
-- following DROP USER then fails because the principal is still in the role. The membership is
-- therefore looked up directly against sys.database_role_members for the named principal.
IF EXISTS (
    SELECT 1
    FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = N'stockly_platform_role'
      AND m.name = {Quote(principals.PlatformLogin)})
    EXEC sp_executesql @dropRole;

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = {Quote(principals.PlatformLogin)})
    EXEC sp_executesql @dropP;

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = {Quote(principals.TenantLogin)})
    EXEC sp_executesql @dropT;
""");
            }
            catch (SqlException)
            {
                // Best effort. A leftover test login on a developer machine is untidy, not a
                // security defect, and it must not mask a test failure.
            }
        }

        if (_createdLogins.Count == 0)
        {
            return;
        }

        try
        {
            await using var master = new SqlConnection(AdminBuilderFor("master").ToString());
            await master.OpenAsync();

            foreach (var login in _createdLogins)
            {
                await ExecuteAsync(master,
                    $"IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = {Quote(login)}) DROP LOGIN {Ident(login)};");
            }
        }
        catch (SqlException)
        {
            // See above: cleanup must never turn a passing suite into a failing one.
        }
    }

    /// <summary>
    /// Creates a login, replacing a leftover from an interrupted run, and returns its
    /// generated password.
    /// <para>
    /// The returned password is the only way to authenticate as this login afterwards, so it
    /// is never logged. It is handed straight to
    /// <see cref="BuildPrincipalConnectionString"/> and discarded.
    /// </para>
    /// </summary>
    private async Task<string> CreateLoginAsync(SqlConnection admin, string loginName)
    {
        // Re-running must be idempotent: drop any leftover first, otherwise CREATE LOGIN fails
        // and the suite reports a provisioning error that has nothing to do with the code under
        // test.
        //
        // DROP USER is database-scoped and there is no server-level "drop this user
        // everywhere" statement, so each database is switched to with USE inside one dynamic
        // batch.
        await ExecuteAsync(admin, $"""
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = {Quote(loginName)})
BEGIN
    DECLARE @drops nvarchar(max) = N'';
    SELECT @drops = @drops + N'USE ' + QUOTENAME(name) + N'; DROP USER IF EXISTS ' + QUOTENAME({Quote(loginName)}) + N';'
    FROM sys.databases WHERE database_id > 4 AND state = 0;
    IF LEN(@drops) > 0 EXEC sp_executesql @drops;
    DROP LOGIN {Ident(loginName)};
END
""");

        var password = GeneratePassword();

        // CHECK_POLICY off: the generated password is random, so complexity adds nothing, and
        // leaving it off keeps the value inside every character class SQL Server accepts in a
        // connection string without escaping gymnastics.
        await ExecuteAsync(admin,
            $"CREATE LOGIN {Ident(loginName)} WITH PASSWORD = {Quote(password)}, CHECK_POLICY = OFF;");

        _createdLogins.Add(loginName);
        return password;
    }

    private static async Task MapUserAsync(SqlConnection database, string userName, string loginName)
    {
        await ExecuteAsync(database, $"""
DECLARE @sql nvarchar(max) = QUOTENAME({Quote(userName)});
DECLARE @drop nvarchar(max) = N'DROP USER ' + @sql + N';';
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = {Quote(userName)})
    EXEC sp_executesql @drop;
""");

        await ExecuteAsync(database, $"""
DECLARE @u nvarchar(max) = QUOTENAME({Quote(userName)});
DECLARE @l nvarchar(max) = QUOTENAME({Quote(loginName)});
DECLARE @sql nvarchar(max) = N'CREATE USER ' + @u + N' FOR LOGIN ' + @l + N';';
EXEC sp_executesql @sql;
""");
    }

    private string BuildPrincipalConnectionString(string databaseName, string loginName, string password)
    {
        var builder = new SqlConnectionStringBuilder(AdminBuilderFor(databaseName).ToString())
        {
            IntegratedSecurity = false,
            UserID = loginName,
            Password = password,
        };

        return builder.ToString();
    }

    private SqlConnectionStringBuilder AdminBuilderFor(string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(_adminConnectionString) { InitialCatalog = databaseName };

        // The principal connections must be plain SQL logins, so the admin connection's own
        // settings are normalised: anything Windows-authenticated is dropped rather than left
        // to conflict with the supplied credentials.
        if (!string.IsNullOrEmpty(builder.UserID) || !string.IsNullOrEmpty(builder.Password))
        {
            builder.IntegratedSecurity = false;
        }

        return builder;
    }

    /// <summary>
    /// Quotes a value as a T-SQL string literal.
    /// <para>
    /// Used for generated passwords and for names being <em>compared</em> in
    /// <c>WHERE name = ...</c>. The doubling of embedded quotes is what makes it safe; an
    /// unescaped value here would be an injection point, so this is deliberately the only
    /// place T-SQL text is assembled from a runtime value.
    /// </para>
    /// <para>
    /// Never use this for an identifier such as a login or user name — that is
    /// <see cref="Ident"/>. A string literal in identifier position is the syntax error
    /// <c>DROP LOGIN N'some_name'</c>.
    /// </para>
    /// </summary>
    private static string Quote(string value) => $"N'{value.Replace("'", "''", StringComparison.Ordinal)}'";

    /// <summary>
    /// Quotes a value as a T-SQL identifier, with or without brackets.
    /// <para>
    /// Closing brackets are doubled rather than escaped, which is what makes this safe. Used
    /// for logins, users and anything else in identifier position.
    /// </para>
    /// </summary>
    private static string Ident(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";

    /// <summary>
    /// True only for error numbers that mean "this principal is not allowed to do that".
    /// <para>
    /// Provisioning uses this to decide whether a failure is something the operator can fix by
    /// granting rights, or a defect in this file. Conflating the two is actively harmful: a
    /// syntax error reported as "you need CREATE LOGIN" sends the reader to grant a privilege
    /// that was never the problem.
    /// </para>
    /// </summary>
    private static bool IsPermissionFailure(int errorNumber) => errorNumber is
        229 or    // The SELECT permission was denied on the object.
        230 or    // The column permission was denied on the object.
        297 or    // The user does not have permission to perform this action.
        300 or    // The VIEW SERVER STATE permission was denied.
        916 or    // The server principal is not able to access the database under the current security context.
        15025 or  // The server principal is not able to access the database under the current security context.
        15026 or  // The server principal is not able to access the database under the current security context.
        15247;    // The user does not have permission to perform this action.

    /// <summary>
    /// A short, deterministic, identifier-safe suffix derived from the database name.
    /// <para>
    /// Deterministic so that a run interrupted mid-provisioning leaves a name the next run
    /// will recognise and replace, rather than accumulating orphans. Uses the same FNV-1a
    /// basis as <c>System.IO.Hashing</c> would, computed inline to avoid a dependency for
    /// eight lines of arithmetic.
    /// </para>
    /// </summary>
    private static string StableSuffix(string databaseName)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var c in databaseName)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash.ToString("x8", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string GeneratePassword()
    {
        // 32 hex characters plus fixed punctuation: long enough to be unguessable, and
        // restricted to characters that need no escaping in a connection string.
        var bytes = RandomNumberGenerator.GetBytes(24);
        return "Zz" + Convert.ToHexString(bytes) + "!7q";
    }

    private static async Task ExecuteAsync(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 120;
        await command.ExecuteNonQueryAsync();
    }
}