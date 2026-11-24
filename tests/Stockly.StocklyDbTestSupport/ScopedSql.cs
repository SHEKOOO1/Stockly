using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Stockly.Infrastructure.Persistence.Security;

namespace Stockly.StocklyDbTestSupport;

/// <summary>
/// Runs SQL as one of the two test principals, with an explicit scope in
/// <c>SESSION_CONTEXT</c>.
/// </summary>
/// <remarks>
/// <para>
/// The tests deliberately go through raw ADO.NET rather than only through EF. Half of what
/// has to be proven is that the <em>database</em> refuses a cross-tenant write even when the
/// ORM is not in the picture at all -- a hand-written SQL statement, a stored procedure, or a
/// future service that bypasses the data access layer. An EF-only test cannot distinguish
/// "the database refused" from "the ORM filtered it", and that distinction is the entire
/// point of the security boundary.
/// </para>
/// <para>
/// Every connection opened here has its scope written before the first statement runs, which
/// is the same contract
/// <see cref="TenantScopeConnectionInterceptor"/> implements for the application. A test that
/// forgot to set a scope would silently assert against an unscoped connection, so the scope is
/// always explicit, including the unscoped case.
/// </para>
/// </remarks>
public static class ScopedSql
{
    /// <summary>
    /// Opens a connection as the <em>tenant</em> principal, scoped to
    /// <paramref name="tenantId"/>.
    /// <para>
    /// Writes both keys: the scope discriminator <em>and</em> the tenant id. Setting only
    /// the discriminator would produce a connection that claims tenant scope with no tenant,
    /// which the predicates deny — a test written that way asserts against a fail-closed
    /// connection and would report cross-tenant attacks as "successfully refused" while the
    /// tenant could not read its own data either.
    /// </para>
    /// </summary>
    public static async Task<SqlConnection> OpenTenantAsync(
        string connectionString,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Guards the same thing TenantScopeSession.ApplyTenantAsync guards. Scoping a connection
        // to the empty GUID would make every predicate evaluate against an unmatchable value:
        // reads silently return nothing and writes are refused, which looks exactly like a
        // working tenant session that happens to own no rows.
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "The empty GUID is not a tenant id.", nameof(tenantId));
        }

        var connection = await OpenAsync(connectionString, cancellationToken);

        await SetScopeAsync(
            connection,
            (TenantScopeSessionKeys.Scope, TenantScopeValues.Tenant));

        await SetTenantIdAsync(connection, tenantId);

        return connection;
    }

    /// <summary>
    /// Opens a connection as the <em>platform</em> principal, scoped to platform authority.
    /// <para>
    /// This only does anything because that principal is a member of
    /// <c>stockly_platform_role</c>. Setting the scope value alone is not sufficient, which is
    /// the property the spoofing test relies on.
    /// </para>
    /// </summary>
    public static async Task<SqlConnection> OpenPlatformAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var connection = await OpenAsync(connectionString, cancellationToken);

        await SetScopeAsync(
            connection,
            (TenantScopeSessionKeys.Scope, TenantScopeValues.Platform));

        return connection;
    }

    /// <summary>
    /// Opens a connection with <strong>no</strong> Stockly scope at all, modelling a
    /// connection that nobody has authenticated.
    /// </summary>
    public static Task<SqlConnection> OpenUnscopedAsync(
        string connectionString,
        CancellationToken cancellationToken = default) => OpenAsync(connectionString, cancellationToken);

    /// <summary>
    /// Opens a connection as the tenant principal but claims <c>StocklyScope = 2</c> without
    /// being a member of the platform role. This is the spoofing attempt, and it must see
    /// nothing.
    /// </summary>
    public static async Task<SqlConnection> OpenSpoofedPlatformAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var connection = await OpenAsync(connectionString, cancellationToken);

        await SetScopeAsync(
            connection,
            (TenantScopeSessionKeys.Scope, TenantScopeValues.Platform));

        return connection;
    }

    /// <summary>
    /// Executes <paramref name="sql"/> and returns the affected row count. The single call
    /// every write assertion goes through, so "0 rows affected" and "error 547" are read the
    /// same way everywhere.
    /// </summary>
    public static async Task<int> ExecuteAsync(SqlConnection connection, string sql, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 60;
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Executes <paramref name="sql"/> and returns a single scalar value.</summary>
    public static async Task<T> ScalarAsync<T>(SqlConnection connection, string sql, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 60;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? default! : (T)Convert.ChangeType(result, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Executes <paramref name="sql"/> and returns every row as a dictionary.</summary>
    public static async Task<List<Dictionary<string, object?>>> QueryAsync(
        SqlConnection connection,
        string sql,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 60;

        var results = new List<Dictionary<string, object?>>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
            }

            results.Add(row);
        }

        return results;
    }

    /// <summary>
    /// Wraps <paramref name="action"/> and returns the SQL Server error number if it failed,
    /// or <see langword="null"/> if it succeeded.
    /// <para>
    /// Used so a test can assert on the <em>specific</em> error. Asserting only "it threw"
    /// would pass for a typo in the SQL as readily as for the security control firing, which
    /// is a test that can only ever be accidentally green.
    /// </para>
    /// </summary>
    public static async Task<int?> ErrorNumberFromAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (SqlException ex)
        {
            return ex.Number;
        }
    }

    private static async Task<SqlConnection> OpenAsync(string connectionString, CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task SetScopeAsync(
        SqlConnection connection,
        params (string Key, int? Value)[] assignments)
    {
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 60;

        var sql = string.Empty;
        foreach (var (key, value) in assignments)
        {
            // The key names are the compiled-in constants from TenantScopeSessionKeys and the
            // value is one of the two compiled-in TenantScopeValues, so no runtime text reaches
            // this SQL. Emitting the integer unquoted matters: sp_set_session_context stores it
            // as an int sql_variant, and the predicate reads it back with TRY_CONVERT(int, ...).
            var literal = value?.ToString(CultureInfo.InvariantCulture) ?? "NULL";

            sql += $"EXEC sys.sp_set_session_context @key = N'{key}', @value = {literal}, @read_only = 0;";
        }

        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Writes the tenant id alongside the scope.
    /// <para>
    /// A <see cref="Guid"/> is written as a quoted <c>'D'</c>-formatted literal, and quoted
    /// rather than passed as a parameter because <c>sp_set_session_context</c> does not accept
    /// parameters for its key. The value is formatted here rather than by the caller so no
    /// unvalidated text can reach the statement.
    /// </para>
    /// </summary>
    private static async Task SetTenantIdAsync(SqlConnection connection, Guid tenantId)
    {
        var literal = tenantId.ToString("D", CultureInfo.InvariantCulture);

        await using var command = connection.CreateCommand();
        command.CommandTimeout = 60;
        command.CommandText =
            $"EXEC sys.sp_set_session_context @key = N'{TenantScopeSessionKeys.TenantId}', @value = N'{literal}', @read_only = 0;";

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// The SQL Server error number raised when a CHECK constraint rejects a row.
    /// <para>
    /// Hard-coded rather than looked up, because the test's value is that this specific,
    /// documented code comes back: it is the number Stockly's error translation maps, so a
    /// test that accepted any error would no longer verify that the mapping target is the one
    /// actually produced.
    /// </para>
    /// </summary>
    public const int CheckConstraintViolationErrorNumber = 547;

    /// <summary>The SQL Server error number raised when a permission is denied.</summary>
    public const int PermissionDeniedErrorNumber = 229;

    /// <summary>Kept so callers can assert on <see cref="Data"/>-shaped helpers explicitly.</summary>
    public static CommandBehavior DefaultBehaviour => CommandBehavior.Default;
}