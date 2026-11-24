using System.Data.Common;
using System.Globalization;
using System.Text;

namespace Stockly.Infrastructure.Persistence.Security;

/// <summary>
/// Writes the authenticated scope into <c>SESSION_CONTEXT</c> for one physical
/// connection, and removes it again before that connection is released.
/// <para>
/// <c>SESSION_CONTEXT</c> is per physical connection, not per <see cref="DbContext"/> and
/// not per request. ADO.NET pools connections, so a connection that served tenant A can
/// be handed to one serving tenant B. Two consequences follow, and both are
/// load-bearing:
/// </para>
/// <list type="number">
/// <item>The scope is set on <strong>every physical open</strong>, so a pooled connection
/// can never carry a stale tenant's scope into the next request. This is the guarantee
/// the isolation tests assert.</item>
/// <item>The scope is <strong>cleared before close</strong>. Clearing is not tidiness: a
/// connection returned to the pool still holds its <c>SESSION_CONTEXT</c>, so without
/// this a connection opened outside EF would inherit a previous tenant's scope. It is
/// defence in depth — see the note in
/// <see cref="TenantScopeConnectionInterceptor"/> about why the clear is best effort.</item>
/// </list>
/// <para>
/// Every write is unconditional: both keys are always assigned, including to
/// <c>NULL</c>. Nothing is only ever "updated if already present", because a
/// partially-populated session context is exactly the ambiguous state that must never
/// be reachable.
/// </para>
/// </summary>
public static class TenantScopeSession
{
    /// <summary>
    /// Sets the session context for <paramref name="connection"/> from the authenticated
    /// tenant context.
    /// <para>
    /// A tenant session writes the scope and the tenant id. A platform session writes
    /// the scope and no tenant id — none is invented for it. Anything else (no tenant
    /// id: an unauthenticated request) writes <c>NULL</c> to both keys, leaving the
    /// connection unscoped, which the database predicates treat as "no access at all"
    /// rather than as "no restriction".
    /// </para>
    /// </summary>
    public static Task ApplyAsync(DbConnection connection, ITenantContext tenantContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(tenantContext);

        if (tenantContext.IsPlatformScope)
        {
            return ApplyPlatformAsync(connection, cancellationToken);
        }

        if (tenantContext.TenantId is { } tenantId && tenantId != Guid.Empty)
        {
            return ApplyTenantAsync(connection, tenantId, cancellationToken);
        }

        // Deliberately does not fall back to platform scope. An absent tenant id is the
        // signature of an unauthenticated request, and granting it platform authority
        // would make "not logged in" the most powerful session in the system.
        return ClearAsync(connection, cancellationToken);
    }

    /// <summary>Scopes a connection to one tenant.</summary>
    public static Task ApplyTenantAsync(DbConnection connection, Guid tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "The empty GUID is not a tenant id. Scoping a connection to it would make "
                + "every predicate evaluate against an unmatchable value, which reads as a "
                + "deny for writes but as a silent empty result set for reads.",
                nameof(tenantId));
        }

        var tenantIdText = tenantId.ToString("D", CultureInfo.InvariantCulture);

        // The tenant id is cleared first and re-set last, so the connection is never
        // momentarily holding scope=1 together with a stale tenant id from an earlier
        // request. It is never momentarily holding scope=1 and no tenant id either:
        // the scope is assigned before the id, and both land in one batch.
        return ExecuteAsync(connection, BuildScript(
            (TenantScopeSessionKeys.TenantId, null),
            (TenantScopeSessionKeys.Scope, IntLiteral(TenantScopeValues.Tenant)),
            (TenantScopeSessionKeys.TenantId, TextLiteral(tenantIdText))), cancellationToken);
    }

    /// <summary>
    /// Scopes a connection to platform authority.
    /// <para>
    /// The session-context value is necessary but not sufficient: the database predicate
    /// additionally requires membership of <see cref="TenantScopePlatformRole"/>, which
    /// only an <c>ALTER ROLE</c> against the database can confer.
    /// </para>
    /// </summary>
    public static Task ApplyPlatformAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return ExecuteAsync(connection, BuildScript(
            (TenantScopeSessionKeys.TenantId, null),
            (TenantScopeSessionKeys.Scope, IntLiteral(TenantScopeValues.Platform))), cancellationToken);
    }

    /// <summary>
    /// Removes every Stockly key. The connection then matches nothing tenant-scoped,
    /// which is the correct state for a connection nobody has authenticated.
    /// </summary>
    public static Task ClearAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return ExecuteAsync(connection, BuildScript(
            (TenantScopeSessionKeys.Scope, null),
            (TenantScopeSessionKeys.TenantId, null)), cancellationToken);
    }

    /// <summary>
    /// Renders a session-context value as an <c>int</c> literal.
    /// <para>
    /// The predicates read the scope with <c>TRY_CONVERT(int, SESSION_CONTEXT(...))</c>, and an
    /// <c>int</c> <c>sql_variant</c> is what they are documented to be tested against, so the
    /// scope value stays unquoted here rather than being rendered as text.
    /// </para>
    /// </summary>
    private static string IntLiteral(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Renders a session-context value as a quoted string literal.
    /// <para>
    /// Quoting is not cosmetic. Emitting a bare <c>uniqueidentifier</c> produces
    /// <c>@value = 4f2c8d54-465e-...</c>, which T-SQL parses as arithmetic on the numeric
    /// literal <c>4</c> minus identifiers -- so the statement fails, or worse, succeeds while
    /// storing something that is not the tenant id at all.
    /// </para>
    /// </summary>
    private static string TextLiteral(string value) => $"N'{value}'";

    /// <summary>
    /// Builds the <c>sp_set_session_context</c> calls.
    /// <para>
    /// The key names are embedded rather than parameterised because
    /// <c>sp_set_session_context</c> does not accept parameters for its key. Both names come
    /// from <see cref="TenantScopeSessionKeys"/> and are compiled-in constants.
    /// </para>
    /// <para>
    /// Each value is an <em>already-rendered SQL literal</em> produced by
    /// <see cref="IntLiteral"/> or <see cref="TextLiteral"/>, or the keyword <c>NULL</c>. That
    /// is deliberate: the two value kinds need different quoting, and picking the wrong one is
    /// silent -- a mis-quoted GUID is parsed as arithmetic rather than rejected -- so the
    /// decision is made explicitly per call site instead of being inferred from a runtime type.
    /// </para>
    /// <para>
    /// All assignments are emitted into one batch so a physical open costs one round trip, not
    /// one per key. Ordering within the batch is preserved by the server, which is what makes
    /// the clear-then-set sequences above meaningful.
    /// </para>
    /// </summary>
    private static string BuildScript(params (string Key, string? Literal)[] assignments)
    {
        var script = new StringBuilder();

        foreach (var (key, literal) in assignments)
        {
            _ = script.Append("EXEC sys.sp_set_session_context @key = N'")
                .Append(key)
                .Append("', @value = ")
                .Append(literal ?? "NULL")
                .Append(", @read_only = 0;");
        }

        return script.ToString();
    }

    private static async Task ExecuteAsync(DbConnection connection, string script, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = script;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}